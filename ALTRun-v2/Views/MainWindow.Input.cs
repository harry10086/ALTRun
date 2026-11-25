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
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 任何按键决策前先把待处理的防抖搜索落地，保证基于最新输入结果操作
            FlushPendingSearch();

            if (e.Key == Key.Escape)
            {
                // 第一次 Esc：清空输入但保留界面；第二次（输入本来就是空的）Esc：隐藏界面
                if (!string.IsNullOrEmpty(SearchBox.Text))
                {
                    SearchBox.Text = string.Empty;
                    SearchBox.Focus();
                    e.Handled = true;
                    return;
                }

                HideWindow();
                e.Handled = true;
                return;
            }

            // 输入后按空格 = 执行当前选中项（Shift+Space 仍可输入普通空格）
            if (e.Key == Key.Space &&
                (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift &&
                SearchBox.IsKeyboardFocused &&
                SearchBox.SelectionLength == 0 &&
                SearchBox.CaretIndex == SearchBox.Text.Length &&
                !string.IsNullOrWhiteSpace(SearchBox.Text) &&
                !SearchBox.Text.TrimStart().StartsWith(">") &&
                !SearchBox.Text.TrimStart().StartsWith("=") &&
                ResultListBox.SelectedItem is ShortCutItem)
            {
                bool runAsAdmin = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
                ExecuteSelectedItem(runAsAdmin);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.D && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (ResultListBox.SelectedItem is ShortCutItem selected)
                {
                    OpenItemDirectory(selected);
                    e.Handled = true;
                    return;
                }
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

            // ── 数字键直达触发机制 (支持 1~9 以及 0 表示第 10 项) ──
            Key actualKey = e.Key;
            if (actualKey == Key.System) actualKey = e.SystemKey;
            if (actualKey == Key.ImeProcessed) actualKey = e.ImeProcessedKey;

            int digit = -1;
            char digitChar = '\0';
            if (actualKey >= Key.D1 && actualKey <= Key.D9)
            {
                digit = actualKey - Key.D1 + 1;
                digitChar = (char)('0' + digit);
            }
            else if (actualKey == Key.D0)
            {
                digit = 10;
                digitChar = '0';
            }
            else if (actualKey >= Key.NumPad1 && actualKey <= Key.NumPad9)
            {
                digit = actualKey - Key.NumPad1 + 1;
                digitChar = (char)('0' + digit);
            }
            else if (actualKey == Key.NumPad0)
            {
                digit = 10;
                digitChar = '0';
            }

            if (digit > 0)
            {
                bool hasAlt = (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;
                bool hasCtrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
                bool hasShift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

                // 1. 显式修饰键直达: Alt + 数字 或 Ctrl + 数字 (最高优先级，100% 确认意图为选择序号)
                if (hasAlt || (hasCtrl && !hasShift))
                {
                    int targetIndex = digit - 1;
                    if (targetIndex >= 0 && targetIndex < _currentResult.Items.Count)
                    {
                        ResultListBox.SelectedIndex = targetIndex;
                        bool runAsAdmin = hasCtrl;
                        ExecuteSelectedItem(runAsAdmin);
                        e.Handled = true;
                        return;
                    }
                }

                // 2. 单数字键智能判定:
                // 如果输入的数字在快捷方式名称/快捷键中有匹配，则显示匹配项；若无任何匹配，则作为序号直达运行对应项
                if (Keyboard.Modifiers == ModifierKeys.None)
                {
                    string currentText = SearchBox.SelectionLength == SearchBox.Text.Length ? string.Empty : SearchBox.Text;

                    // 算式模式(=)和终端命令行模式(>)豁免，保证用户能正常输入数字算式与命令
                    if (!currentText.StartsWith(">") && !currentText.StartsWith("="))
                    {
                        // 预测如果把当前数字追加到搜索词中
                        string predictedQuery = currentText + digitChar;

                        // 检索整个快捷方式库是否包含或匹配该数字组合 (如 12306, 1hshutdown, c2 等)
                        var predictedResult = SearchEngine.Search(predictedQuery, App.Config.Settings.ShortCuts);

                        // 如果有匹配的快捷方式: 放行输入到搜索框中展示匹配项
                        if (predictedResult.Items.Count > 0)
                        {
                            return;
                        }

                        // 如果没有任何快捷方式匹配该数字组合: 100% 为序号直达操作
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
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject dep)
            {
                if (FindVisualParent<System.Windows.Controls.TextBox>(dep) != null ||
                    FindVisualParent<System.Windows.Controls.Button>(dep) != null ||
                    FindVisualParent<System.Windows.Controls.Primitives.ScrollBar>(dep) != null ||
                    FindVisualParent<ListBoxItem>(dep) != null)
                {
                    return;
                }
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                try
                {
                    DragMove();
                }
                catch (Exception ex)
                {
                    AppLog.Write($"[ALTRun] MainWindow.xaml.cs:883 静默异常: {ex.Message}");
                }
            }
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
                                MessageBox.Show(string.Format(AppStrings.ImportSuccessFormat, imported.Count), AppStrings.AppName, MessageBoxButton.OK, MessageBoxImage.Information);
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

    }
}
