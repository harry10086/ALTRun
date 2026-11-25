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

        public ManageWindow(ShortCutItem? itemToEdit = null, bool addNew = false)
        {
            InitializeComponent();
            WindowHelper.FitToWorkArea(this);
            InitVersionInfo();
            InitHotKeyControls();
            var currentTheme = ThemeManager.GetTheme(App.Config.Settings.Theme, App.Config.Settings.DarkMode);
            ApplyTheme(currentTheme);

            Loaded += (s, e) =>
            {
                ManageWindow_Loaded(s, e);

                if (addNew)
                {
                    BtnAdd_Click(this, new RoutedEventArgs());
                }
                else if (itemToEdit != null)
                {
                    SelectAndFocusItem(itemToEdit);
                }
            };
        }

        public void SelectAndFocusItem(ShortCutItem item)
        {
            var match = App.Config.Settings.ShortCuts.FirstOrDefault(x => x.Id == item.Id ||
                (x.ShortCut == item.ShortCut && x.Name == item.Name && x.CommandLine == item.CommandLine));

            if (match != null)
            {
                TxtFilter.Text = string.Empty;
                RefreshGrid();
                ShortcutGrid.SelectedItem = match;
                ShortcutGrid.ScrollIntoView(match);
                _selectedItem = match;
                EditShortCut.Text = match.ShortCut;
                EditName.Text = match.Name;
                EditCommandLine.Text = match.CommandLine;
                EditShortCut.Focus();
                EditShortCut.SelectAll();
            }
        }

        private void InitVersionInfo()
        {
            try
            {
                var ver = typeof(ManageWindow).Assembly.GetName().Version;
                string verStr = ver != null ? $"v{ver.Major}.{ver.Minor}.{ver.Build}" : "v2.0.8";
                Title = string.Format(AppStrings.ManageTitleFormat, verStr);
            }
            catch
            {
                Title = AppStrings.ManageTitleFallback;
            }
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
                WpfMessageBox.Show(AppStrings.HotKeyNeedModifier, AppStrings.MsgBoxTitleHint, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string? selectedKeyStr = CmbKey.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedKeyStr))
            {
                WpfMessageBox.Show(AppStrings.HotKeyNeedMainKey, AppStrings.MsgBoxTitleHint, MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    WpfMessageBox.Show(string.Format(AppStrings.HotKeyUpdatedFormat, hotkeyText), AppStrings.HotKeyUpdatedTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    WpfMessageBox.Show(AppStrings.HotKeyConflict, AppStrings.HotKeyConflictTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
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
            catch (Exception ex)
            {
                AppLog.Write($"[ALTRun] ManageWindow.xaml.cs:236 静默异常: {ex.Message}");
            }
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
                Title = AppStrings.SelectProgramTitle,
                Filter = AppStrings.ProgramFileFilter
            };

            if (dialog.ShowDialog() == true)
            {
                string filePath = dialog.FileName;
                EditCommandLine.Text = filePath;

                // 若名称为空或是默认占位符，自动提取文件名
                if (string.IsNullOrWhiteSpace(EditName.Text) || EditName.Text == AppStrings.NewShortCutName)
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
                Title = AppStrings.SelectFolderTitle
            };

            if (dialog.ShowDialog(this) == true)
            {
                string folderPath = dialog.FolderName;
                EditCommandLine.Text = folderPath;

                if (string.IsNullOrWhiteSpace(EditName.Text) || EditName.Text == AppStrings.NewShortCutName)
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

                // 必须走 RefreshPinyinCache：它同时刷新拼音首字母与 4 个小写缓存（LowerShortCut / LowerName /
                // LowerPinyinInitials / LowerPinyinShortcut）。只更新 PinyinInitials/PinyinShortcut 会漏掉小写缓存，
                // 导致改名后主界面仍用旧关键词匹配——新名字搜不到，旧关键词却能搜到（搜索结果与显示内容不一致）。
                App.Config.RefreshPinyinCache();
                App.Config.Save();
                RefreshGrid(TxtFilter.Text);
                WpfMessageBox.Show(AppStrings.SavedMessage, AppStrings.AppName, MessageBoxButton.OK, MessageBoxImage.Information);
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
                    App.Config.Settings.ShortCuts.Insert(0, newItem);
                    App.Config.RefreshPinyinCache();   // 新增项同样补齐小写缓存，保证立刻可被新名称/新快捷键搜到
                    App.Config.Save();
                    RefreshGrid(TxtFilter.Text);
                    ShortcutGrid.SelectedItem = newItem;
                    WpfMessageBox.Show(AppStrings.AddedMessage, AppStrings.AppName, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        public void BtnAdd_Click_External()
        {
            BtnAdd_Click(this, new RoutedEventArgs());
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var newItem = new ShortCutItem
            {
                ShortCut = "new",
                Name = AppStrings.NewShortCutName,
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

        private void BtnScanApps_Click(object sender, RoutedEventArgs e)
        {
            var scanWin = new ScanAppsWindow
            {
                Owner = this
            };

            if (scanWin.ShowDialog() == true)
            {
                RefreshGrid(TxtFilter.Text);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (ShortcutGrid.SelectedItem is ShortCutItem item)
            {
                if (WpfMessageBox.Show(string.Format(AppStrings.DeleteConfirmFormat, item.Name), AppStrings.DeleteConfirmTitle, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
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
                Filter = AppStrings.LegacyImportFilter,
                Title = AppStrings.SelectLegacyTitle
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
                            // 手工编辑过的配置可能出现 null 字段，旧写法 x.ShortCut.Equals(...) 会抛空引用 → "导入失败"
                            var existing = App.Config.Settings.ShortCuts.FirstOrDefault(x =>
                                string.Equals(x.ShortCut, newItem.ShortCut, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(x.Name, newItem.Name, StringComparison.OrdinalIgnoreCase));

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

                        WpfMessageBox.Show(string.Format(AppStrings.ImportSuccessFormat, imported.Count), AppStrings.ImportTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        WpfMessageBox.Show(AppStrings.ImportEmpty, AppStrings.MsgBoxTitleHint, MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    WpfMessageBox.Show(AppStrings.ImportFailPrefix + ex.Message, AppStrings.MsgBoxTitleError, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WpfSaveFileDialog
            {
                Filter = AppStrings.LegacyExportFilter,
                FileName = "ShortCutList.txt",
                Title = AppStrings.ExportLegacyTitle
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    App.Config.ExportToLegacyFile(dialog.FileName);
                    WpfMessageBox.Show(string.Format(AppStrings.ExportSuccessFormat, dialog.FileName), AppStrings.ExportTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    WpfMessageBox.Show(AppStrings.ExportFailPrefix + ex.Message, AppStrings.MsgBoxTitleError, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnCheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            var btn = BtnCheckUpdate;
            string originalContent = btn.Content?.ToString() ?? AppStrings.UpdateBtnDefault;
            btn.IsEnabled = false;
            btn.Content = AppStrings.UpdateBtnChecking;

            try
            {
                var info = await UpdateCheckerService.CheckForUpdatesAsync();

                if (info.HasUpdate)
                {
                    string msg = string.Format(AppStrings.UpdateNewVersionBodyFormat, info.CurrentVersion, info.LatestVersion);

                    var result = WpfMessageBox.Show(this, msg, AppStrings.UpdateTitleSoftware, MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (result == MessageBoxResult.Yes)
                    {
                        UpdateCheckerService.OpenUrl(info.ReleaseUrl);
                    }
                }
                else
                {
                    string statusDesc = string.IsNullOrEmpty(info.ErrorMessage)
                        ? AppStrings.UpdateLatest
                        : AppStrings.UpdateNoticePrefix + info.ErrorMessage;

                    string msg = string.Format(AppStrings.UpdateLatestWithNoticeBodyFormat,
                        info.CurrentVersion, UpdateCheckerService.RepoUrl, statusDesc);

                    var result = WpfMessageBox.Show(this, msg, AppStrings.UpdateTitleVersion, MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (result == MessageBoxResult.Yes)
                    {
                        UpdateCheckerService.OpenUrl(UpdateCheckerService.RepoUrl);
                    }
                }
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show(this, AppStrings.UpdateErrorPrefix + ex.Message, AppStrings.MsgBoxTitleHint, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                btn.IsEnabled = true;
                btn.Content = originalContent;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }
    }
}
