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

            string targetCmd = item.CommandLine.Trim();
            string finalArgs = string.Empty;

            // 1. 处理剪贴板宏 {%c} 或 %c
            if (targetCmd.Contains("{%c}") || targetCmd.Contains("%c"))
            {
                string clipText = SafeGetClipboardText();
                targetCmd = targetCmd.Replace("{%c}", clipText).Replace("%c", clipText);
            }

            // 2. 处理参数宏 %p
            if (targetCmd.Contains("%p"))
            {
                string encodedArg = EncodeParam(userArg, item.ParamType);
                targetCmd = targetCmd.Replace("%p", encodedArg);
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

            // 3. 展开环境变量
            targetCmd = Environment.ExpandEnvironmentVariables(targetCmd);

            // 4. 执行进程
            try
            {
                var psi = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = targetCmd
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
                if (TrySplitAndRun(targetCmd, finalArgs, item.WorkingDir, runAsAdmin))
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

        private static bool TrySplitAndRun(string fullCmd, string extraArgs, string workingDir, bool runAsAdmin)
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
                    Arguments = args
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
