using System;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace ALTRun.Services
{
    [Flags]
    public enum KeyModifiers
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8
    }

    public class GlobalHotKey : IDisposable
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;
        private readonly int _hotKeyId;
        private IntPtr _hWnd;
        private HwndSource? _source;

        public event Action? HotKeyPressed;

        public GlobalHotKey(int hotKeyId = 9886)
        {
            _hotKeyId = hotKeyId;
        }

        public bool Register(IntPtr hWnd, KeyModifiers modifiers, Key key)
        {
            uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
            return Register(hWnd, modifiers, vk);
        }

        public bool Register(IntPtr hWnd, KeyModifiers modifiers, uint vk)
        {
            Unregister();
            _hWnd = hWnd;

            if (_hWnd == IntPtr.Zero) return false;

            _source = HwndSource.FromHwnd(_hWnd);
            _source?.AddHook(HwndHook);

            return RegisterHotKey(_hWnd, _hotKeyId, (uint)modifiers, vk);
        }

        public void Unregister()
        {
            if (_source != null)
            {
                _source.RemoveHook(HwndHook);
                _source = null;
            }

            if (_hWnd != IntPtr.Zero)
            {
                UnregisterHotKey(_hWnd, _hotKeyId);
                _hWnd = IntPtr.Zero;
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == _hotKeyId)
            {
                HotKeyPressed?.Invoke();
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            Unregister();
            GC.SuppressFinalize(this);
        }
    }
}
