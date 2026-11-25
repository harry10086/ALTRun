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
        private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject? parentObj = child;
            while (parentObj != null)
            {
                if (parentObj is T parent)
                    return parent;
                if (parentObj is Visual || parentObj is System.Windows.Media.Media3D.Visual3D)
                    parentObj = VisualTreeHelper.GetParent(parentObj);
                else
                    parentObj = LogicalTreeHelper.GetParent(parentObj);
            }
            return null;
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;
                var foundChild = FindVisualChild<T>(child);
                if (foundChild != null)
                    return foundChild;
            }
            return null;
        }

        private void ResultListBox_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject dep)
            {
                if (FindVisualParent<System.Windows.Controls.Primitives.ScrollBar>(dep) != null) return;

                var item = FindVisualParent<ListBoxItem>(dep);
                if (item != null && item.DataContext is ShortCutItem shortcut)
                {
                    ResultListBox.SelectedItem = shortcut;
                    ShowItemContextMenu(shortcut, item);
                    e.Handled = true;
                }
            }
        }

        private void ShowItemContextMenu(ShortCutItem item, FrameworkElement targetElement)
        {
            var menu = new ContextMenu();
            if (TryFindResource("ModernContextMenu") is Style menuStyle)
            {
                menu.Style = menuStyle;
            }

            var menuItemStyle = TryFindResource("ModernMenuItem") as Style;

            // 辅助构建矢量 Path 图标
            UIElement CreateVectorIcon(string pathData, double size = 12.5)
            {
                var path = new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse(pathData),
                    Fill = (System.Windows.Media.Brush)FindResource("AccentBrush"),
                    Width = size,
                    Height = size,
                    Stretch = Stretch.Uniform,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                };
                return new Border
                {
                    Width = 20,
                    Height = 20,
                    CornerRadius = new CornerRadius(4),
                    Background = (System.Windows.Media.Brush)FindResource("BadgeBgBrush"),
                    Child = path
                };
            }

            // 1. 添加新条目 (现代加号图标)
            var miAdd = new MenuItem
            {
                Header = AppStrings.ContextAddNew,
                Icon = CreateVectorIcon("M19,13H13V19H11V13H5V11H11V5H13V11H19V13Z", 12),
                Style = menuItemStyle
            };
            miAdd.Click += (s, ev) =>
            {
                App.OpenManageWindow(null, addNew: true);
                HideWindow();
            };
            menu.Items.Add(miAdd);

            // 2. 编辑本条目 (现代铅笔/编辑图标)
            var miEdit = new MenuItem
            {
                Header = AppStrings.ContextEdit,
                Icon = CreateVectorIcon("M20.71,7.04C21.1,6.65 21.1,6 20.71,5.63L18.37,3.29C18,2.9 17.35,2.9 16.96,3.29L15.12,5.12L18.87,8.87M3,17.25V21H6.75L17.81,9.93L14.06,6.18L3,17.25Z", 12),
                Style = menuItemStyle
            };
            miEdit.Click += (s, ev) =>
            {
                App.OpenManageWindow(item, addNew: false);
                HideWindow();
            };
            menu.Items.Add(miEdit);

            // 3. 打开目标程序目录 (现代文件夹图标)
            var miOpenDir = new MenuItem
            {
                Header = AppStrings.ContextOpenFolder,
                Icon = CreateVectorIcon("M20,18H4V8H20M20,6H12L10,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V8C22,6.89 21.1,6 20,6Z", 12.5),
                InputGestureText = "Ctrl+D",
                Style = menuItemStyle
            };
            miOpenDir.Click += (s, ev) =>
            {
                OpenItemDirectory(item);
            };
            menu.Items.Add(miOpenDir);

            menu.PlacementTarget = targetElement;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
            menu.IsOpen = true;
        }

        private void OpenItemDirectory(ShortCutItem item)
        {
            if (item == null) return;

            string? dir = ResolveTargetDirectory(item);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{dir}\"",
                        UseShellExecute = true
                    });
                    HideWindow();
                    return;
                }
                catch (Exception ex)
                {
                    AppLog.Write($"[ALTRun] MainWindow.xaml.cs:1032 静默异常: {ex.Message}");
                }
            }

            MessageBox.Show(string.Format(AppStrings.OpenFolderFailedFormat, item.CommandLine, item.WorkingDir), AppStrings.AppName, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static string? ResolveTargetDirectory(ShortCutItem item)
        {
            if (item == null) return null;

            // 1. 如果显式配置了有效的 WorkingDir
            if (!string.IsNullOrWhiteSpace(item.WorkingDir))
            {
                string expWorkingDir = Environment.ExpandEnvironmentVariables(item.WorkingDir.Trim().Trim('\"'));
                if (Directory.Exists(expWorkingDir))
                {
                    return expWorkingDir;
                }
            }

            string cmd = (item.CommandLine ?? "").Trim();
            if (string.IsNullOrWhiteSpace(cmd)) return null;

            // 2. 剥除旧版前缀修饰符 @ / @+ / @-
            if (cmd.StartsWith("@+") || cmd.StartsWith("@-"))
            {
                cmd = cmd.Substring(2).TrimStart();
            }
            else if (cmd.StartsWith("@"))
            {
                cmd = cmd.Substring(1).TrimStart();
            }

            // 3. 展开系统环境变量
            cmd = Environment.ExpandEnvironmentVariables(cmd);

            // 4. 去除外层引号
            string targetPath = cmd;
            if (targetPath.StartsWith("\""))
            {
                int endQuote = targetPath.IndexOf('\"', 1);
                if (endQuote > 0)
                {
                    targetPath = targetPath.Substring(1, endQuote - 1).Trim();
                }
                else
                {
                    targetPath = targetPath.Trim('\"');
                }
            }

            // 5. 如果 targetPath 本身就是存在的目录
            if (Directory.Exists(targetPath))
            {
                return targetPath;
            }

            // 6. 如果 targetPath 是存在的文件
            if (File.Exists(targetPath))
            {
                return Path.GetDirectoryName(targetPath);
            }

            // 7. 处理包含参数的命令行
            string[] parts = cmd.Split(' ');
            if (parts.Length > 1)
            {
                string first = parts[0].Trim('\"');
                if (File.Exists(first))
                {
                    return Path.GetDirectoryName(first);
                }
                if (Directory.Exists(first))
                {
                    return first;
                }

                string last = parts[parts.Length - 1].Trim('\"');
                if (File.Exists(last))
                {
                    return Path.GetDirectoryName(last);
                }
                if (Directory.Exists(last))
                {
                    return last;
                }
            }

            // 8. 尝试从 Windows 注册表解析文件名
            try
            {
                string exeName = targetPath;
                if (!exeName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    exeName += ".exe";
                }

                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{exeName}");
                if (key != null)
                {
                    string? appPath = key.GetValue("") as string;
                    if (!string.IsNullOrEmpty(appPath))
                    {
                        appPath = appPath.Trim().Trim('\"');
                        if (File.Exists(appPath)) return Path.GetDirectoryName(appPath);
                    }
                    string? appDir = key.GetValue("Path") as string;
                    if (!string.IsNullOrEmpty(appDir) && Directory.Exists(appDir))
                    {
                        return appDir.Trim().Trim('\"');
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Write($"[ALTRun] MainWindow.xaml.cs:1145 静默异常: {ex.Message}");
            }

            return null;
        }

        private void ResultListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

            if (e.OriginalSource is DependencyObject dep)
            {
                if (FindVisualParent<System.Windows.Controls.Primitives.ScrollBar>(dep) != null) return;

                var item = FindVisualParent<ListBoxItem>(dep);
                if (item != null && item.DataContext is ShortCutItem shortcut)
                {
                    ResultListBox.SelectedItem = shortcut;
                    bool runAsAdmin = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
                    ExecuteSelectedItem(runAsAdmin);
                    e.Handled = true;
                }
            }
        }

        private void ResultListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (App.Config.Settings.SingleClickToRun) return;

            bool runAsAdmin = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            ExecuteSelectedItem(runAsAdmin);
        }

    }
}
