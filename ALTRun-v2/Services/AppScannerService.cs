using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using ALTRun.Models;

namespace ALTRun.Services
{
    public class ScannedAppItem
    {
        public bool IsSelected { get; set; } = true;
        public string Name { get; set; } = string.Empty;
        public string ShortCut { get; set; } = string.Empty;
        public string TargetPath { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public string WorkingDir { get; set; } = string.Empty;
        public bool IsAlreadyExists { get; set; } = false;
        public string SourcePath { get; set; } = string.Empty;
    }

    public static class AppScannerService
    {
        // 快捷方式名称及关键词黑名单（不区分大小写，包含即排除）
        private static readonly string[] NoiseKeywords = new[]
        {
            // 安装/卸载/更新
            "uninstall", "uninst", "卸载", "反安装", "卸載",
            "setup", "install", "installer", "安装向导", "配置向导", "设置向导", "configuration", "wizard", "向导",
            "update", "updater", "升级", "更新", "patch", "patcher",
            // 服务/后台进程/部署
            "server", "daemon", "service", "deploy", "deployer", "服务", "后台", "部署",
            // 帮助/文档/授权
            "help", "readme", "帮助", "说明", "手册", "manual", "license", "许可", "changelog", "更新日志", "guide", "指南", "documentation", "文档",
            // 诊断/修复/测试
            "repair", "diagnostics", "诊断", "troubleshoot", "修复", "cleaner", "doctor",
            // 反馈/崩溃上报
            "feedback", "反馈", "crash", "report", "上报", "telemetry",
            // 网页与社交链接
            "website", "官网", "网页", "homepage", "forum", "社区", "bbs"
        };

        // 知名常见软件推荐快捷键映射表（优先匹配）
        private static readonly Dictionary<string, string> WellKnownShortcutKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            { "微信", "wx" },
            { "WeChat", "wx" },
            { "企业微信", "qywx" },
            { "QQ", "qq" },
            { "腾讯会议", "tshy" },
            { "钉钉", "dd" },
            { "DingTalk", "dd" },
            { "飞书", "fs" },
            { "Feishu", "fs" },
            { "Google Chrome", "chrome" },
            { "Chrome", "chrome" },
            { "Microsoft Edge", "edge" },
            { "Edge", "edge" },
            { "Firefox", "ff" },
            { "火狐", "ff" },
            { "网易云音乐", "wyy" },
            { "QQ音乐", "qqyy" },
            { "汽水音乐", "qsyy" },
            { "酷狗音乐", "kugou" },
            { "哔哩哔哩", "bili" },
            { "bilibili", "bili" },
            { "Visual Studio Code", "code" },
            { "VS Code", "code" },
            { "Visual Studio", "vs" },
            { "Sublime Text", "subl" },
            { "Notepad++", "npp" },
            { "Typora", "typora" },
            { "Obsidian", "ob" },
            { "Postman", "postman" },
            { "Git Bash", "bash" },
            { "Windows Terminal", "wt" },
            { "PowerToys", "powertoys" },
            { "Steam", "steam" },
            { "Epic Games Launcher", "epic" },
            { "Epic", "epic" },
            { "PotPlayer", "pot" },
            { "VLC", "vlc" },
            { "Everything", "ev" },
            { "Snipaste", "snip" },
            { "PixPin", "pix" },
            { "Bandizip", "bzip" },
            { "7-Zip", "7z" },
            { "WinRAR", "rar" },
            { "百度网盘", "bdwp" },
            { "阿里云盘", "alyp" },
            { "迅雷", "xl" },
            { "Word", "word" },
            { "Excel", "excel" },
            { "PowerPoint", "ppt" },
            { "WPS Office", "wps" },
            { "WPS", "wps" },
            { "Photoshop", "ps" },
            { "Premiere", "pr" },
            { "After Effects", "ae" }
        };

