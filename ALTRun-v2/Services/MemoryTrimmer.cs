using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace ALTRun.Services
{
    /// <summary>
    /// 隐藏态工作集裁剪。
    /// <para>
    /// 托盘常驻程序的真实内存大头是「已驻留但暂时用不到的页」——WPF 视觉树、字体缓存、
    /// 运行时 JIT 代码等。主窗口隐藏后这些页没有任何用处，却仍占着物理内存。
    /// 这里在窗口持续隐藏一段时间后调用 Win32 的 SetProcessWorkingSetSize(-1,-1)，
    /// 让系统把这些页转入待命列表（任务管理器「内存」列可从约 100MB 降到个位数），
    /// 需要时再自然换回，功能与界面完全不受影响。
    /// </para>
    /// <para>
    /// 刻意只在「主窗口与所有子窗口都不可见」时执行：向导、管理窗口、扫描窗口打开期间
    /// 一律跳过，避免正在交互的界面触发缺页造成卡顿。
    /// </para>
    /// </summary>
    internal static class MemoryTrimmer
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr dwMinimumWorkingSetSize, IntPtr dwMaximumWorkingSetSize);

        /// <summary>传 -1 表示「交给系统决定」，即标准的工作集裁剪语义。</summary>
        private static readonly IntPtr TrimToSystemChoice = new IntPtr(-1);

        private const int PollSeconds = 10;        // 轮询间隔：只读一个 IsVisible，开销可忽略
        private const int MinHiddenSeconds = 10;   // 隐藏满这么久才裁，避免「呼出又立刻隐藏」被反复裁剪
        private const int RepeatSeconds = 300;     // 长期隐藏时每 5 分钟复裁一次，抑制后台缓慢回升

        private static DispatcherTimer? _timer;
        private static Window? _owner;
        private static DateTime _hiddenSince = DateTime.MinValue;
        private static DateTime _lastTrimAt = DateTime.MinValue;

        /// <summary>挂上主窗口并开始轮询。重复调用无副作用。</summary>
        public static void Start(Window owner)
        {
            if (_timer != null) return;

            _owner = owner;
            _timer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(PollSeconds)
            };
            _timer.Tick += OnTick;
            _timer.Start();
        }

        /// <summary>退出时停掉轮询并清空状态。</summary>
        public static void Stop()
        {
            _timer?.Stop();
            _timer = null;
            _owner = null;
            _hiddenSince = DateTime.MinValue;
            _lastTrimAt = DateTime.MinValue;
        }

        private static void OnTick(object? sender, EventArgs e)
        {
            var owner = _owner;
            if (owner == null)
            {
                Stop();
                return;
            }

            if (IsAnyWindowVisible(owner))
            {
                // 只要有任何窗口可见就重置计时，下次完全隐藏后重新计 10 秒
                _hiddenSince = DateTime.MinValue;
                return;
            }

            var now = DateTime.UtcNow;
            if (_hiddenSince == DateTime.MinValue)
            {
                _hiddenSince = now;
                return;
            }

            if ((now - _hiddenSince).TotalSeconds < MinHiddenSeconds) return;
            if (_lastTrimAt != DateTime.MinValue && (now - _lastTrimAt).TotalSeconds < RepeatSeconds) return;

            Trim();
            _lastTrimAt = now;
        }

        private static bool IsAnyWindowVisible(Window owner)
        {
            if (owner.IsVisible) return true;

            var app = System.Windows.Application.Current;
            if (app == null) return false;

            foreach (Window window in app.Windows)
            {
                if (!ReferenceEquals(window, owner) && window.IsVisible) return true;
            }
            return false;
        }

        private static void Trim()
        {
            long before = ReadWorkingSet();
            try
            {
                if (!SetProcessWorkingSetSize(GetCurrentProcess(), TrimToSystemChoice, TrimToSystemChoice))
                {
                    AppLog.Write($"MemoryTrimmer 工作集裁剪失败，Win32 错误码 {Marshal.GetLastWin32Error()}");
                    return;
                }
            }
            catch (Exception ex)
            {
                AppLog.Write("MemoryTrimmer 工作集裁剪异常", ex);
                return;
            }

            long after = ReadWorkingSet();
            AppLog.Write($"MemoryTrimmer 隐藏态裁剪：工作集 {before / 1024:N0} KB → {after / 1024:N0} KB");
        }

        private static long ReadWorkingSet()
        {
            try
            {
                using var process = Process.GetCurrentProcess();
                return process.WorkingSet64;
            }
            catch (Exception ex)
            {
                AppLog.Write("MemoryTrimmer 读取工作集失败", ex);
                return 0;
            }
        }
    }
}
