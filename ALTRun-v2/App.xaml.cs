using System;
using System.Threading;
using System.Windows;
using ALTRun.Services;
using ALTRun.Views;

namespace ALTRun
{
    public partial class App : System.Windows.Application
    {
        private const string MutexName = "Local\\ALTRun_V2_SingleInstance_Mutex";
        private const string WakeupEventName = "Local\\ALTRun_V2_Wakeup_Event";

        private static Mutex? _mutex;
        private static bool _ownsMutex = false;
        private static EventWaitHandle? _wakeEvent;
        private static RegisteredWaitHandle? _registeredWait;

        public static ConfigManager Config { get; } = new();

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // 渲染模式交由 WPF 默认（硬件加速）：保留 ClearType 字体与流畅动画。
            // 不再强制 SoftwareOnly——强制软件渲染会让字体失去 ClearType、动画掉帧，
            // 与主题切换的淡入淡出效果冲突，且实测"极速呼出"并无收益。

            AppLog.Write($"===== ALTRun 启动 v{typeof(App).Assembly.GetName().Version} ({(e.Args?.Length > 0 ? string.Join(" ", e.Args) : "无参数")}) =====");

            // 非 UI 线程的未处理异常（如后台任务）同样落盘，便于事后排查
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
                AppLog.Write("AppDomain 未处理异常", args.ExceptionObject as Exception);

