using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ALTRun.Models;
using ALTRun.Services;
using WpfMessageBox = System.Windows.MessageBox;

namespace ALTRun.Views
{
    public partial class WelcomeWizardWindow : Window
    {
        public WelcomeWizardWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private async void BtnAutoImport_Click(object sender, RoutedEventArgs e)
        {
            PnlButtons.IsEnabled = false;
            PnlScanning.Visibility = Visibility.Visible;

            int count = 0;
            await Task.Run(() =>
            {
                var existing = App.Config.Settings.ShortCuts.ToList();
                var scanned = AppScannerService.ScanInstalledApps(existing);

                var existingPaths = new HashSet<string>(existing.Select(x => x.CommandLine.Trim().Trim('\"')), StringComparer.OrdinalIgnoreCase);

                foreach (var item in scanned)
                {
                    if (existingPaths.Contains(item.TargetPath)) continue;

                    var newItem = new ShortCutItem
                    {
                        Name = item.Name,
                        ShortCut = item.ShortCut.ToLowerInvariant(),
                        CommandLine = item.TargetPath,
                        WorkingDir = string.IsNullOrWhiteSpace(item.WorkingDir) ? Path.GetDirectoryName(item.TargetPath) ?? "" : item.WorkingDir,
                        Freq = 0
                    };

                    existing.Add(newItem);
                    existingPaths.Add(item.TargetPath);
                    count++;
                }

                App.Config.Settings.ShortCuts = existing;
                App.Config.RefreshPinyinCache();
                App.Config.Save();
            });

            PnlScanning.Visibility = Visibility.Collapsed;

            WpfMessageBox.Show($"初始化导入完成！已成功收录 {count} 个软件快捷方式。\n按下 Alt+R 唤醒 ALTRun 即可开始体验！", "ALTRun 准备就绪", MessageBoxButton.OK, MessageBoxImage.Information);

            SafeClose(true);
        }

        private void BtnCustomSelect_Click(object sender, RoutedEventArgs e)
        {
            var scanWin = new ScanAppsWindow
            {
                Owner = this
            };

            bool? res = scanWin.ShowDialog();
            if (res == true)
            {
                SafeClose(true);
            }
        }

        private void BtnSkip_Click(object sender, RoutedEventArgs e)
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