        /// <summary>
        /// 扫描本机开始菜单与桌面上的快捷方式，过滤无效与垃圾项目
        /// </summary>
        public static List<ScannedAppItem> ScanInstalledApps(IEnumerable<ShortCutItem>? existingShortcuts = null)
        {
            var results = new List<ScannedAppItem>();
            var seenTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 记录现有快捷方式的路径和快捷键，防止重复
            var existingPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var existingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (existingShortcuts != null)
            {
                foreach (var item in existingShortcuts)
                {
                    if (!string.IsNullOrWhiteSpace(item.CommandLine))
                    {
                        string cleanCmd = item.CommandLine.Trim().Trim('\"');
                        existingPaths.Add(cleanCmd);
                    }
                    if (!string.IsNullOrWhiteSpace(item.ShortCut))
                    {
                        existingKeys.Add(item.ShortCut.Trim().ToLowerInvariant());
                    }
                }
            }

            // 准备待扫描的目录
            var directories = new List<string>();

            // 1. 开始菜单程序目录
            AddDirectoryIfExists(directories, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs"));
            AddDirectoryIfExists(directories, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs"));

            // 2. 桌面目录
            AddDirectoryIfExists(directories, Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory));
            AddDirectoryIfExists(directories, Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));

            foreach (var dir in directories)
            {
                try
                {
                    var lnkFiles = Directory.GetFiles(dir, "*.lnk", SearchOption.AllDirectories);
                    foreach (var lnk in lnkFiles)
                    {
                        try
                        {
                            // 快捷方式自身路径检查
                            if (IsBlacklistedPath(lnk))
                            {
                                continue;
                            }

                            string fileName = Path.GetFileNameWithoutExtension(lnk);

                            // 快捷方式名称黑名单过滤与 GUID 乱码检测
                            if (IsNoise(fileName) || IsGuidOrRandomHex(fileName))
                            {
                                continue;
                            }

                            // 解析 .lnk
                            if (!TryResolveShortcut(lnk, out string target, out string args, out string workDir))
                            {
                                continue;
                            }

                            if (string.IsNullOrWhiteSpace(target)) continue;

                            // 校验目标有效性：必须存在且是可执行文件
                            string ext = Path.GetExtension(target).ToLowerInvariant();
                            if (ext != ".exe" && ext != ".bat" && ext != ".cmd")
                            {
                                continue;
                            }

                            // 目标路径黑名单检查（如 C:\Windows\Installer\、Temp、Package Cache 等）
                            if (IsBlacklistedPath(target))
                            {
                                continue;
                            }

                            if (!File.Exists(target))
                            {
                                continue;
                            }

                            string targetFileName = Path.GetFileNameWithoutExtension(target);
                            // 目标文件名黑名单检查（如 *setup*, *server*, *deployer*, guid 等）
                            if (IsTargetNoise(targetFileName))
                            {
                                continue;
                            }

                            // 路径规范化去重
                            string normalizedTarget = Path.GetFullPath(target).ToLowerInvariant();
                            if (seenTargets.Contains(normalizedTarget))
                            {
                                continue;
                            }
                            seenTargets.Add(normalizedTarget);

                            // 友好名称清洗
                            string cleanName = CleanAppName(fileName);

                            // 若清洗后名称为空或仍是乱码，排除
                            if (string.IsNullOrWhiteSpace(cleanName) || IsGuidOrRandomHex(cleanName))
                            {
                                continue;
                            }

                            // 判断是否已经在现有快捷方式中存在
                            bool alreadyExists = existingPaths.Contains(target) || existingPaths.Contains(normalizedTarget);

                            // 智能推导快捷词
                            string shortcutKey = GenerateSmartShortcut(cleanName, existingKeys);

                            results.Add(new ScannedAppItem
                            {
                                Name = cleanName,
                                TargetPath = target,
                                Arguments = args,
                                WorkingDir = workDir,
                                ShortCut = shortcutKey,
                                SourcePath = lnk,
                                IsAlreadyExists = alreadyExists,
                                IsSelected = !alreadyExists // 已存在的默认不勾选，新发现的默认勾选
                            });
                        }
                        catch
                        {
                            // 忽略单个损坏快捷方式
                        }
                    }
                }
                catch
                {
                    // 忽略无权限目录
                }
            }

            // 按首字母与名称排序
            return results.OrderBy(r => r.IsAlreadyExists).ThenBy(r => r.Name).ToList();
        }

