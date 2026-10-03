using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ALTRun.Services
{
    public static class MemoryOptimizer
    {
        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool EmptyWorkingSet(IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetProcessWorkingSetSize(IntPtr proc, IntPtr min, IntPtr max);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        /// <summary>
        /// 彻底回收未使用的内存与垃圾，并清空物理内存工作集，使软件常驻内存保持在极低水平（~15MB）
        /// </summary>
        public static void TrimMemory()
        {
            try
            {
                GC.Collect(2, GCCollectionMode.Forced, true, true);
                GC.WaitForPendingFinalizers();
                GC.Collect(2, GCCollectionMode.Forced, true, true);

                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    IntPtr hProc = GetCurrentProcess();
                    SetProcessWorkingSetSize(hProc, new IntPtr(-1), new IntPtr(-1));
                    EmptyWorkingSet(hProc);
                }
            }
            catch { }
        }

        /// <summary>
        /// 异步延迟修剪内存，等待当前动画或 UI 消息处理彻底完毕后再回收
        /// </summary>
        public static void TrimMemoryAsync(int delayMs = 500)
        {
            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delayMs);
                    
                    var app = System.Windows.Application.Current;
                    if (app != null && app.Dispatcher != null && !app.Dispatcher.HasShutdownStarted)
                    {
                        await app.Dispatcher.InvokeAsync(() =>
                        {
                            TrimMemory();
                        }, System.Windows.Threading.DispatcherPriority.SystemIdle);
                    }
                    else
                    {
                        TrimMemory();
                    }
                }
                catch { }
            });
        }
    }
}
