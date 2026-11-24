using System;
using System.Windows;

namespace ALTRun.Services
{
    /// <summary>
    /// 让窗口尺寸始终落在当前显示器工作区（已排除任务栏）内，并居中显示。
    /// 解决固定 Width/Height 在较矮屏幕上超出屏幕、顶部/底部被裁掉的问题。
    /// </summary>
    public static class WindowHelper
    {
        public static void FitToWorkArea(Window window, double margin = 16)
        {
            if (window == null) return;

            Rect area = SystemParameters.WorkArea;
            if (area.IsEmpty || area.Width <= 0 || area.Height <= 0) return;

            double maxW = Math.Max(320, area.Width - margin * 2);
            double maxH = Math.Max(240, area.Height - margin * 2);

            // 最小尺寸本身也不能大于屏幕，否则小屏幕上依然会溢出
            double minW = Math.Min(window.MinWidth, maxW);
            double minH = Math.Min(window.MinHeight, maxH);
            window.MinWidth = minW;
            window.MinHeight = minH;
            window.MaxWidth = maxW;
            window.MaxHeight = maxH;

            double w = double.IsNaN(window.Width) ? minW : window.Width;
            double h = double.IsNaN(window.Height) ? minH : window.Height;
            window.Width = Math.Min(Math.Max(w, minW), maxW);
            window.Height = Math.Min(Math.Max(h, minH), maxH);

            // 手动定位，避免 CenterScreen 在窗口高于工作区时把 Top 算成负数
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = area.Left + Math.Max(0, (area.Width - window.Width) / 2);
            window.Top = area.Top + Math.Max(0, (area.Height - window.Height) / 2);
        }
    }
}
