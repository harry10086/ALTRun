using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using ALTRun.Models;
using ALTRun.Services;
using WpfMessageBox = System.Windows.MessageBox;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WpfSaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace ALTRun.Views
{
    public partial class ManageWindow : Window
    {
        private ObservableCollection<ShortCutItem> _viewList = new();
        private ShortCutItem? _selectedItem;

        public ManageWindow()
        {
            InitializeComponent();
            InitHotKeyControls();
            var currentTheme = ThemeManager.GetTheme(App.Config.Settings.Theme, App.Config.Settings.DarkMode);
            ApplyTheme(currentTheme);
            Loaded += ManageWindow_Loaded;
        }

        private void ManageWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 绑定主题下拉选项
            CmbThemes.ItemsSource = ThemeManager.Themes;
            var currentTheme = ThemeManager.GetTheme(App.Config.Settings.Theme, App.Config.Settings.DarkMode);
            CmbThemes.SelectedItem = ThemeManager.Themes.FirstOrDefault(x => x.Id == currentTheme.Id) ?? ThemeManager.Themes[0];
            ApplyTheme(currentTheme);

            LoadCurrentHotKey();
            RefreshGrid();
        }

        private void InitHotKeyControls()
        {
            // 初始化常用按键候选列表
            var keys = new List<string>();

            // 字母 A-Z
            for (char c = 'A'; c <= 'Z'; c++)
            {
                keys.Add(c.ToString());
            }

            // 特殊键
            keys.Add("Space");
            keys.Add("Tab");
            keys.Add("Return");
            keys.Add("OemTilde");

            // 数字 0-9
            for (int i = 0; i <= 9; i++)
            {
                keys.Add("D" + i);
            }

            // 功能键 F1-F12
            for (int i = 1; i <= 12; i++)
            {
                keys.Add("F" + i);
            }

            CmbKey.ItemsSource = keys;
        }

        private void LoadCurrentHotKey()
        {
            string current = App.Config.Settings.HotKey;
            var (modifiers, key) = MainWindow.ParseHotKey(current);
            ChkAlt.IsChecked = (modifiers & KeyModifiers.Alt) != 0;
            ChkCtrl.IsChecked = (modifiers & KeyModifiers.Control) != 0;
            ChkShift.IsChecked = (modifiers & KeyModifiers.Shift) != 0;
            ChkWin.IsChecked = (modifiers & KeyModifiers.Windows) != 0;

            string keyStr = key == Key.Space ? "Space" : key.ToString();
            CmbKey.SelectedItem = keyStr;
            if (CmbKey.SelectedIndex < 0)
            {
                CmbKey.SelectedItem = "R";
            }
        }

        private void BtnSaveHotKey_Click(object sender, RoutedEventArgs e)
        {
            KeyModifiers modifiers = KeyModifiers.None;
            if (ChkAlt.IsChecked == true) modifiers |= KeyModifiers.Alt;
            if (ChkCtrl.IsChecked == true) modifiers |= KeyModifiers.Control;
            if (ChkShift.IsChecked == true) modifiers |= KeyModifiers.Shift;
            if (ChkWin.IsChecked == true) modifiers |= KeyModifiers.Windows;

            if (modifiers == KeyModifiers.None)
            {
                WpfMessageBox.Show("请至少勾选一个修饰键（例如 Alt 或 Ctrl）！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string? selectedKeyStr = CmbKey.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedKeyStr))
            {
                WpfMessageBox.Show("请选择主按键！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Key key;
            if (selectedKeyStr == "Space")
            {
                key = Key.Space;
            }
            else if (!Enum.TryParse(selectedKeyStr, true, out key))
            {
                key = Key.R;
            }

            if (System.Windows.Application.Current.MainWindow is MainWindow mainWin)
            {
                bool success = mainWin.UpdateHotKey(modifiers, key);
                if (success)
                {
                    string hotkeyText = MainWindow.FormatHotKeyText(modifiers, key);
                    WpfMessageBox.Show($"全局唤醒热键已成功更新为: {hotkeyText}", "设置成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    WpfMessageBox.Show("热键注册失败！该按键组合可能已被系统或其他运行中的软件占用，请尝试其他组合（例如 Alt+Space 或 Ctrl+Shift+Space）。", "热键冲突", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        public void ApplyTheme(ThemeDefinition theme)
        {
            ThemeManager.ApplyTheme(this, theme);

            if (CmbThemes != null && (CmbThemes.SelectedItem as ThemeDefinition)?.Id != theme.Id)
            {
                var match = ThemeManager.Themes.FirstOrDefault(x => x.Id == theme.Id);
                if (match != null)
                {
                    CmbThemes.SelectedItem = match;
                }
            }

            // 表格行与边框特定对比配色
            if (theme.IsDark)
            {
                Resources["RowBg"] = new SolidColorBrush(theme.CardBgColor);
                Resources["RowAltBg"] = new SolidColorBrush(Color.FromRgb((byte)Math.Min(theme.CardBgColor.R + 6, 255),
                                                                         (byte)Math.Min(theme.CardBgColor.G + 6, 255),
                                                                         (byte)Math.Min(theme.CardBgColor.B + 8, 255)));
                Resources["RowHoverBg"] = new SolidColorBrush(theme.HoverBgColor);
                Resources["RowSelectedBg"] = new SolidColorBrush(theme.HighlightBgColor);
                Resources["HeaderBg"] = new SolidColorBrush(theme.BgColor);
                Resources["BtnBg"] = new SolidColorBrush(theme.CardBgColor);
                Resources["BtnFg"] = new SolidColorBrush(theme.TextColor);
                Resources["BtnBorder"] = new SolidColorBrush(theme.BorderColor);
                Resources["GridLine"] = new SolidColorBrush(Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF));
                Resources["BadgeBg"] = new SolidColorBrush(theme.BadgeBgColor);
            }
            else
            {
                Resources["RowBg"] = new SolidColorBrush(Colors.White);
                Resources["RowAltBg"] = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
                Resources["RowHoverBg"] = new SolidColorBrush(Color.FromRgb(0xEE, 0xF2, 0xF6));
                Resources["RowSelectedBg"] = new SolidColorBrush(theme.HighlightBgColor);
                Resources["HeaderBg"] = new SolidColorBrush(Color.FromRgb(0xEA, 0xED, 0xF4));
                Resources["BtnBg"] = new SolidColorBrush(Color.FromRgb(0xEE, 0xF0, 0xF4));
                Resources["BtnFg"] = new SolidColorBrush(theme.TextColor);
                Resources["BtnBorder"] = new SolidColorBrush(theme.BorderColor);
                Resources["GridLine"] = new SolidColorBrush(Color.FromArgb(0x18, 0x00, 0x00, 0x00));
                Resources["BadgeBg"] = new SolidColorBrush(theme.BadgeBgColor);
            }

            try
            {
                DwmHelper.ApplyModernWindowStyles(this, theme.IsDark);
            }
            catch { }
        }

        private void CmbThemes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbThemes.SelectedItem is ThemeDefinition selected)
            {
                if (App.Config.Settings.Theme != selected.Id)
                {
                    App.Config.Settings.Theme = selected.Id;
                    App.Config.Settings.DarkMode = selected.IsDark;
                    App.Config.Save();

                    ApplyTheme(selected);

                    if (System.Windows.Application.Current.MainWindow is MainWindow mainWin)
                    {
                        mainWin.ApplyTheme(selected);
                    }
                }
            }
        }



        private void BtnToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            var allThemes = ThemeManager.Themes;
            var current = ThemeManager.GetTheme(App.Config.Settings.Theme, App.Config.Settings.DarkMode);
            int idx = allThemes.FindIndex(x => x.Id == current.Id);
            int nextIdx = (idx + 1) % allThemes.Count;
            var nextTheme = allThemes[nextIdx];

            CmbThemes.SelectedItem = nextTheme;
        }

        private void RefreshGrid(string filter = "")
        {
            var source = App.Config.Settings.ShortCuts;
            if (!string.IsNullOrWhiteSpace(filter))
            {
                string lower = filter.ToLowerInvariant();
                source = source.Where(x => (x.ShortCut ?? "").ToLowerInvariant().Contains(lower) ||
                                           (x.Name ?? "").ToLowerInvariant().Contains(lower) ||
                                           (x.CommandLine ?? "").ToLowerInvariant().Contains(lower) ||
                                           (x.PinyinInitials ?? "").ToLowerInvariant().Contains(lower)).ToList();
            }

            _viewList = new ObservableCollection<ShortCutItem>(source);
            ShortcutGrid.ItemsSource = _viewList;
        }

        private void TxtFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshGrid(TxtFilter.Text);
        }

        private void ShortcutGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ShortcutGrid.SelectedItem is ShortCutItem item)
            {
                _selectedItem = item;
                EditShortCut.Text = item.ShortCut;
                EditName.Text = item.Name;
                EditCommandLine.Text = item.CommandLine;
            }
        }

        private void BtnBrowseFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WpfOpenFileDialog
            {
                Title = "选择要启动的程序、快捷方式或文件",
                Filter = "所有支持的文件 (*.exe;*.lnk;*.bat;*.cmd;*.*)|*.exe;*.lnk;*.bat;*.cmd;*.*|应用程序 (*.exe)|*.exe|桌面快捷方式 (*.lnk)|*.lnk|批处理脚本 (*.bat;*.cmd)|*.bat;*.cmd|所有文件 (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                string filePath = dialog.FileName;
                EditCommandLine.Text = filePath;

                // 若名称为空或是默认占位符，自动提取文件名
                if (string.IsNullOrWhiteSpace(EditName.Text) || EditName.Text == "新快捷方式")
                {
                    string name = Path.GetFileNameWithoutExtension(filePath);
                    EditName.Text = name;

                    if (string.IsNullOrWhiteSpace(EditShortCut.Text) || EditShortCut.Text == "new")
                    {
                        string initials = PinyinHelper.GetInitials(name).ToLowerInvariant();
                        EditShortCut.Text = !string.IsNullOrEmpty(initials) ? initials : name.ToLowerInvariant();
                    }
                }
            }
        }

        private void BtnBrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "选择要快速打开的文件夹目录"
            };

            if (dialog.ShowDialog(this) == true)
            {
                string folderPath = dialog.FolderName;
                EditCommandLine.Text = folderPath;

                if (string.IsNullOrWhiteSpace(EditName.Text) || EditName.Text == "新快捷方式")
                {
                    string name = Path.GetFileName(folderPath);
                    if (string.IsNullOrEmpty(name)) name = folderPath;
                    EditName.Text = name;

                    if (string.IsNullOrWhiteSpace(EditShortCut.Text) || EditShortCut.Text == "new")
                    {
                        string initials = PinyinHelper.GetInitials(name).ToLowerInvariant();
                        EditShortCut.Text = !string.IsNullOrEmpty(initials) ? initials : name.ToLowerInvariant();
                    }
                }
            }
        }

        private void BtnSaveEdit_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem != null)
            {
                _selectedItem.ShortCut = EditShortCut.Text.Trim();
                _selectedItem.Name = EditName.Text.Trim();
                _selectedItem.CommandLine = EditCommandLine.Text.Trim();
                try
                {
                    _selectedItem.PinyinInitials = PinyinHelper.GetInitials(_selectedItem.Name);
                    _selectedItem.PinyinShortcut = PinyinHelper.GetInitials(_selectedItem.ShortCut);
                }
                catch
                {
                    _selectedItem.PinyinInitials = _selectedItem.Name.ToLowerInvariant();
                    _selectedItem.PinyinShortcut = _selectedItem.ShortCut.ToLowerInvariant();
                }

                App.Config.Save();
                RefreshGrid(TxtFilter.Text);
                WpfMessageBox.Show("快捷项修改已保存！", "ALTRun", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // 若没有选中项，直接作为新增项保存
                var newItem = new ShortCutItem
                {
                    ShortCut = EditShortCut.Text.Trim(),
                    Name = EditName.Text.Trim(),
                    CommandLine = EditCommandLine.Text.Trim(),
                    Freq = 10
                };
                if (!string.IsNullOrEmpty(newItem.ShortCut) || !string.IsNullOrEmpty(newItem.Name))
                {
                    newItem.PinyinInitials = PinyinHelper.GetInitials(newItem.Name);
                    newItem.PinyinShortcut = PinyinHelper.GetInitials(newItem.ShortCut);
                    App.Config.Settings.ShortCuts.Insert(0, newItem);
                    App.Config.Save();
                    RefreshGrid(TxtFilter.Text);
                    ShortcutGrid.SelectedItem = newItem;
                    WpfMessageBox.Show("已新增快捷方式！", "ALTRun", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var newItem = new ShortCutItem
            {
                ShortCut = "new",
                Name = "新快捷方式",
                CommandLine = "",
                Freq = 10
            };

            App.Config.Settings.ShortCuts.Insert(0, newItem);
            App.Config.RefreshPinyinCache();
            App.Config.Save();
            RefreshGrid(TxtFilter.Text);

            ShortcutGrid.SelectedItem = newItem;
            EditShortCut.Focus();
            EditShortCut.SelectAll();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (ShortcutGrid.SelectedItem is ShortCutItem item)
            {
                if (WpfMessageBox.Show($"确定要删除快捷项 [{item.Name}] 吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    App.Config.Settings.ShortCuts.Remove(item);
                    App.Config.Save();
                    RefreshGrid(TxtFilter.Text);
                }
            }
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WpfOpenFileDialog
            {
                Filter = "ALTRun 列表文件 (ShortCutList.txt;FavoriteList.txt;*.txt;*.ini)|ShortCutList.txt;FavoriteList.txt;*.txt;*.ini|文本文件 (*.txt)|*.txt|INI 配置文件 (*.ini)|*.ini|所有文件 (*.*)|*.*",
                Title = "选择原版 ShortCutList.txt / FavoriteList.txt / INI 文件"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var imported = App.Config.ImportFromLegacyFile(dialog.FileName);
                    if (imported.Count > 0)
                    {
                        foreach (var newItem in imported)
                        {
                            var existing = App.Config.Settings.ShortCuts.FirstOrDefault(x =>
                                x.ShortCut.Equals(newItem.ShortCut, StringComparison.OrdinalIgnoreCase) &&
                                x.Name.Equals(newItem.Name, StringComparison.OrdinalIgnoreCase));

                            if (existing != null)
                            {
                                existing.CommandLine = newItem.CommandLine;
                                existing.Freq = Math.Max(existing.Freq, newItem.Freq);
                            }
                            else
                            {
                                App.Config.Settings.ShortCuts.Add(newItem);
                            }
                        }

                        App.Config.RefreshPinyinCache();
                        App.Config.Save();
                        RefreshGrid(TxtFilter.Text);

                        WpfMessageBox.Show($"成功解析并导入/更新了 {imported.Count} 个快捷方式！", "导入完成", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        WpfMessageBox.Show("未能从文件中解析出有效的快捷方式项，请确保文件包含有效内容。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    WpfMessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WpfSaveFileDialog
            {
                Filter = "ALTRun 列表文件 (*.txt)|*.txt|INI 配置文件 (*.ini)|*.ini",
                FileName = "ShortCutList.txt",
                Title = "导出为原版 ShortCutList.txt"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    App.Config.ExportToLegacyFile(dialog.FileName);
                    WpfMessageBox.Show($"已成功导出到: {dialog.FileName}", "导出完成", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    WpfMessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            MemoryOptimizer.TrimMemoryAsync(300);
        }
    }
}
