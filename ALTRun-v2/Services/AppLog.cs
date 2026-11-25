using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ALTRun.Services
{
    /// <summary>
    /// 轻量文件日志。
    /// <para>
    /// 原实现只有 <see cref="Debug.WriteLine(string)"/>，而它在 Release 下会被编译器整体剔除，
    /// 也就是说发布版出问题时没有任何线索可查。这里把关键事件与异常落到
    /// <c>%LOCALAPPDATA%\ALTRun\logs\yyyy-MM-dd.log</c>，单文件超过 1MB 自动滚动为 <c>.1</c>。
    /// </para>
    /// <para>
    /// 设计原则：日志绝不能影响主流程——所有 IO 异常一律吞掉，目录不可写时直接停用（只记录一次失败原因）。
    /// </para>
    /// </summary>
    public static class AppLog
    {
        private const long MaxLogBytes = 1024 * 1024;   // 单文件上限 1MB
        private static readonly object SyncRoot = new();

        private static readonly string? LogDirectory = ResolveLogDirectory();
        private static bool _disabled;

        /// <summary>写入一条普通事件日志。</summary>
        public static void Write(string message) => Write(message, null);

        /// <summary>写入一条日志；<paramref name="ex"/> 不为空时追加异常类型与堆栈。</summary>
        public static void Write(string message, Exception? ex)
        {
#if DEBUG
            Debug.WriteLine(ex == null ? message : $"{message} | {ex.GetType().Name}: {ex.Message}");
#endif
            if (_disabled || LogDirectory == null) return;

            try
            {
                var sb = new StringBuilder(message.Length + 64);
                sb.Append(DateTime.Now.ToString("HH:mm:ss.fff")).Append("  ").Append(message);
                if (ex != null)
                {
                    sb.Append(" | ").Append(ex.GetType().Name).Append(": ").Append(ex.Message);
                    if (!string.IsNullOrEmpty(ex.StackTrace))
                    {
                        sb.AppendLine().Append(ex.StackTrace);
                    }
                }

                string file = Path.Combine(LogDirectory, $"{DateTime.Now:yyyy-MM-dd}.log");

                lock (SyncRoot)
                {
                    Directory.CreateDirectory(LogDirectory);
                    RotateIfTooLarge(file);
                    File.AppendAllText(file, sb.ToString() + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch (Exception logEx)
            {
                // 日志写不进去就彻底停用，避免每次异常都重复触发 IO 失败
                _disabled = true;
                Debug.WriteLine($"[ALTRun] 文件日志已停用: {logEx.Message}");
            }
        }

        private static string? ResolveLogDirectory()
        {
            try
            {
                string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrEmpty(root))
                {
                    return null;
                }
                return Path.Combine(root, "ALTRun", "logs");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ALTRun] 解析日志目录失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>超过上限就把当前文件滚成 .1（覆盖上一次的 .1），保证日志不会无限增长。</summary>
        private static void RotateIfTooLarge(string file)
        {
            try
            {
                var info = new FileInfo(file);
                if (!info.Exists || info.Length < MaxLogBytes) return;

                string rolled = file + ".1";
                if (File.Exists(rolled))
                {
                    File.Delete(rolled);
                }
                File.Move(file, rolled);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ALTRun] 日志滚动失败: {ex.Message}");
            }
        }
    }
}
