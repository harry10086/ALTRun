using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using ALTRun.Models;
using ALTRun.Services;
using ContextMenu = System.Windows.Controls.ContextMenu;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;
using Color = System.Windows.Media.Color;

namespace ALTRun.Views
{
    public partial class MainWindow : Window
    {
        public void InitializeBackgroundTray()
        {
            var helper = new WindowInteropHelper(this);
            helper.EnsureHandle();
            InitializeSystemIntegration();
        }

        private void InitializeSystemIntegration()
        {
            if (_isSystemIntegrationInitialized) return;
            _isSystemIntegrationInitialized = true;

            var helper = new WindowInteropHelper(this);
            var hwnd = helper.Handle;
            if (hwnd == IntPtr.Zero) return;

            // 1. 应用 DWM 原生圆角与沉浸式暗色模式特性
            try
            {
                var currentTheme = ThemeManager.GetTheme(App.Config.Settings.Theme, App.Config.Settings.DarkMode);
                DwmHelper.ApplyModernWindowStyles(this, currentTheme.IsDark);
            }
            catch (Exception ex)
            {
                AppLog.Write($"[ALTRun] MainWindow.xaml.cs:110 静默异常: {ex.Message}");
            }

            // 2. 初始化系统托盘服务
            try
            {
                string hotKeyStr = App.Config.Settings.HotKey;
                _trayIconService = new TrayIconService();
                _trayIconService.LeftClicked += ShowAndActivate;
                _trayIconService.RightClicked += ShowTrayContextMenu;
                _trayIconService.Initialize(hwnd, string.Format(AppStrings.TrayTooltipFormat, hotKeyStr));

                var source = HwndSource.FromHwnd(hwnd);
                source?.AddHook(WndProc);
            }
            catch (Exception ex)
            {
                AppLog.Write($"[ALTRun] MainWindow.xaml.cs:124 静默异常: {ex.Message}");
            }

            // 2. 注册全局热键 (从配置读取)
            RegisterConfiguredHotKey(hwnd);

            // 3. 校验并同步开机自启动设置 (启动文件夹与注册表双重保障)
            try
            {
                if (App.Config.Settings.AutoRun)
                {
                    AutoRunService.ApplyAutoRun(true);
                }
            }
            catch (Exception ex)
            {
                AppLog.Write($"[ALTRun] MainWindow.xaml.cs:137 静默异常: {ex.Message}");
            }
        }

        // 托盘右键菜单缓存：右键是高频操作，原实现每次都新建 ContextMenu + 2 个 DispatcherTimer + 十余个事件，
        // 既产生 GC 压力，也让主题勾选状态难以维护。这里改为只创建一次，每次打开只刷新动态状态。
        private ContextMenu? _trayMenu;
        private MenuItem? _trayShowItem;
        private MenuItem? _trayAutoRunItem;
        private readonly System.Collections.Generic.Dictionary<string, MenuItem> _trayThemeItems = new(StringComparer.OrdinalIgnoreCase);
        private System.Windows.Threading.DispatcherTimer? _themeCloseTimer;
        private System.Windows.Threading.DispatcherTimer? _switchTimer;
        private MenuItem? _pendingSwitchItem;

        private void ShowTrayContextMenu()
        {
            Dispatcher.Invoke(() =>
            {
                EnsureTrayMenu();
                RefreshTrayMenuState();

                var helper = new WindowInteropHelper(this);
                SetForegroundWindow(helper.Handle);
                _trayMenu!.IsOpen = true;
            });
        }

        /// <summary>首次右键时构造托盘菜单，之后复用同一实例。</summary>
        private void EnsureTrayMenu()
        {
            if (_trayMenu != null) return;

            var menu = new ContextMenu();

            _trayShowItem = new MenuItem();
            _trayShowItem.Click += (s, e) => ShowAndActivate();
            menu.Items.Add(_trayShowItem);

            var itemManage = new MenuItem { Header = AppStrings.MenuManage };
            itemManage.Click += (s, e) => App.OpenManageWindow();
            menu.Items.Add(itemManage);

            var itemScan = new MenuItem { Header = AppStrings.MenuScan };
            itemScan.Click += (s, e) => new ScanAppsWindow().Show();
            menu.Items.Add(itemScan);

            menu.Items.Add(new Separator());

            var themeMenu = new MenuItem { Header = AppStrings.MenuTheme };
            themeMenu.Items.Add(new MenuItem { Header = AppStrings.MenuDarkHeader, IsEnabled = false, FontSize = 12 });
            foreach (var th in ThemeManager.Themes.Where(t => t.IsDark))
            {
                AddThemeItem(themeMenu, th);
            }

            themeMenu.Items.Add(new Separator());
            themeMenu.Items.Add(new MenuItem { Header = AppStrings.MenuLightHeader, IsEnabled = false, FontSize = 12 });
            foreach (var th in ThemeManager.Themes.Where(t => !t.IsDark))
            {
                AddThemeItem(themeMenu, th);
            }
            menu.Items.Add(themeMenu);

            _trayAutoRunItem = new MenuItem { Header = AppStrings.MenuAutoRun };
            _trayAutoRunItem.Click += (s, e) => ToggleAutoRun(_trayAutoRunItem);
            menu.Items.Add(_trayAutoRunItem);

            var itemCheckUpdate = new MenuItem { Header = AppStrings.MenuCheckUpdate };
            itemCheckUpdate.Click += async (s, e) => await CheckUpdateFromMenuAsync();
            menu.Items.Add(itemCheckUpdate);

            menu.Items.Add(new Separator());

            var itemExit = new MenuItem { Header = AppStrings.MenuExit };
            itemExit.Click += (s, e) => System.Windows.Application.Current.Shutdown();
            menu.Items.Add(itemExit);

            // 子菜单斜向平滑移动防抖 (解决鼠标斜切掠过相邻项时子菜单闪退的问题)，定时器与菜单一起复用
            _themeCloseTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(450) };
            _themeCloseTimer.Tick += (ts, te) =>
            {
                _themeCloseTimer.Stop();
                if (!themeMenu.IsMouseOver && !IsSubmenuMouseOver(themeMenu))
                {
                    themeMenu.IsSubmenuOpen = false;
                }
            };

