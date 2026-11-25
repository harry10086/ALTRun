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
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            PlaceholderText.Visibility = string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;

            // 程序内部清空输入（隐藏窗口时）不需要再搜一次
            if (_suspendSearch) return;

            // 防抖：重置定时器，连续输入只在最后一次停顿后触发一次搜索
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }

        private void PerformSearch()
        {
            // 立即执行时取消待触发的防抖，避免随后又重复搜索一次
            _searchDebounceTimer.Stop();

            string query = SearchBox.Text;
            _currentResult = SearchEngine.Search(query, App.Config.Settings.ShortCuts);

            ResultListBox.ItemsSource = _currentResult.Items;
            if (_currentResult.Items.Count > 0)
            {
                ResultListBox.SelectedIndex = 0;
            }
            ResetListScrollToTop();
        }


        /// <summary>
        /// 键盘执行类操作前调用：若还有防抖未落地，先同步执行一次，确保基于最新输入结果操作。
        /// </summary>
        private void FlushPendingSearch()
        {
            if (_searchDebounceTimer.IsEnabled)
            {
                PerformSearch();
            }
        }

        private void ResetListScrollToTop()
        {
            try
            {
                var scrollViewer = FindVisualChild<ScrollViewer>(ResultListBox);
                scrollViewer?.ScrollToTop();
                if (_currentResult.Items.Count > 0 && ResultListBox.Items.Count > 0)
                {
                    ResultListBox.ScrollIntoView(_currentResult.Items[0]);
                }
            }
            catch (Exception ex)
            {
                AppLog.Write($"[ALTRun] MainWindow.xaml.cs:609 静默异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 鼠标滚轮：每格切换一个候选项。候选全部可见时列表本身不会滚动；
        /// 候选超出可视区时 ScrollIntoView 会把新选中项滚入视野。
        /// </summary>
        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            FlushPendingSearch();
            if (ResultListBox.Items.Count == 0) return;

            int steps = Math.Max(1, Math.Abs(e.Delta) / 120);
            if (e.Delta > 0) steps = -steps; // 向上滚动选中上一项

            int current = ResultListBox.SelectedIndex;
            int target = current < 0 ? 0 : Math.Min(ResultListBox.Items.Count - 1, Math.Max(0, current + steps));

            if (current != target)
            {
                ResultListBox.SelectedIndex = target;
                ResultListBox.ScrollIntoView(ResultListBox.SelectedItem);
            }
            e.Handled = true;
        }

        private void ExecuteSelectedItem(bool runAsAdmin)
        {
            if (ResultListBox.SelectedItem is ShortCutItem item)
            {
                string param = _currentResult.ExtractedParam;
                bool success = CommandExecutor.Execute(item, param, runAsAdmin, out string? errorMessage);

                if (success)
                {
                    // 搜索结果是 Clone 副本，Freq 自增发生在副本上；按 Id 同步回原始配置项才能持久化
                    var original = App.Config.Settings.ShortCuts.FirstOrDefault(x => x.Id == item.Id);
                    if (original != null)
                    {
                        original.Freq = item.Freq;
                    }
                    App.Config.MarkDirty();
                    HideWindow();
                }
                else if (!string.IsNullOrEmpty(errorMessage))
                {
                    ShowMissingFileTip(errorMessage);
                }
            }
        }

    }
}
