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
        /// <summary>
        /// 显示“文件未找到”提示：非模态、置顶于主界面上层，在屏幕工作区内上下左右居中；
        /// 高度随内容自适应，确保“确定”按钮完整显示；按钮默认聚焦，空格/Enter 均可关闭；
        /// 打开期间抑制主界面的失焦自动隐藏。
        /// </summary>
        private void ShowMissingFileTip(string message)
        {
            // 已经有一个提示窗时只把它激活，不重复弹出
            if (_missingFileTip != null && _missingFileTip.IsLoaded)
            {
                _missingFileTip.Activate();
                return;
            }

            // 提示界面改为独立 XAML 控件 TipWindow：主题配色、居中、聚焦、键盘关闭均在其内部处理
            var tip = new TipWindow(message, this);

            _missingFileTip = tip;
            _suppressHideOnDeactivate = true;
            tip.Closed += (_, _) =>
            {
                _missingFileTip = null;
                _suppressHideOnDeactivate = false;
                if (IsLoaded)
                {
                    Activate();
                    SearchBox.Focus();
                }
            };

            tip.Show();
        }

    }
}
