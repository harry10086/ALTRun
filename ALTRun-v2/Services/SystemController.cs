using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ALTRun.Services
{
    /// <summary>
    /// 现代化 Windows 窗口管理与系统控制服务
    /// 无需外部第三方工具，采用纯 Windows 原生 API / Shell COM 实现
    /// </summary>
    public static class SystemController
    {
        #region Win32 API 导入

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex);
            return new IntPtr(GetWindowLong32(hWnd, nIndex));
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("powrprof.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);

        [DllImport("user32.dll")]
        public static extern bool LockWorkStation();

        #endregion

        #region 常量定义

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;

        private const int SW_HIDE = 0;
        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOWMINIMIZED = 2;
        private const int SW_MAXIMIZE = 3;
        private const int SW_RESTORE = 9;

        private const uint WM_CLOSE = 0x0010;
        private const int GWL_EXSTYLE = -20;
        private const long WS_EX_TOPMOST = 0x00000008L;

        #endregion

        /// <summary>
        /// 唤醒 ALTRun 之前的上一个活动前台窗口句柄
        /// </summary>
        public static IntPtr LastForegroundWindow { get; set; } = IntPtr.Zero;

        private static readonly List<IntPtr> _hiddenWindows = new();

        /// <summary>
        /// 获取当前应操作的目标窗口
        /// </summary>
        public static IntPtr GetTargetWindow()
        {
            if (LastForegroundWindow != IntPtr.Zero && IsWindow(LastForegroundWindow))
            {
                return LastForegroundWindow;
            }
            return GetForegroundWindow();
        }

        #region 桌面与全局窗口管理

        /// <summary>
        /// 切换显示桌面 (等同于 Win + D)
        /// </summary>
        public static void ToggleDesktop()
        {
            try
            {
                Type? shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType != null)
                {
                    dynamic? shell = Activator.CreateInstance(shellType);
                    shell?.ToggleDesktop();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ToggleDesktop error: {ex.Message}");
            }
        }

        /// <summary>
        /// 最小化所有窗口 (等同于 Win + M)
        /// </summary>
        public static void MinimizeAll()
        {
            try
            {
                Type? shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType != null)
                {
                    dynamic? shell = Activator.CreateInstance(shellType);
                    shell?.MinimizeAll();
                }
            }
            catch { }
        }

        /// <summary>
        /// 还原所有窗口 (等同于 Win + Shift + M)
        /// </summary>
        public static void UndoMinimizeAll()
        {
            try
            {
                Type? shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType != null)
                {
                    dynamic? shell = Activator.CreateInstance(shellType);
                    shell?.UndoMinimizeAll();
                }
            }
            catch { }
        }

        /// <summary>
        /// 呼出系统原生“运行”对话框 (Win + R)
        /// </summary>
        public static void OpenRunDialog()
        {
            try
            {
                Type? shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType != null)
                {
                    dynamic? shell = Activator.CreateInstance(shellType);
                    shell?.FileRun();
                }
            }
            catch { }
        }

        /// <summary>
        /// 关闭所有非系统常规应用窗口
        /// </summary>
        public static void CloseAllWindows()
        {
            var currentPid = (uint)Process.GetCurrentProcess().Id;
            EnumWindows((hwnd, lParam) =>
            {
                if (IsWindow(hwnd) && IsWindowVisible(hwnd))
                {
                    GetWindowThreadProcessId(hwnd, out uint pid);
                    if (pid == currentPid) return true;

                    var sbClass = new StringBuilder(256);
                    GetClassName(hwnd, sbClass, 256);
                    string cls = sbClass.ToString();

                    if (cls == "Shell_TrayWnd" || cls == "Progman" || cls == "WorkerW" || cls == "Shell_SecondaryTrayWnd")
                        return true;

                    var sbTitle = new StringBuilder(256);
                    GetWindowText(hwnd, sbTitle, 256);
                    if (string.IsNullOrWhiteSpace(sbTitle.ToString()))
                        return true;

                    PostMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                }
                return true;
            }, IntPtr.Zero);
        }

        /// <summary>
        /// 按窗口类名批量关闭窗口 (例如 CabinetWClass 资源管理器)
        /// </summary>
        public static void CloseWindowsByClass(string targetClass)
        {
            if (string.IsNullOrWhiteSpace(targetClass)) return;
            targetClass = targetClass.Trim('\"', '”', '“', '\'', ' ');

            EnumWindows((hwnd, lParam) =>
            {
                if (IsWindow(hwnd) && IsWindowVisible(hwnd))
                {
                    var sbClass = new StringBuilder(256);
                    GetClassName(hwnd, sbClass, 256);
                    if (sbClass.ToString().Equals(targetClass, StringComparison.OrdinalIgnoreCase))
                    {
                        PostMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                    }
                }
                return true;
            }, IntPtr.Zero);
        }

        #endregion

        #region 单窗口控制 (置顶 / 最小化 / 最大化 / 关闭 / 隐藏)

        public static bool SetTopmost(IntPtr hwnd, bool topmost)
        {
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd)) return false;
            return SetWindowPos(hwnd, topmost ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }

        public static bool ToggleTopmost(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd)) return false;
            long exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
            bool isTop = (exStyle & WS_EX_TOPMOST) != 0;
            return SetTopmost(hwnd, !isTop);
        }

        public static bool MinimizeWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd)) return false;
            return ShowWindow(hwnd, SW_SHOWMINIMIZED);
        }

        public static bool MaximizeWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd)) return false;
            return ShowWindow(hwnd, SW_MAXIMIZE);
        }

        public static bool RestoreWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd)) return false;
            return ShowWindow(hwnd, SW_RESTORE);
        }

        public static bool CloseWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd)) return false;
            PostMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            return true;
        }

        public static bool HideWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd)) return false;
            if (!_hiddenWindows.Contains(hwnd))
            {
                _hiddenWindows.Add(hwnd);
            }
            return ShowWindow(hwnd, SW_HIDE);
        }

        public static void UnhideWindows()
        {
            foreach (var hwnd in _hiddenWindows)
            {
                if (IsWindow(hwnd))
                {
                    ShowWindow(hwnd, SW_SHOWNORMAL);
                }
            }
            _hiddenWindows.Clear();
        }

        public static void ShowOnlyWindow(IntPtr targetHwnd)
        {
            if (targetHwnd == IntPtr.Zero || !IsWindow(targetHwnd)) return;
            _hiddenWindows.Clear();

            var currentPid = (uint)Process.GetCurrentProcess().Id;
            EnumWindows((hwnd, lParam) =>
            {
                if (hwnd != targetHwnd && IsWindow(hwnd) && IsWindowVisible(hwnd))
                {
                    GetWindowThreadProcessId(hwnd, out uint pid);
                    if (pid == currentPid) return true;

                    var sbClass = new StringBuilder(256);
                    GetClassName(hwnd, sbClass, 256);
                    string cls = sbClass.ToString();
                    if (cls == "Shell_TrayWnd" || cls == "Progman" || cls == "WorkerW")
                        return true;

                    _hiddenWindows.Add(hwnd);
                    ShowWindow(hwnd, SW_HIDE);
                }
                return true;
            }, IntPtr.Zero);
        }

        #endregion

        #region 电源与系统锁定

        public static void Sleep()
        {
            try
            {
                SetSuspendState(false, true, false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Sleep error: {ex.Message}");
            }
        }

        public static void Lock()
        {
            try
            {
                LockWorkStation();
            }
            catch { }
        }

        #endregion

        #region 指令解析与分发

        /// <summary>
        /// 尝试识别并执行内置窗口/系统指令
        /// </summary>
        public static bool TryExecuteInternalCommand(string commandLine, string? userArg, out bool handled)
        {
            handled = false;
            if (string.IsNullOrWhiteSpace(commandLine)) return false;

            string cmd = commandLine.Trim();

            // 剥离原版修饰符 @, @+, @-
            if (cmd.StartsWith("@+") || cmd.StartsWith("@-"))
                cmd = cmd.Substring(2).TrimStart();
            else if (cmd.StartsWith("@"))
                cmd = cmd.Substring(1).TrimStart();

            // 1. 现代专用协议前缀 win: / sys:
            if (cmd.StartsWith("win:", StringComparison.OrdinalIgnoreCase) ||
                cmd.StartsWith("sys:", StringComparison.OrdinalIgnoreCase))
            {
                handled = true;
                string action = cmd.Substring(4).Trim();
                return ExecuteAction(action);
            }

            // 2. 拦截并原生执行旧版 WinCtl 指令（如 @.\WinCtl.exe MinAll / @WinCtl.exe Top 等）
            if (cmd.Contains("WinCtl", StringComparison.OrdinalIgnoreCase))
            {
                handled = true;
                return ParseAndExecuteWinCtl(cmd);
            }

            // 3. 常见直接指令别名 (如 desktop / sleep / lock)
            string lowerCmd = cmd.ToLowerInvariant();
            if (lowerCmd == "desktop" || lowerCmd == "toggledesktop")
            {
                handled = true;
                ToggleDesktop();
                return true;
            }

            if (lowerCmd.Contains("setsuspendstate") && lowerCmd.Contains("powrprof.dll"))
            {
                handled = true;
                Sleep();
                return true;
            }

            return false;
        }

        private static bool ExecuteAction(string action)
        {
            string act = action.ToLowerInvariant();
            var target = GetTargetWindow();

            switch (act)
            {
                case "desktop":
                case "toggledesktop":
                    ToggleDesktop();
                    return true;
                case "minall":
                    MinimizeAll();
                    return true;
                case "maxall":
                case "undominall":
                    UndoMinimizeAll();
                    return true;
                case "top":
                    return ToggleTopmost(target);
                case "untop":
                    return SetTopmost(target, false);
                case "min":
                case "minwin":
                    return MinimizeWindow(target);
                case "max":
                case "maxwin":
                    return MaximizeWindow(target);
                case "restore":
                    return RestoreWindow(target);
                case "close":
                case "closewin":
                    return CloseWindow(target);
                case "closeall":
                    CloseAllWindows();
                    return true;
                case "hide":
                    return HideWindow(target);
                case "unhide":
                    UnhideWindows();
                    return true;
                case "showonly":
                    ShowOnlyWindow(target);
                    return true;
                case "sleep":
                    Sleep();
                    return true;
                case "lock":
                    Lock();
                    return true;
                case "rundlg":
                case "run":
                    OpenRunDialog();
                    return true;
                default:
                    return false;
            }
        }

        private static bool ParseAndExecuteWinCtl(string fullCmd)
        {
            string lower = fullCmd.ToLowerInvariant();
            var target = GetTargetWindow();

            if (lower.Contains("minall"))
            {
                // MinAll: 原版 ALTRun 默认用作显示桌面
                ToggleDesktop();
                return true;
            }
            if (lower.Contains("max all") || lower.Contains("maxall"))
            {
                UndoMinimizeAll();
                return true;
            }
            if (lower.Contains("close all") || lower.Contains("closeall"))
            {
                CloseAllWindows();
                return true;
            }
            if (lower.Contains("close class"))
            {
                int classIdx = fullCmd.IndexOf("Class=", StringComparison.OrdinalIgnoreCase);
                if (classIdx > 0)
                {
                    string cls = fullCmd.Substring(classIdx + 6).Trim();
                    CloseWindowsByClass(cls);
                    return true;
                }
            }
            if (lower.Contains("showonly") || lower.Contains("handle!="))
            {
                ShowOnlyWindow(target);
                return true;
            }
            if (lower.Contains("unhide"))
            {
                UnhideWindows();
                return true;
            }
            if (lower.Contains("hide"))
            {
                return HideWindow(target);
            }
            if (lower.Contains("untop"))
            {
                return SetTopmost(target, false);
            }
            if (lower.Contains("top"))
            {
                return ToggleTopmost(target);
            }
            if (lower.Contains("min"))
            {
                return MinimizeWindow(target);
            }
            if (lower.Contains("max"))
            {
                return MaximizeWindow(target);
            }
            if (lower.Contains("close"))
            {
                return CloseWindow(target);
            }

            return false;
        }

        #endregion
    }
}
