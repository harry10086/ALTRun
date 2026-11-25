using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace ALTRun.Services
{
    /// <summary>
    /// 让窗口尺寸始终落在**窗口所在显示器**的工作区（已排除任务栏）内，并居中显示。
    /// 解决固定 Width/Height 在较矮屏幕上超出屏幕、顶部/底部被裁掉的问题，
    /// 以及多显示器环境下原实现（SystemParameters.WorkArea 只代表主屏）把窗口摆到主屏的问题。
    /// </summary>
    public static class WindowHelper
    {
        private const uint MONITOR_DEFAULTTONEAREST = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        public static void FitToWorkArea(Window window, double margin = 16)
        {
            if (window == null) return;

            Rect area = GetWorkArea(window);
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

        /// <summary>
        /// 取窗口所在显示器的工作区（DIP 单位）。窗口尚无句柄或查询失败时退回主屏工作区。
        /// </summary>
        private static Rect GetWorkArea(Window window)
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
                    if (monitor != IntPtr.Zero)
                    {
                        var info = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
                        if (GetMonitorInfo(monitor, ref info))
                        {
                            // GetMonitorInfo 返回物理像素，WPF 的 Window.Left/Top/Width/Height 是 DIP，
                            // 需按该窗口当前的 DPI 缩放换算，否则 125% / 150% 缩放下会偏移。
                            DpiScale dpi = VisualTreeHelper.GetDpi(window);
                            double scaleX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
                            double scaleY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;

                            return new Rect(
                                info.rcWork.Left / scaleX,
                                info.rcWork.Top / scaleY,
                                (info.rcWork.Right - info.rcWork.Left) / scaleX,
                                (info.rcWork.Bottom - info.rcWork.Top) / scaleY);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Write($"[ALTRun] WindowHelper 获取显示器工作区失败，回退主屏: {ex.Message}");
            }

            return SystemParameters.WorkArea;
        }
    }
}
