using System;
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
        private GlobalHotKey? _globalHotKey;
        private TrayIconService? _trayIconService;
        private SearchResult _currentResult = new();

        private bool _isSystemIntegrationInitialized = false;

        public MainWindow()
        {
            InitializeComponent();
            ApplyTheme(ThemeManager.GetTheme(App.Config.Settings.Theme, App.Config.Settings.DarkMode));
            SourceInitialized += (s, e) => InitializeSystemIntegration();
            Loaded += MainWindow_Loaded;
            Closing += (s, e) =>
            {
                e.Cancel = true;
                HideWindow();
            };
        }

        public void InitializeBackgroundTray()
        {
            var helper = new WindowInteropHelper(this);
            helper.EnsureHandle();
            InitializeSystemIntegration();
            MemoryOptimizer.TrimMemoryAsync(1000);
        }

        private void InitializeSystemIntegration()
        {
            if (_isSystemIntegrationInitialized) return;
            _isSystemIntegrationInitialized = true;

            var helper = new WindowInteropHelper(this);
            var hwnd = helper.Handle;
            if (hwnd == IntPtr.Zero) return;

            // 1. 初始化系统托盘服务
            try
            {
                string hotKeyStr = App.Config.Settings.HotKey;
                _trayIconService = new TrayIconService();
                _trayIconService.LeftClicked += ShowAndActivate;
                _trayIconService.RightClicked += ShowTrayContextMenu;
                _trayIconService.Initialize(hwnd, $"ALTRun v2.0 - 快速启动 ({hotKeyStr})");

                var source = HwndSource.FromHwnd(hwnd);
                source?.AddHook(WndProc);
            }
            catch { }

            // 2. 注册全局热键 (从配置读取)
            RegisterConfiguredHotKey(hwnd);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeSystemIntegration();
            PerformSearch();
        }

        private void RegisterConfiguredHotKey(IntPtr hwnd)
        {
            try
            {
                _globalHotKey = new GlobalHotKey();
                _globalHotKey.HotKeyPressed += ShowAndActivate;

                var (modifiers, key) = ParseHotKey(App.Config.Settings.HotKey);
                bool success = _globalHotKey.Register(hwnd, modifiers, key);
                if (!success && (modifiers != KeyModifiers.Alt || key != Key.R))
                {
                    _globalHotKey.Register(hwnd, KeyModifiers.Alt, Key.R);
                }
            }
            catch { }
        }

        public bool UpdateHotKey(KeyModifiers modifiers, Key key)
        {
            var helper = new WindowInteropHelper(this);
            var hwnd = helper.Handle;
            if (hwnd == IntPtr.Zero) return false;

            _globalHotKey ??= new GlobalHotKey();
            _globalHotKey.HotKeyPressed -= ShowAndActivate;
            _globalHotKey.HotKeyPressed += ShowAndActivate;

            bool success = _globalHotKey.Register(hwnd, modifiers, key);
            if (success)
            {
                string hotkeyText = FormatHotKeyText(modifiers, key);
                App.Config.Settings.HotKey = hotkeyText;
                App.Config.Save();

                _trayIconService?.UpdateTooltip($"ALTRun v2.0 - 快速启动 ({hotkeyText})");
            }
            return success;
        }

        public static (KeyModifiers modifiers, Key key) ParseHotKey(string hotkeyStr)
        {
            KeyModifiers modifiers = KeyModifiers.None;
            Key key = Key.R;

            if (string.IsNullOrWhiteSpace(hotkeyStr))
                return (KeyModifiers.Alt, Key.R);

            var parts = hotkeyStr.Split(new[] { '+', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                string p = part.Trim();
                if (p.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                    modifiers |= KeyModifiers.Alt;
                else if (p.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || p.Equals("Control", StringComparison.OrdinalIgnoreCase))
                    modifiers |= KeyModifiers.Control;
                else if (p.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                    modifiers |= KeyModifiers.Shift;
                else if (p.Equals("Win", StringComparison.OrdinalIgnoreCase) || p.Equals("Windows", StringComparison.OrdinalIgnoreCase))
                    modifiers |= KeyModifiers.Windows;
                else if (Enum.TryParse<Key>(p, true, out var k))
                    key = k;
                else if (p.Equals("Space", StringComparison.OrdinalIgnoreCase))
                    key = Key.Space;
            }

            if (modifiers == KeyModifiers.None) modifiers = KeyModifiers.Alt;
            return (modifiers, key);
        }

        public static string FormatHotKeyText(KeyModifiers modifiers, Key key)
        {
            var parts = new System.Collections.Generic.List<string>();
            if ((modifiers & KeyModifiers.Control) != 0) parts.Add("Ctrl");
            if ((modifiers & KeyModifiers.Alt) != 0) parts.Add("Alt");
            if ((modifiers & KeyModifiers.Shift) != 0) parts.Add("Shift");
            if ((modifiers & KeyModifiers.Windows) != 0) parts.Add("Win");
            parts.Add(key == Key.Space ? "Space" : key.ToString());
            return string.Join(" + ", parts);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            _trayIconService?.ProcessWindowMessage(msg, wParam, lParam);
            return IntPtr.Zero;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        public void ForceForeground()
        {
            try
            {
                var helper = new WindowInteropHelper(this);
                IntPtr hWnd = helper.Handle;
                if (hWnd != IntPtr.Zero)
                {
                    keybd_event(0, 0, 0, UIntPtr.Zero);
                    SetForegroundWindow(hWnd);
                }
            }
            catch { }
        }

        private void ShowTrayContextMenu()
        {
            Dispatcher.Invoke(() =>
            {
                var menu = new ContextMenu();

                var itemShow = new MenuItem { Header = $"显示搜索框 ({App.Config.Settings.HotKey})" };
                itemShow.Click += (s, e) => ShowAndActivate();
                menu.Items.Add(itemShow);

                var itemManage = new MenuItem { Header = "快捷方式管理..." };
                itemManage.Click += (s, e) => App.OpenManageWindow();
                menu.Items.Add(itemManage);

                menu.Items.Add(new Separator());

                var themeMenu = new MenuItem { Header = "🎨 外观主题风格" };
                string currentThemeId = App.Config.Settings.Theme;

                var darkHeader = new MenuItem { Header = "── 🌙 暗色系列 ──", IsEnabled = false };
                themeMenu.Items.Add(darkHeader);
                foreach (var th in ThemeManager.Themes.Where(t => t.IsDark))
                {
                    var itemTh = new MenuItem
                    {
                        Header = $"{th.Icon} {th.Name}  ({th.Description})",
                        IsChecked = th.Id.Equals(currentThemeId, StringComparison.OrdinalIgnoreCase)
                    };
                    itemTh.Click += (s, e) => SetTheme(th.Id);
                    themeMenu.Items.Add(itemTh);
                }

                themeMenu.Items.Add(new Separator());

                var lightHeader = new MenuItem { Header = "── ☀️ 浅色系列 ──", IsEnabled = false };
                themeMenu.Items.Add(lightHeader);
                foreach (var th in ThemeManager.Themes.Where(t => !t.IsDark))
                {
                    var itemTh = new MenuItem
                    {
                        Header = $"{th.Icon} {th.Name}  ({th.Description})",
                        IsChecked = th.Id.Equals(currentThemeId, StringComparison.OrdinalIgnoreCase)
                    };
                    itemTh.Click += (s, e) => SetTheme(th.Id);
                    themeMenu.Items.Add(itemTh);
                }
                menu.Items.Add(themeMenu);

                var itemAutoRun = new MenuItem { Header = "开机自启动", IsChecked = App.Config.Settings.AutoRun };
                itemAutoRun.Click += (s, e) => ToggleAutoRun(itemAutoRun);
                menu.Items.Add(itemAutoRun);

                menu.Items.Add(new Separator());

                var itemExit = new MenuItem { Header = "退出 ALTRun" };
                itemExit.Click += (s, e) => System.Windows.Application.Current.Shutdown();
                menu.Items.Add(itemExit);

                var helper = new WindowInteropHelper(this);
                SetForegroundWindow(helper.Handle);
                menu.IsOpen = true;
            });
        }

        private void ToggleAutoRun(MenuItem menuItem)
        {
            try
            {
                App.Config.Settings.AutoRun = !App.Config.Settings.AutoRun;
                App.Config.Save();

                string runKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(runKey, true);
                if (key != null)
                {
                    if (App.Config.Settings.AutoRun)
                    {
                        string exePath = Environment.ProcessPath ?? "";
                        key.SetValue("ALTRun", $"\"{exePath}\"");
                    }
                    else
                    {
                        key.DeleteValue("ALTRun", false);
                    }
                }

                menuItem.IsChecked = App.Config.Settings.AutoRun;
            }
            catch { }
        }

        public void SetTheme(string themeId)
        {
            var theme = ThemeManager.GetTheme(themeId);
            App.Config.Settings.Theme = theme.Id;
            App.Config.Settings.DarkMode = theme.IsDark;
            App.Config.Save();
            ApplyTheme(theme);
        }

        public void ToggleTheme()
        {
            var allThemes = ThemeManager.Themes;
            var current = ThemeManager.GetTheme(App.Config.Settings.Theme, App.Config.Settings.DarkMode);
            int idx = allThemes.FindIndex(x => x.Id == current.Id);
            int nextIdx = (idx + 1) % allThemes.Count;
            SetTheme(allThemes[nextIdx].Id);
        }

        public void ApplyTheme(ThemeDefinition theme)
        {
            ThemeManager.ApplyTheme(this, theme);
            ThemeManager.ApplyThemeToAll(theme);
        }

        public void ShowAndActivate()
        {
            Dispatcher.Invoke(() =>
            {
                var workArea = SystemParameters.WorkArea;
                Left = (workArea.Width - Width) / 2 + workArea.Left;
                Top = workArea.Height * 0.22 + workArea.Top;

                Show();
                WindowState = WindowState.Normal;
                Activate();
                ForceForeground();
                SearchBox.Focus();
                SearchBox.SelectAll();
            });
        }

        public void HideWindow()
        {
            SearchBox.Text = string.Empty;
            Hide();
            MemoryOptimizer.TrimMemoryAsync(300);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            PlaceholderText.Visibility = string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            PerformSearch();
        }

        private void PerformSearch()
        {
            string query = SearchBox.Text;
            _currentResult = SearchEngine.Search(query, App.Config.Settings.ShortCuts);

            ResultListBox.ItemsSource = _currentResult.Items;
            if (_currentResult.Items.Count > 0)
            {
                ResultListBox.SelectedIndex = 0;
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                HideWindow();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Tab)
            {
                if (ResultListBox.SelectedItem is ShortCutItem selected)
                {
                    SearchBox.Text = selected.ShortCut + " ";
                    SearchBox.CaretIndex = SearchBox.Text.Length;
                }
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Down)
            {
                if (ResultListBox.SelectedIndex < ResultListBox.Items.Count - 1)
                {
                    ResultListBox.SelectedIndex++;
                    ResultListBox.ScrollIntoView(ResultListBox.SelectedItem);
                }
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Up)
            {
                if (ResultListBox.SelectedIndex > 0)
                {
                    ResultListBox.SelectedIndex--;
                    ResultListBox.ScrollIntoView(ResultListBox.SelectedItem);
                }
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter)
            {
                bool runAsAdmin = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
                ExecuteSelectedItem(runAsAdmin);
                e.Handled = true;
                return;
            }

            // 数字键直达触发 (1~9, 0)
            if (Keyboard.Modifiers == ModifierKeys.None && SearchBox.Text.Length > 0 && !SearchBox.Text.StartsWith(">") && !SearchBox.Text.StartsWith("="))
            {
                int digit = -1;
                if (e.Key >= Key.D1 && e.Key <= Key.D9)
                {
                    digit = e.Key - Key.D1 + 1;
                }
                else if (e.Key == Key.D0)
                {
                    digit = 10;
                }

                if (digit > 0 && !int.TryParse(SearchBox.Text, out _))
                {
                    int targetIndex = digit - 1;
                    if (targetIndex >= 0 && targetIndex < _currentResult.Items.Count)
                    {
                        ResultListBox.SelectedIndex = targetIndex;
                        ExecuteSelectedItem(false);
                        e.Handled = true;
                        return;
                    }
                }
            }
        }

        private void ExecuteSelectedItem(bool runAsAdmin)
        {
            if (ResultListBox.SelectedItem is ShortCutItem item)
            {
                string param = _currentResult.ExtractedParam;
                bool success = CommandExecutor.Execute(item, param, runAsAdmin);

                if (success)
                {
                    App.Config.Save();
                    HideWindow();
                }
            }
        }

        private void ResultListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ExecuteSelectedItem(false);
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (App.Config.Settings.HideWhenLostFocus)
            {
                HideWindow();
            }
        }

        private void BtnTheme_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            string currentThemeId = App.Config.Settings.Theme;

            var darkHeader = new MenuItem { Header = "── 🌙 暗色系列 ──", IsEnabled = false, FontSize = 12 };
            menu.Items.Add(darkHeader);
            foreach (var th in ThemeManager.Themes.Where(t => t.IsDark))
            {
                var itemTh = new MenuItem
                {
                    Header = $"{th.Icon}  {th.Name}  ({th.Description})",
                    IsChecked = th.Id.Equals(currentThemeId, StringComparison.OrdinalIgnoreCase),
                    FontSize = 14
                };
                itemTh.Click += (s, ev) => SetTheme(th.Id);
                menu.Items.Add(itemTh);
            }

            menu.Items.Add(new Separator());

            var lightHeader = new MenuItem { Header = "── ☀️ 浅色系列 ──", IsEnabled = false, FontSize = 12 };
            menu.Items.Add(lightHeader);
            foreach (var th in ThemeManager.Themes.Where(t => !t.IsDark))
            {
                var itemTh = new MenuItem
                {
                    Header = $"{th.Icon}  {th.Name}  ({th.Description})",
                    IsChecked = th.Id.Equals(currentThemeId, StringComparison.OrdinalIgnoreCase),
                    FontSize = 14
                };
                itemTh.Click += (s, ev) => SetTheme(th.Id);
                menu.Items.Add(itemTh);
            }

            menu.PlacementTarget = BtnTheme;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        private void BtnManage_Click(object sender, RoutedEventArgs e)
        {
            App.OpenManageWindow();
        }

        private void Window_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[]? files = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
                if (files != null && files.Length > 0)
                {
                    foreach (var file in files)
                    {
                        string fileName = Path.GetFileName(file);
                        if (fileName.Equals("ShortCutList.txt", StringComparison.OrdinalIgnoreCase) ||
                            fileName.EndsWith(".ini", StringComparison.OrdinalIgnoreCase))
                        {
                            var imported = App.Config.ImportFromLegacyFile(file);
                            if (imported.Count > 0)
                            {
                                App.Config.Settings.ShortCuts.AddRange(imported);
                                App.Config.RefreshPinyinCache();
                                App.Config.Save();
                                PerformSearch();
                                MessageBox.Show($"成功导入 {imported.Count} 个快捷方式！", "ALTRun", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            continue;
                        }

                        string baseName = Path.GetFileNameWithoutExtension(file);
                        string shortCutKey = baseName.Length >= 4 ? baseName.Substring(0, 4).ToLowerInvariant() : baseName.ToLowerInvariant();

                        var newItem = new ShortCutItem
                        {
                            ShortCut = shortCutKey,
                            Name = baseName,
                            CommandLine = file,
                            Freq = 10
                        };

                        App.Config.Settings.ShortCuts.Insert(0, newItem);
                    }

                    App.Config.RefreshPinyinCache();
                    App.Config.Save();
                    PerformSearch();
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _trayIconService?.Dispose();
            _globalHotKey?.Dispose();
            base.OnClosed(e);
        }
    }
}