        private static void AddDirectoryIfExists(List<string> list, string? dir)
        {
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir) && !list.Contains(dir))
            {
                list.Add(dir);
            }
        }

        /// <summary>
        /// 黑名单路径过滤（过滤 Windows Installer 缓存目录、临时缓存目录等）
        /// </summary>
        private static bool IsBlacklistedPath(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return true;
            string lower = fullPath.ToLowerInvariant();

            // 1. Windows 系统核心目录 (C:\Windows\System32, SysWOW64 等) - 这些是系统管理与底层调试工具，不是用户已安装的第三方应用
            string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows).ToLowerInvariant();
            if (lower.StartsWith(winDir)) return true;

            // 2. Windows 管理工具与内置系统组件快捷方式源目录
            if (lower.Contains(@"windows 管理工具") || lower.Contains(@"windows administrative tools") ||
                lower.Contains(@"windows 工具") || lower.Contains(@"windows tools") ||
                lower.Contains(@"windows powershell") || lower.Contains(@"windows 系统") ||
                lower.Contains(@"windows 附件") || lower.Contains(@"windows accessories"))
            {
                return true;
            }

            // 3. Windows Installer 缓存目录（极其典型的长随机 GUID / 缓存 exe）
            if (lower.Contains(@"\windows\installer")) return true;

            // 4. 临时目录与系统安装缓存
            if (lower.Contains(@"\appdata\local\temp") || lower.Contains(@"\windows\temp") || lower.Contains(@"\temp\")) return true;
            if (lower.Contains(@"\package cache")) return true;
            if (lower.Contains(@"\installshield installation information")) return true;
            if (lower.Contains(@"\$recycle.bin")) return true;
            if (lower.Contains(@"\windows\winsxs")) return true;
            if (lower.Contains(@"\windows\servicing")) return true;

            return false;
        }

        /// <summary>
        /// 检查名称是否命中噪音关键词
        /// </summary>
        private static bool IsNoise(string name)
        {
            string lower = name.ToLowerInvariant();
            foreach (var kw in NoiseKeywords)
            {
                if (lower.Contains(kw.ToLowerInvariant()))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 检查目标可执行程序名称是否为噪音或后台进程
        /// </summary>
        private static bool IsTargetNoise(string targetFileName)
        {
            string lower = targetFileName.ToLowerInvariant();

            // 1. 安装/卸载/部署相关 (*setup*, *install*, *unins*, *deploy*, *deployer*)
            if (lower.Contains("setup") || lower.Contains("install") || lower.Contains("unins") || lower.Contains("deploy"))
            {
                return true;
            }

            // 2. 服务/守护/后台相关 (*server*, *daemon*, *service*)
            if (lower.Contains("server") || lower.Contains("daemon") || lower.Contains("service"))
            {
                return true;
            }

            // 3. 辅助进程/崩溃收集/更新/诊断相关
            if (lower.Contains("helper") || lower.Contains("assistant") || lower.Contains("agent") ||
                lower.Contains("crashpad") || lower.Contains("crashreport") || lower.Contains("feedback") ||
                lower.Contains("updater") || lower.Contains("patcher") || lower.Contains("update") ||
                lower.Contains("repair") || lower.Contains("troubleshoot") || lower.Contains("diag") ||
                lower.Contains("wizard") || lower.Contains("cleaner") || lower.Contains("host") ||
                lower.Contains("worker") || lower.Contains("elevation"))
            {
                return true;
            }

            // 4. GUID 或长十六进制字符串模式（常见于 Windows Installer 残留，如 {7131646D-...} 或 2a4f6d8c...）
            if (IsGuidOrRandomHex(lower))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检测字符串是否为 GUID 或者是系统生成的长十六进制/随机乱码字符串
        /// </summary>
        private static bool IsGuidOrRandomHex(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return false;

            // 1. 匹配标准 GUID 格式 (带或不带大括号)
            if (Regex.IsMatch(str, @"\{?[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\}?"))
            {
                return true;
            }

            // 2. 去除连字符与下划线后，若长度 >= 12 且全部由十六进制字符组成，直接判定为机器生成的哈希或缓存名
            string clean = Regex.Replace(str, @"[\-_\{\}\s]", "");
            if (clean.Length >= 12 && Regex.IsMatch(clean, @"^[0-9a-fA-F]+$"))
            {
                return true;
            }

            return false;
        }

        private static string CleanAppName(string rawName)
        {
            // 去除末尾的常见冗余后缀，如 " - 快捷方式", " (64-bit)" 等
            string cleaned = Regex.Replace(rawName, @"\s*-\s*快捷方式$", "", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"\s*\(64-bit\)$", "", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"\s*\(32-bit\)$", "", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"\s*\(x64\)$", "", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"\s*\(x86\)$", "", RegexOptions.IgnoreCase);
            return cleaned.Trim();
        }

        public static string GenerateSmartShortcut(string appName, HashSet<string> usedKeys)
        {
            string baseKey = "";

            // 1. 检查已知流行软件精准映射
            foreach (var kv in WellKnownShortcutKeywords)
            {
                if (appName.IndexOf(kv.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    baseKey = kv.Value;
                    break;
                }
            }

            // 2. 如果无精准映射，根据语言结构生成
            if (string.IsNullOrEmpty(baseKey))
            {
                // 检查是否包含汉字
                bool hasChinese = appName.Any(c => c >= 0x4E00 && c <= 0x9FA5);
                if (hasChinese)
                {
                    // 汉字取拼音首字母
                    baseKey = PinyinHelper.GetInitials(appName).ToLowerInvariant();
                }
                else
                {
                    // 纯英文/数字：若多单词取首字母，单单词取小写单词或前3~4字母
                    var words = Regex.Split(appName, @"[\s_\-\.]+").Where(w => !string.IsNullOrWhiteSpace(w)).ToArray();
                    if (words.Length > 1)
                    {
                        var sb = new StringBuilder();
                        foreach (var w in words)
                        {
                            if (char.IsLetterOrDigit(w[0]))
                            {
                                sb.Append(w[0]);
                            }
                        }
                        baseKey = sb.ToString().ToLowerInvariant();
                    }
                    else if (words.Length == 1)
                    {
                        baseKey = words[0].ToLowerInvariant();
                    }
                }
            }

            // 清洗非法字符，只保留小写字母与数字
            baseKey = Regex.Replace(baseKey, @"[^a-z0-9]", "");
            if (string.IsNullOrEmpty(baseKey))
            {
                baseKey = "app";
            }

            // 避免与已有快捷键冲突（若冲突，递增加数字）
            string candidate = baseKey;
            int counter = 2;
            while (usedKeys.Contains(candidate))
            {
                candidate = $"{baseKey}{counter}";
                counter++;
            }

            usedKeys.Add(candidate);
            return candidate;
        }

        #region Windows IShellLink 原生 COM 解析

        private static bool TryResolveShortcut(string lnkPath, out string target, out string arguments, out string workingDir)
        {
            target = string.Empty;
            arguments = string.Empty;
            workingDir = string.Empty;

            try
            {
                var shellLink = new ShellLink();
                var persistFile = (IPersistFile)shellLink;
                persistFile.Load(lnkPath, 0);

                var link = (IShellLinkW)shellLink;
                var sbPath = new StringBuilder(260);
                var findData = new WIN32_FIND_DATAW();
                link.GetPath(sbPath, sbPath.Capacity, out findData, 0);
                target = sbPath.ToString();

                var sbArgs = new StringBuilder(1024);
                link.GetArguments(sbArgs, sbArgs.Capacity);
                arguments = sbArgs.ToString();

                var sbDir = new StringBuilder(260);
                link.GetWorkingDirectory(sbDir, sbDir.Capacity);
                workingDir = sbDir.ToString();

                return !string.IsNullOrEmpty(target);
            }
            catch
            {
                return false;
            }
        }

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLink
        {
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out WIN32_FIND_DATAW pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out ushort pwHotkey);
            void SetHotkey(ushort wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
            void Resolve(IntPtr hwnd, uint fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("0000010b-0000-0000-C000-000000000046")]
        private interface IPersistFile
        {
            void GetClassID(out Guid pClassID);
            void IsDirty();
            void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
            void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
            void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
            void GetCurFile([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder ppszFileName);
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WIN32_FIND_DATAW
        {
            public uint dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        #endregion
    }
}