            _switchTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
            _switchTimer.Tick += (ts, te) =>
            {
                _switchTimer.Stop();
                var pending = _pendingSwitchItem;
                if (pending != null && pending.IsMouseOver)
                {
                    themeMenu.IsSubmenuOpen = false;
                    pending.Focus();
                }
            };

            themeMenu.MouseEnter += (s, e) =>
            {
                _themeCloseTimer?.Stop();
                themeMenu.IsSubmenuOpen = true;
            };

            themeMenu.MouseLeave += (s, e) =>
            {
                _themeCloseTimer?.Stop();
                _themeCloseTimer?.Start();
            };

            foreach (var obj in themeMenu.Items)
            {
                if (obj is MenuItem mi)
                {
                    mi.MouseEnter += (s, e) =>
                    {
                        _themeCloseTimer?.Stop();
                        themeMenu.IsSubmenuOpen = true;
                    };
                }
            }

            foreach (var obj in menu.Items)
            {
                if (obj is MenuItem item && item != themeMenu)
                {
                    item.PreviewMouseMove += (s, e) =>
                    {
                        if (!themeMenu.IsSubmenuOpen) return;

                        e.Handled = true;
                        if (ReferenceEquals(_pendingSwitchItem, item)) return;

                        _pendingSwitchItem = item;
                        _switchTimer?.Stop();
                        _switchTimer?.Start();
                    };

                    item.MouseLeave += (s, e) =>
                    {
                        if (ReferenceEquals(_pendingSwitchItem, item))
                        {
                            _pendingSwitchItem = null;
                        }
                    };
                }
            }

            menu.Closed += (s, e) =>
            {
                _themeCloseTimer?.Stop();
                _switchTimer?.Stop();
                _pendingSwitchItem = null;
                themeMenu.IsSubmenuOpen = false;
            };

            _trayMenu = menu;
        }

        /// <summary>主题项与菜单一起创建一次，之后只切换勾选状态。</summary>
        private void AddThemeItem(MenuItem themeMenu, ThemeDefinition theme)
        {
            var item = CreateThemeMenuItem(theme, App.Config.Settings.Theme, 13.5);
            _trayThemeItems[theme.Id] = item;
            themeMenu.Items.Add(item);
        }

        /// <summary>刷新每次打开都需要变化的动态状态（热键文案、自启动勾选、当前主题勾选）。</summary>
        private void RefreshTrayMenuState()
        {
            if (_trayShowItem != null)
            {
                _trayShowItem.Header = string.Format(AppStrings.MenuShowSearchFormat, EffectiveHotKeyText);
            }

            if (_trayAutoRunItem != null)
            {
                _trayAutoRunItem.IsChecked = App.Config.Settings.AutoRun;
            }

            string currentThemeId = App.Config.Settings.Theme;
            foreach (var pair in _trayThemeItems)
            {
                pair.Value.IsChecked = pair.Key.Equals(currentThemeId, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static bool IsSubmenuMouseOver(MenuItem parent)
        {
            foreach (var it in parent.Items)
            {
                if (it is MenuItem mi && (mi.IsMouseOver || mi.IsHighlighted))
                    return true;
            }
            return false;
        }

        private async System.Threading.Tasks.Task CheckUpdateFromMenuAsync()
        {
            try
            {
                var info = await UpdateCheckerService.CheckForUpdatesAsync();
                if (info.HasUpdate)
                {
                    string msg = string.Format(AppStrings.UpdateNewVersionBodyFormat, info.CurrentVersion, info.LatestVersion);
                    if (MessageBox.Show(msg, AppStrings.UpdateTitleSoftware, MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    {
                        UpdateCheckerService.OpenUrl(info.ReleaseUrl);
                    }
                }
                else
                {
                    string msg = string.Format(AppStrings.UpdateLatestBodyFormat, info.CurrentVersion, UpdateCheckerService.RepoUrl);
                    if (MessageBox.Show(msg, AppStrings.UpdateTitleVersion, MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    {
                        UpdateCheckerService.OpenUrl(UpdateCheckerService.RepoUrl);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(AppStrings.UpdateErrorPrefix + ex.Message, AppStrings.MsgBoxTitleHint, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ToggleAutoRun(MenuItem menuItem)
        {
            try
            {
                App.Config.Settings.AutoRun = !App.Config.Settings.AutoRun;
                App.Config.Save();

                AutoRunService.ApplyAutoRun(App.Config.Settings.AutoRun);

                menuItem.IsChecked = App.Config.Settings.AutoRun;
            }
            catch (Exception ex)
            {
                AppLog.Write($"[ALTRun] MainWindow.xaml.cs:499 静默异常: {ex.Message}");
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            _trayIconService?.ProcessWindowMessage(msg, wParam, lParam);
            return IntPtr.Zero;
        }

    }
}