            DispatcherUnhandledException += (s, args) =>
            {
                // 针对 DirectWrite 字体度量或文件丢失偶发异常做静默容错保护
                if (args.Exception != null &&
                    (args.Exception.Message.Contains("Unable to find the specified file") ||
                     args.Exception.StackTrace?.Contains("GetDesignGlyphMetrics") == true ||
                     args.Exception.StackTrace?.Contains("GlyphTypeface") == true))
                {
                    AppLog.Write("已忽略字体度量异常", args.Exception);
                    args.Handled = true;
                    return;
                }

                AppLog.Write("UI 线程未处理异常", args.Exception);
                System.Windows.MessageBox.Show(
                    string.Format(AppStrings.CrashMessageFormat, args.Exception?.Message, args.Exception?.StackTrace),
                    AppStrings.TipTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                args.Handled = true;
            };

            // 检测是否为开机静默启动参数 (--silent 或 --autorun)
            bool isSilentStartup = false;
            if (e.Args != null && e.Args.Length > 0)
            {
                foreach (var arg in e.Args)
                {
                    if (string.Equals(arg, "--silent", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(arg, "--autorun", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(arg, "-silent", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(arg, "/silent", StringComparison.OrdinalIgnoreCase))
                    {
                        isSilentStartup = true;
                        break;
                    }
                }
            }

            // 单实例检测与重复运行唤醒支持
            _mutex = new Mutex(true, MutexName, out bool createdNew);
            if (!createdNew)
            {
                // 仅当用户手动显式再次运行程序（非开机静默自启动模式）时，才触发命名事件唤醒现有实例的主搜索框
                if (!isSilentStartup)
                {
                    try
                    {
                        if (EventWaitHandle.TryOpenExisting(WakeupEventName, out var existingEvent))
                        {
                            existingEvent.Set();
                            existingEvent.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLog.Write($"[ALTRun] App.xaml.cs:75 静默异常: {ex.Message}");
                    }
                }

                // 当前实例并未拥有互斥体所有权，绝不调用 ReleaseMutex，直接释放句柄并退出
                _mutex.Dispose();
                _mutex = null;
                AppLog.Write("检测到已有实例，本次启动已退出并唤醒现有实例");
                Shutdown();
                return;
            }

            _ownsMutex = true;

            // 监听重复运行唤醒事件
            try
            {
                _wakeEvent = new EventWaitHandle(false, EventResetMode.AutoReset, WakeupEventName);
                _registeredWait = ThreadPool.RegisterWaitForSingleObject(_wakeEvent, (state, timedOut) =>
                {
                    if (!timedOut)
                    {
                        Current?.Dispatcher?.InvokeAsync(() =>
                        {
                            if (Current.MainWindow is MainWindow mainWin)
                            {
                                mainWin.ShowAndActivate();
                            }
                        });
                    }
                }, null, -1, false);
            }
            catch (Exception ex)
            {
                AppLog.Write($"[ALTRun] App.xaml.cs:105 静默异常: {ex.Message}");
            }

            // 加载配置
            Config.Load();

            // 配置损坏时已自动备份原文件，这里明确告知用户备份位置：避免"我的快捷方式莫名全没了"的误解，
            // 也给出恢复路径。开机静默启动时不打扰用户（备份文件就在程序目录，随时可取）。
            if (Config.CorruptConfigBackupPath != null && !isSilentStartup)
            {
                try
                {
                    System.Windows.MessageBox.Show(
                        string.Format(AppStrings.ConfigCorruptMessageFormat, Config.CorruptConfigBackupPath),
                        AppStrings.ConfigCorruptTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    AppLog.Write($"[ALTRun] App.xaml.cs:119 提示配置备份失败: {ex.Message}");
                }
            }

            // 启动主搜索窗（静默常驻右下角托盘，不弹窗打扰用户，极速待命）
            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.InitializeBackgroundTray();

            // 首次运行向导（如果是全新运行无配置，引导用户扫描已安装软件；但若为开机静默启动，绝不弹窗打扰用户）
            if (Config.IsFirstRun && !isSilentStartup)
            {
                var wizard = new WelcomeWizardWindow();
                wizard.ShowDialog();
            }

            // 后台静默检测 GitHub Releases 更新（延迟 10 秒启动，完全不影响开机极速响应）
            CheckUpdateInBackground();
        }

        private static void CheckUpdateInBackground()
        {
            // 每月 1 号检查一次更新，避免每次启动都请求 GitHub API（原为每次启动都检查）。
            // 用配置里的 LastUpdateCheckMonth（yyyy-MM）记录已检查月份，同一天多次启动不会重复请求；
            // 若 1 号当天未开机，则本月跳过、次月 1 号再检查。
            var now = DateTime.Now;
            if (now.Day != 1) return;

            string thisMonth = now.ToString("yyyy-MM");
            if (string.Equals(Config.Settings.LastUpdateCheckMonth, thisMonth, StringComparison.Ordinal)) return;

            Task.Run(async () =>
            {
                await Task.Delay(10000);
                try
                {
                    var info = await UpdateCheckerService.CheckForUpdatesAsync();
                    if (info.HasUpdate)
                    {
                        Current?.Dispatcher?.Invoke(() =>
                        {
                            string msg = string.Format(AppStrings.UpdateNewVersionBodyFormat, info.CurrentVersion, info.LatestVersion);

                            if (System.Windows.MessageBox.Show(msg, AppStrings.UpdateTitleNewVersion, MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                            {
                                UpdateCheckerService.OpenUrl(info.ReleaseUrl);
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    AppLog.Write($"[ALTRun] App.xaml.cs:159 静默异常: {ex.Message}");
                }
                finally
                {
                    // 无论是否有更新、网络是否可用，都标记本月已检查，避免 1 号当天反复启动反复请求。
                    // 用 MarkDirty 延迟合并写盘，不阻塞后台线程。
                    Current?.Dispatcher?.Invoke(() =>
                    {
                        Config.Settings.LastUpdateCheckMonth = thisMonth;
                        Config.MarkDirty();
                    });
                }
            });
        }

        public static void OpenManageWindow(Models.ShortCutItem? itemToEdit = null, bool addNew = false)
        {
            foreach (Window win in Current.Windows)
            {
                if (win is ManageWindow manageWin)
                {
                    if (addNew)
                    {
                        manageWin.BtnAdd_Click_External();
                    }
                    else if (itemToEdit != null)
                    {
                        manageWin.SelectAndFocusItem(itemToEdit);
                    }
                    manageWin.Activate();
                    return;
                }
            }

            var newManageWin = new ManageWindow(itemToEdit, addNew);
            newManageWin.Show();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            // 兜底释放托盘图标与全局热键：Shutdown 强关窗口时不一定走 OnClosed，
            // 不释放会残留"僵尸"托盘图标（鼠标划过才消失）。
            try
            {
                (this.MainWindow as MainWindow)?.ReleaseSystemResources();
            }
            catch (Exception ex)
            {
                AppLog.Write($"[ALTRun] App.xaml.cs:208 释放系统资源失败: {ex.Message}");
            }

            // 确保延迟保存的频次改动在退出前落盘
            Config.FlushPendingSave();

            try
            {
                _registeredWait?.Unregister(null);
                _wakeEvent?.Dispose();
            }
            catch (Exception ex)
            {
                AppLog.Write($"[ALTRun] App.xaml.cs:206 静默异常: {ex.Message}");
            }

            if (_ownsMutex && _mutex != null)
            {
                try
                {
                    _mutex.ReleaseMutex();
                }
                catch (Exception ex)
                {
                    AppLog.Write($"[ALTRun] App.xaml.cs:214 静默异常: {ex.Message}");
                }
                _mutex.Dispose();
                _mutex = null;
            }

            AppLog.Write("===== ALTRun 退出 =====");
        }
    }
}
