using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ALTRun.Services
{
    /// <summary>
    /// 系统托盘服务：使用纯 Win32 Shell_NotifyIcon 实现，不依赖 WinForms 运行时。
    /// 托盘图标的鼠标回调消息发送到你传入的主窗口 HWND，
    /// 由 MainWindow.WndProc → ProcessWindowMessage 转发处理（左键/双击呼出、右键菜单）。
    /// </summary>
    public class TrayIconService : IDisposable
    {
        private const uint WM_TRAYICON = 0x8001;      // WM_APP + 1，应用自定义消息
        private const uint NIM_ADD = 0x00000000;
        private const uint NIM_MODIFY = 0x00000001;
        private const uint NIM_DELETE = 0x00000002;
        private const uint NIF_MESSAGE = 0x00000001;
        private const uint NIF_ICON = 0x00000002;
        private const uint NIF_TIP = 0x00000004;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int WM_RBUTTONUP = 0x0205;
        private const uint IDI_APPLICATION = 32512;
        private const uint IMAGE_ICON = 1;
        private const uint LR_LOADFROMFILE = 0x0008;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NOTIFYICONDATA
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uID;
            public uint uFlags;
            public uint uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public uint dwState;
            public uint dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;
            public uint uVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;
            public uint dwInfoFlags;
            public Guid guidItem;
            public IntPtr hBalloonIcon;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint ExtractIconEx(string lpszFile, int nIconIndex, out IntPtr phiconLarge, out IntPtr phiconSmall, uint nIcons);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadImage(IntPtr hInst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private const int SM_CXSMICON = 49;
        private const int SM_CYSMICON = 50;

        // 资源管理器（explorer.exe）重启后会广播 "TaskbarCreated" 消息，托盘图标需要重新 NIM_ADD。
        // 原 WinForms NotifyIcon 内部已处理，改为纯 Win32 实现后必须自己监听，否则 explorer 崩溃重启后图标永久消失。
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint RegisterWindowMessage(string lpString);

        private NOTIFYICONDATA _data;
        private IntPtr _ownedIcon = IntPtr.Zero;   // 本服务加载、需手动销毁的图标句柄
        private uint _taskbarCreatedMessage;       // "TaskbarCreated" 动态消息号（0 表示注册失败）
        private bool _added;

        public event Action? LeftClicked;
        public event Action? RightClicked;

        public void Initialize(IntPtr hWnd, string tooltip)
        {
            if (hWnd == IntPtr.Zero) return;

            _data = new NOTIFYICONDATA
            {
                cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                hWnd = hWnd,
                uID = 1,
                uCallbackMessage = WM_TRAYICON,
                szTip = Clip(tooltip)
            };

            _data.hIcon = LoadTrayIcon();
            _data.uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP;

            _added = Shell_NotifyIcon(NIM_ADD, ref _data);
            if (!_added)
            {
                AppLog.Write("[ALTRun] TrayIconService Shell_NotifyIcon(NIM_ADD) 失败");
            }

            // 注册任务栏重建通知（explorer.exe 重启后用来重建图标）
            try
            {
                _taskbarCreatedMessage = RegisterWindowMessage("TaskbarCreated");
            }
            catch (Exception ex)
            {
                AppLog.Write($"[ALTRun] TrayIconService 注册 TaskbarCreated 消息失败: {ex.Message}");
            }

            // 自动配置 Win11 注册表: 将托盘图标设置为始终常驻任务栏 (IsPromoted = 1)
            EnsureTrayIconPromoted();
        }

        private IntPtr LoadTrayIcon()
        {
            // 1. 优先从本程序 .exe 提取高清主图标（对应原 Icon.ExtractAssociatedIcon）
            try
            {
                string exePath = Environment.ProcessPath ?? "";
                if (File.Exists(exePath) &&
                    ExtractIconEx(exePath, 0, out var large, out var small, 1) > 0)
                {
                    if (large != IntPtr.Zero) DestroyIcon(large);
                    if (small != IntPtr.Zero)
                    {
                        _ownedIcon = small;
                        return small;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Write($"[ALTRun] TrayIconService 提取 exe 图标失败: {ex.Message}");
            }

            // 2. 其次从 ALTRun.ico 文件加载（尺寸取系统小图标尺寸，高 DPI / 125% 缩放下才不发虚）
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ALTRun.ico");
                if (File.Exists(iconPath))
                {
                    int size = GetSystemMetrics(SM_CXSMICON);
                    if (size <= 0) size = 16;
                    int sizeY = GetSystemMetrics(SM_CYSMICON);
                    if (sizeY <= 0) sizeY = size;

                    IntPtr h = LoadImage(IntPtr.Zero, iconPath, IMAGE_ICON, size, sizeY, LR_LOADFROMFILE);
                    if (h != IntPtr.Zero)
                    {
                        _ownedIcon = h;
                        return h;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Write($"[ALTRun] TrayIconService 加载 ico 失败: {ex.Message}");
            }

            // 3. 兜底：系统默认应用图标（共享资源，不由本服务销毁）
            return LoadIcon(IntPtr.Zero, new IntPtr(IDI_APPLICATION));
        }

        private static string Clip(string s) => s.Length > 63 ? s.Substring(0, 63) : s;

        public void UpdateTooltip(string tooltip)
        {
            if (!_added) return;
            _data.szTip = Clip(tooltip);
            _data.uFlags = NIF_TIP;
            Shell_NotifyIcon(NIM_MODIFY, ref _data);
        }

        /// <summary>
        /// 处理发到主窗口的托盘回调消息：左键/双击呼出主界面，右键弹出托盘菜单。
        /// 由 MainWindow.WndProc 通过 HwndSource.AddHook 转发进来。
        /// </summary>
        public void ProcessWindowMessage(int msg, IntPtr wParam, IntPtr lParam)
        {
            // 任务栏重建：shell 重启后旧的图标注册已失效，这里重新 NIM_ADD 一次（图标句柄仍然有效）
            if (_taskbarCreatedMessage != 0 && (uint)msg == _taskbarCreatedMessage)
            {
                ReAddIcon();
                return;
            }

            if (msg != WM_TRAYICON) return;
            int mouseMsg = (int)(lParam.ToInt64() & 0xFFFF);
            switch (mouseMsg)
            {
                case WM_LBUTTONUP:
                case WM_LBUTTONDBLCLK:
                    LeftClicked?.Invoke();
                    break;
                case WM_RBUTTONUP:
                    RightClicked?.Invoke();
                    break;
            }
        }

        /// <summary>
        /// 任务栏重建后重新注册托盘图标（不重新加载图标资源，复用已有句柄）。
        /// </summary>
        private void ReAddIcon()
        {
            if (_data.hWnd == IntPtr.Zero) return;

            _data.uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP;
            _added = Shell_NotifyIcon(NIM_ADD, ref _data);
            if (_added)
            {
                AppLog.Write("[ALTRun] TrayIconService 任务栏重建，托盘图标已恢复");
            }
            else
            {
                AppLog.Write("[ALTRun] TrayIconService 任务栏重建后 NIM_ADD 失败");
            }
        }

        public static void EnsureTrayIconPromoted()
        {
            try
            {
                string currentExe = Environment.ProcessPath ?? "";
                if (string.IsNullOrEmpty(currentExe)) return;

                using var notifyKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\NotifyIconSettings", true);
                if (notifyKey == null) return;

                foreach (var subKeyName in notifyKey.GetSubKeyNames())
                {
                    using var subKey = notifyKey.OpenSubKey(subKeyName, true);
                    if (subKey != null)
                    {
                        var exePath = subKey.GetValue("ExecutablePath") as string;
                        if (!string.IsNullOrEmpty(exePath) && exePath.Equals(currentExe, StringComparison.OrdinalIgnoreCase))
                        {
                            subKey.SetValue("IsPromoted", 1, Microsoft.Win32.RegistryValueKind.DWord);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Write($"[ALTRun] TrayIconService.cs 注册表常驻设置异常: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_added)
            {
                _data.uFlags = 0;
                Shell_NotifyIcon(NIM_DELETE, ref _data);
                _added = false;
            }
            if (_ownedIcon != IntPtr.Zero)
            {
                DestroyIcon(_ownedIcon);
                _ownedIcon = IntPtr.Zero;
            }
        }
    }
}
