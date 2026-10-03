using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ALTRun.Services
{
    public static class DwmHelper
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        // Win11 窗口圆角设置
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2; // 圆角
        private const int DWMWCP_ROUNDSMALL = 3; // 小圆角

        // 沉浸式深色模式开关 (Win10 1903+ & Win11)
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public static void ApplyModernWindowStyles(Window window, bool isDarkMode = true)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            // 1. 开启 Win11 圆角
            try
            {
                int cornerVal = DWMWCP_ROUND;
                DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerVal, sizeof(int));
            }
            catch { }

            // 2. 开启沉浸式深色模式
            try
            {
                int darkVal = isDarkMode ? 1 : 0;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkVal, sizeof(int));
            }
            catch { }
        }
    }
}
