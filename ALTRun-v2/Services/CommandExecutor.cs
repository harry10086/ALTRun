using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows;
using ALTRun.Models;

namespace ALTRun.Services
{
    public static class CommandExecutor
    {
        public static bool Execute(ShortCutItem item, string userArg, bool runAsAdmin = false)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.CommandLine))
                return false;

            // 特殊模式：即时数学计算结果
            if (item.CommandLine.StartsWith("COPY:"))
            {
                string textToCopy = item.CommandLine.Substring(5);
                SafeSetClipboard(textToCopy);
                return true;
            }

            // 特殊模式：命令行内联执行 (> cmd)
            if (item.CommandLine.StartsWith("RUNCMD:"))
            {
                string cmdToRun = item.CommandLine.Substring(7);
                return RunInTerminal(cmdToRun, runAsAdmin);
            }

            // 特殊模式：原生系统与窗口管理指令 (包含对历史 WinCtl 外部工具的兼容原生接管)
            if (SystemController.TryExecuteInternalCommand(item.CommandLine, userArg, out bool handled))
            {
                if (handled)
                {
                    item.Freq++;
                    return true;
                }
            }

            string targetCmd = item.CommandLine.Trim();
            string finalArgs = string.Empty;

            // 处理原版旧窗口模式修饰符 @ / @+ / @-
            var windowStyle = ProcessWindowStyle.Normal;
            bool createNoWindow = false;

            if (targetCmd.StartsWith("@+"))
            {
                windowStyle = ProcessWindowStyle.Maximized;
                targetCmd = targetCmd.Substring(2).TrimStart();
            }
            else if (targetCmd.StartsWith("@-"))
            {
                windowStyle = ProcessWindowStyle.Minimized;
                targetCmd = targetCmd.Substring(2).TrimStart();
            }
            else if (targetCmd.StartsWith("@"))
            {
                windowStyle = ProcessWindowStyle.Hidden;
                createNoWindow = true;
                targetCmd = targetCmd.Substring(1).TrimStart();
            }

            // 再次检测剥除 @ 后的系统内置指令
            if (SystemController.TryExecuteInternalCommand(targetCmd, userArg, out handled))
            {
                if (handled)
                {
                    item.Freq++;
                    return true;
                }
            }

            // 1. 处理剪贴板宏 {%c} 或 %c
            if (targetCmd.Contains("{%c}") || targetCmd.Contains("%c"))
            {
                string clipText = SafeGetClipboardText();
                targetCmd = targetCmd.Replace("{%c}", clipText).Replace("%c", clipText);
            }

            // 2. 处理参数宏 {%p} 或 %p
            if (targetCmd.Contains("{%p}") || targetCmd.Contains("%p"))
            {
                if (string.IsNullOrWhiteSpace(userArg) && (targetCmd == "{%p}" || targetCmd == "%p"))
                {
                    SystemController.OpenRunDialog();
                    item.Freq++;
                    return true;
                }

                string encodedArg = EncodeParam(userArg, item.ParamType);
                targetCmd = targetCmd.Replace("{%p}", encodedArg).Replace("%p", encodedArg);
            }
            else if (!string.IsNullOrWhiteSpace(userArg))
            {
                // 如果没有显式 %p 占位符，且为网址或文件，则作为后续参数
                if (targetCmd.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    targetCmd.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    targetCmd += EncodeParam(userArg, item.ParamType);
                }
                else
                {
                    finalArgs = userArg;
                }
            }

            // 3. 处理 CLSID 虚拟文件夹路径（例如我的电脑 ::{20D04FE0-3AEA-1069-A2D8-08002B30309D}）
            if (targetCmd.StartsWith("::{"))
            {
                finalArgs = targetCmd;
                targetCmd = "explorer.exe";
            }

            // 4. 展开环境变量
            targetCmd = Environment.ExpandEnvironmentVariables(targetCmd);

            // 5. 执行进程
            try
            {
                var psi = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = targetCmd,
                    WindowStyle = windowStyle,
                    CreateNoWindow = createNoWindow
                };

                if (!string.IsNullOrWhiteSpace(finalArgs))
                {
                    psi.Arguments = finalArgs;
                }

                if (!string.IsNullOrWhiteSpace(item.WorkingDir) && Directory.Exists(item.WorkingDir))
                {
                    psi.WorkingDirectory = item.WorkingDir;
                }

                if (runAsAdmin)
                {
                    psi.Verb = "runas";
                }

                Process.Start(psi);
                item.Freq++;
                return true;
            }
            catch (Exception ex)
            {
                // 如果直接启动失败（例如带有参数被写在同一行），尝试拆分 FileName 和 Arguments
                if (TrySplitAndRun(targetCmd, finalArgs, item.WorkingDir, runAsAdmin, windowStyle, createNoWindow))
                {
                    item.Freq++;
                    return true;
                }

                Debug.WriteLine($"Failed to execute: {targetCmd}, error: {ex.Message}");
                return false;
            }
        }

        private static bool RunInTerminal(string command, bool runAsAdmin)
        {
            try
            {
                // 优先使用 Windows Terminal (wt.exe)，不存在则回退至 cmd.exe
                bool hasWt = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft\\WindowsApps\\wt.exe"));

                var psi = new ProcessStartInfo
                {
                    UseShellExecute = true
                };

                if (hasWt)
                {
                    psi.FileName = "wt.exe";
                    psi.Arguments = $"cmd.exe /k \"{command}\"";
                }
                else
                {
                    psi.FileName = "cmd.exe";
                    psi.Arguments = $"/k \"{command}\"";
                }

                if (runAsAdmin)
                {
                    psi.Verb = "runas";
                }

                Process.Start(psi);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TrySplitAndRun(string fullCmd, string extraArgs, string workingDir, bool runAsAdmin, ProcessWindowStyle windowStyle = ProcessWindowStyle.Normal, bool createNoWindow = false)
        {
            try
            {
                fullCmd = fullCmd.Trim();
                string exe;
                string args;

                if (fullCmd.StartsWith("\""))
                {
                    int secondQuote = fullCmd.IndexOf('\"', 1);
                    if (secondQuote > 0)
                    {
                        exe = fullCmd.Substring(1, secondQuote - 1);
                        args = fullCmd.Substring(secondQuote + 1).Trim();
                    }
                    else
                    {
                        exe = fullCmd.Trim('\"');
                        args = "";
                    }
                }
                else
                {
                    int firstSpace = fullCmd.IndexOf(' ');
                    if (firstSpace > 0)
                    {
                        exe = fullCmd.Substring(0, firstSpace);
                        args = fullCmd.Substring(firstSpace + 1).Trim();
                    }
                    else
                    {
                        exe = fullCmd;
                        args = "";
                    }
                }

                if (!string.IsNullOrWhiteSpace(extraArgs))
                {
                    args = string.IsNullOrWhiteSpace(args) ? extraArgs : $"{args} {extraArgs}";
                }

                var psi = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = exe,
                    Arguments = args,
                    WindowStyle = windowStyle,
                    CreateNoWindow = createNoWindow
                };

                if (!string.IsNullOrWhiteSpace(workingDir) && Directory.Exists(workingDir))
                {
                    psi.WorkingDirectory = workingDir;
                }

                if (runAsAdmin)
                {
                    psi.Verb = "runas";
                }

                Process.Start(psi);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string EncodeParam(string param, ParamType paramType)
        {
            if (string.IsNullOrEmpty(param)) return string.Empty;

            return paramType switch
            {
                ParamType.ptURLQuery => WebUtility.UrlEncode(param),
                ParamType.ptUTF8Query => Uri.EscapeDataString(param),
                _ => param
            };
        }

        // 带重试的安全剪贴板读取，彻底避免 Win11 剪贴板锁竞争异常
        public static string SafeGetClipboardText()
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    if (System.Windows.Clipboard.ContainsText())
                    {
                        return System.Windows.Clipboard.GetText();
                    }
                    return string.Empty;
                }
                catch
                {
                    Thread.Sleep(20);
                }
            }
            return string.Empty;
        }

        // 带重试的安全剪贴板写入
        public static void SafeSetClipboard(string text)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    System.Windows.Clipboard.SetText(text);
                    return;
                }
                catch
                {
                    Thread.Sleep(20);
                }
            }
        }
    }
}
