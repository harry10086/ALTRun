using System;
using System.Windows;
using System.Windows.Input;

namespace ALTRun.Views
{
    /// <summary>
    /// 轻量提示窗（原 ShowMissingFileTip 的代码拼界面改为独立 XAML 控件）。
    /// 非模态、置顶于主界面上层，在屏幕工作区内上下左右居中；
    /// 高度随内容自适应，主题配色跟随全局动态资源；
    /// “确定”按钮默认聚焦，Enter / 空格 / Esc 均可关闭。
    /// </summary>
    public partial class TipWindow : Window
    {
        public TipWindow(string message, Window owner)
        {
            InitializeComponent();

            MessageText.Text = message;
            Owner = owner;

            Rect area = SystemParameters.WorkArea;
            MaxHeight = area.Height - 40;

            // Enter / 空格 / Esc 均可关闭提示窗
            PreviewKeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter || e.Key == Key.Space || e.Key == Key.Escape)
                {
                    Close();
                    e.Handled = true;
                }
            };

            // SizeToContent 下需在 Loaded 后拿到实际尺寸再定位，同时把焦点交给“确定”按钮
            Loaded += (_, _) =>
            {
                double left = area.Left + (area.Width - ActualWidth) / 2;
                double top = area.Top + (area.Height - ActualHeight) / 2;
                Left = Math.Max(area.Left, left);
                Top = Math.Max(area.Top, top);
                OkButton.Focus();
            };
        }

        private void OkButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
