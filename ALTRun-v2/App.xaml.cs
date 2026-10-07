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
        private static System.Windows.Threading.DispatcherTimer? _idleTimer;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // 1. 启用纯软件渲染模式：彻底关闭 Direct3D 显卡渲染管线与驱动加载，大幅消减常驻内存
            System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;

            DispatcherUnhandledException += (s, args) =>
            {
                System.Windows.MessageBox.Show($"程序发生异常: {args.Exception.Message}\n{args.Exception.StackTrace}", "ALTRun 提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                args.Handled = true;
            };

            // 单实例检测与重复运行唤醒支持
            _mutex = new Mutex(true, MutexName, out bool createdNew);
            if (!createdNew)
            {
                // 如果已有实例在后台运行，通过命名事件唤醒现有实例的主搜索框，然后安全退出当前重复进程
                try
                {
                    if (EventWaitHandle.TryOpenExisting(WakeupEventName, out var existingEvent))
                    {
                        existingEvent.Set();
                        existingEvent.Dispose();
                    }
                }
                catch { }

                // 当前实例并未拥有互斥体所有权，绝不调用 ReleaseMutex，直接释放句柄并退出
                _mutex.Dispose();
                _mutex = null;
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
            catch { }

            // 加载配置
            Config.Load();

            // 启动主搜索窗（静默常驻右下角托盘，不弹窗打扰用户，极速待命）
            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.InitializeBackgroundTray();

            // 首次运行向导（如果是全新运行无配置，引导用户扫描已安装软件）
            if (Config.IsFirstRun)
            {
                var wizard = new WelcomeWizardWindow();
                wizard.ShowDialog();
            }

            // 启动轻量后台空闲内存维护定时器（托盘后台休眠时维持极小物理内存常驻）
            StartIdleMemoryMaintenance();
        }

        private void StartIdleMemoryMaintenance()
        {
            _idleTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.SystemIdle)
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _idleTimer.Tick += (s, e) =>
            {
                // 仅当主窗口隐藏、且没有其他打开的窗口时（即完全静默常驻托盘时），主动修剪工作集
                bool anyVisible = false;
                foreach (Window win in Windows)
                {
                    if (win.IsVisible)
                    {
                        anyVisible = true;
                        break;
                    }
                }

                if (!anyVisible)
                {
                    MemoryOptimizer.TrimMemory();
                }
            };
            _idleTimer.Start();
        }

        public static void OpenManageWindow()
        {
            foreach (Window win in Current.Windows)
            {
                if (win is ManageWindow manageWin)
                {
                    manageWin.Activate();
                    return;
                }
            }

            var newManageWin = new ManageWindow();
            newManageWin.Show();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            try
            {
                _registeredWait?.Unregister(null);
                _wakeEvent?.Dispose();
            }
            catch { }

            if (_ownsMutex && _mutex != null)
            {
                try
                {
                    _mutex.ReleaseMutex();
                }
                catch { }
                _mutex.Dispose();
                _mutex = null;
            }
        }
    }
}
