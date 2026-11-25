using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using ALTRun.Models;
using ALTRun.Services;
using WpfMessageBox = System.Windows.MessageBox;

namespace ALTRun.Views
{
    public partial class ScanAppsWindow : Window
    {
        private List<ScannedAppItem> _allScannedItems = new();
        private ICollectionView? _collectionView;

        public int ImportedCount { get; private set; } = 0;

        public ScanAppsWindow()
        {
            InitializeComponent();
            WindowHelper.FitToWorkArea(this);
            ApplyCurrentThemeColors();
            Loaded += async (s, e) => await StartScanAsync();
        }

        private void ApplyCurrentThemeColors()
        {
            try
            {
                var theme = ThemeManager.GetTheme(App.Config.Settings.Theme, App.Config.Settings.DarkMode);
                ThemeManager.ApplyTheme(this, theme);

                Resources["WindowBg"] = new SolidColorBrush(theme.BgColor);
                Resources["CardBg"] = new SolidColorBrush(theme.CardBgColor);
                Resources["RowBg"] = new SolidColorBrush(theme.CardBgColor);
                Resources["BorderBrush"] = new SolidColorBrush(theme.BorderColor);
                Resources["TextPrimary"] = new SolidColorBrush(theme.TextColor);
                Resources["TextSecondary"] = new SolidColorBrush(theme.TextMutedColor);
                Resources["TextMuted"] = new SolidColorBrush(theme.TextMutedColor);
                Resources["AccentBrush"] = new SolidColorBrush(theme.AccentColor);
            }
            catch (Exception ex)
            {
                AppLog.Write($"[ALTRun] ScanAppsWindow.xaml.cs:47 静默异常: {ex.Message}");
            }
        }

        private async Task StartScanAsync()
        {
            PnlLoading.Visibility = Visibility.Visible;
            GridApps.Visibility = Visibility.Collapsed;
            BtnImportSelected.IsEnabled = false;

            var existing = App.Config.Settings.ShortCuts.ToList();

            // 异步后台扫描，确保 UI 线程完全不卡顿
            _allScannedItems = await Task.Run(() => AppScannerService.ScanInstalledApps(existing));

            PnlLoading.Visibility = Visibility.Collapsed;
            GridApps.Visibility = Visibility.Visible;
            BtnImportSelected.IsEnabled = true;

            _collectionView = CollectionViewSource.GetDefaultView(_allScannedItems);
            _collectionView.Filter = FilterPredicate;
            GridApps.ItemsSource = _collectionView;

            UpdateStatusText();
        }

        private bool FilterPredicate(object obj)
        {
            if (obj is not ScannedAppItem item) return false;
            string filter = TxtFilter.Text.Trim();
            if (string.IsNullOrEmpty(filter)) return true;

            return item.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                   item.ShortCut.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                   item.TargetPath.Contains(filter, StringComparison.OrdinalIgnoreCase);
        }

        private void TxtFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            _collectionView?.Refresh();
            UpdateStatusText();
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _allScannedItems)
            {
                if (!item.IsAlreadyExists)
                {
                    item.IsSelected = true;
                }
            }
            _collectionView?.Refresh();
            UpdateStatusText();
        }

        private void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _allScannedItems)
            {
                item.IsSelected = false;
            }
            _collectionView?.Refresh();
            UpdateStatusText();
        }

        private async void BtnRescan_Click(object sender, RoutedEventArgs e)
        {
            await StartScanAsync();
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatusText();
        }

        private void UpdateStatusText()
        {
            int total = _allScannedItems.Count;
            int selected = _allScannedItems.Count(x => x.IsSelected);
            int newCount = _allScannedItems.Count(x => !x.IsAlreadyExists);

            TxtStatus.Text = string.Format(AppStrings.ScanStatusFormat, total, newCount, selected);
            BtnImportSelected.Content = string.Format(AppStrings.ImportSelectedButtonFormat, selected);
            BtnImportSelected.IsEnabled = selected > 0;
        }

        private void BtnImportSelected_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = _allScannedItems.Where(x => x.IsSelected).ToList();
            if (selectedItems.Count == 0)
            {
                WpfMessageBox.Show(AppStrings.PleaseSelectSoftware, AppStrings.TipTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int count = 0;
            try
            {
                var currentList = App.Config.Settings.ShortCuts;
                // CommandLine 可能因手工编辑配置而为 null，直接 .Trim() 会抛空引用（整个导入按钮失灵）
                var existingPaths = new HashSet<string>(
                    currentList.Select(x => (x.CommandLine ?? "").Trim().Trim('\"')),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var item in selectedItems)
                {
                    if (existingPaths.Contains(item.TargetPath))
                    {
                        continue; // 避免重复
                    }

                    var newItem = new ShortCutItem
                    {
                        Name = item.Name,
                        ShortCut = (item.ShortCut ?? "").ToLowerInvariant(),
                        CommandLine = item.TargetPath,
                        WorkingDir = string.IsNullOrWhiteSpace(item.WorkingDir) ? System.IO.Path.GetDirectoryName(item.TargetPath) ?? "" : item.WorkingDir,
                        Freq = 0
                    };

                    currentList.Add(newItem);
                    existingPaths.Add(item.TargetPath);
                    count++;
                }

                if (count > 0)
                {
                    App.Config.RefreshPinyinCache();
                    App.Config.Save();
                }
            }
            catch (Exception ex)
            {
                AppLog.Write($"[ALTRun] ScanAppsWindow 导入已扫描软件失败: {ex.Message}");
                WpfMessageBox.Show(AppStrings.ImportFailPrefix + ex.Message, AppStrings.MsgBoxTitleError, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (count > 0)
            {
                ImportedCount = count;
                WpfMessageBox.Show(string.Format(AppStrings.ImportSelectedSuccessFormat, count), AppStrings.ImportSelectedSuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                SafeClose(true);
            }
            else
            {
                WpfMessageBox.Show(AppStrings.AlreadyImported, AppStrings.TipTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            SafeClose(false);
        }

        private void SafeClose(bool? dialogResult = null)
        {
            try
            {
                DialogResult = dialogResult;
            }
            catch
            {
                Close();
            }
        }
    }
}
