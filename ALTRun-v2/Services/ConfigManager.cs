using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using ALTRun.Models;

namespace ALTRun.Services
{
    public class AppSettings
    {
        public string HotKey { get; set; } = "Alt + R";
        public bool AutoRun { get; set; } = true;
        public string Theme { get; set; } = "obsidian";
        public bool DarkMode { get; set; } = true;
        public double WindowWidth { get; set; } = 640;
        public bool HideWhenLostFocus { get; set; } = true;
        public bool RunAsAdminShortcut { get; set; } = true;
        public List<ShortCutItem> ShortCuts { get; set; } = new();
    }

    public class ConfigManager
    {
        private readonly string _appDir;
        private readonly string _jsonConfigPath;

        public AppSettings Settings { get; private set; } = new();

        public ConfigManager()
        {
            _appDir = AppDomain.CurrentDomain.BaseDirectory;
            _jsonConfigPath = Path.Combine(_appDir, "ALTRun_Config.json");
        }

        public void Load()
        {
            // 1. 如果存在现代 JSON 配置，优先读取
            if (File.Exists(_jsonConfigPath))
            {
                try
                {
                    string json = File.ReadAllText(_jsonConfigPath, Encoding.UTF8);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null && settings.ShortCuts.Count > 0)
                    {
                        if (string.IsNullOrEmpty(settings.Theme))
                        {
                            settings.Theme = settings.DarkMode ? "obsidian" : "pearl";
                        }
                        Settings = settings;
                        RefreshPinyinCache();
                        return;
                    }
                }
                catch { }
            }

            // 2. 自动检测旧版原版 ShortCutList.txt 或 ShortCut.ini
            string[] possiblePaths = new[]
            {
                Path.Combine(_appDir, "ShortCutList.txt"),
                Path.Combine(Directory.GetParent(_appDir)?.FullName ?? _appDir, "ShortCutList.txt"),
                @"D:\GitHub\ALTRun\Bin\ShortCutList.txt",
                @"D:\GitHub\ALTRun\ShortCutList.txt",
                Path.Combine(_appDir, "ShortCut.ini"),
                Path.Combine(Directory.GetParent(_appDir)?.FullName ?? _appDir, "ShortCut.ini")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    var imported = ImportFromLegacyFile(path);
                    if (imported.Count > 0)
                    {
                        Settings.ShortCuts = imported;

                        // 尝试加载对应的 FavoriteList.txt
                        string favPath = Path.Combine(Path.GetDirectoryName(path) ?? "", "FavoriteList.txt");
                        ApplyFavoriteList(favPath);

                        RefreshPinyinCache();
                        Save();
                        return;
                    }
                }
            }

            // 3. 首次启动加载内置 Win10/11 现代快捷列表
            Settings.ShortCuts = GenerateModernDefaultShortcuts();
            RefreshPinyinCache();
            Save();
        }

        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(Settings, options);
                File.WriteAllText(_jsonConfigPath, json, Encoding.UTF8);
            }
            catch { }
        }

        public void RefreshPinyinCache()
        {
            foreach (var item in Settings.ShortCuts)
            {
                try
                {
                    item.PinyinInitials = PinyinHelper.GetInitials(item.Name ?? "");
                    item.PinyinShortcut = PinyinHelper.GetInitials(item.ShortCut ?? "");
                }
                catch
                {
                    item.PinyinInitials = (item.Name ?? "").ToLowerInvariant();
                    item.PinyinShortcut = (item.ShortCut ?? "").ToLowerInvariant();
                }
            }
        }

        public List<ShortCutItem> ImportFromLegacyFile(string filePath)
        {
            var list = new List<ShortCutItem>();
            if (!File.Exists(filePath)) return list;

            try
            {
                string[] lines;
                try
                {
                    lines = File.ReadAllLines(filePath, Encoding.GetEncoding("GB18030"));
                }
                catch
                {
                    lines = File.ReadAllLines(filePath, Encoding.UTF8);
                }

                foreach (var rawLine in lines)
                {
                    string line = rawLine.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("//") || line.StartsWith(";") || line.StartsWith("%"))
                        continue;

                    // 1. 尝试以管道符 '|' 切分
                    if (line.Contains('|'))
                    {
                        string[] parts = line.Split('|');
                        if (parts.Length >= 5)
                        {
                            var item = new ShortCutItem();
                            string freqStr = parts[0].Trim();
                            if (freqStr.StartsWith("F", StringComparison.OrdinalIgnoreCase))
                            {
                                freqStr = freqStr.Substring(1);
                            }
                            int.TryParse(freqStr, out int freq);
                            item.Freq = freq;

                            string paramTypeStr = parts[1].Trim();
                            if (Enum.TryParse<ParamType>(paramTypeStr, true, out var pt))
                            {
                                item.ParamType = pt;
                            }

                            item.ShortCut = parts[2].Trim();
                            item.Name = parts[3].Trim();
                            item.CommandLine = string.Join("|", parts.Skip(4)).Trim();

                            if (!string.IsNullOrEmpty(item.ShortCut) || !string.IsNullOrEmpty(item.Name))
                            {
                                list.Add(item);
                            }
                        }
                        else if (parts.Length == 4)
                        {
                            var item = new ShortCutItem
                            {
                                ShortCut = parts[1].Trim(),
                                Name = parts[2].Trim(),
                                CommandLine = parts[3].Trim()
                            };
                            if (!string.IsNullOrEmpty(item.ShortCut) || !string.IsNullOrEmpty(item.Name))
                            {
                                list.Add(item);
                            }
                        }
                        else if (parts.Length == 3)
                        {
                            var item = new ShortCutItem
                            {
                                ShortCut = parts[0].Trim(),
                                Name = parts[1].Trim(),
                                CommandLine = parts[2].Trim()
                            };
                            if (!string.IsNullOrEmpty(item.ShortCut) || !string.IsNullOrEmpty(item.Name))
                            {
                                list.Add(item);
                            }
                        }
                        else if (parts.Length == 2)
                        {
                            // 兼容 FavoriteList.txt 格式: Keyword|Name
                            string keyword = parts[0].Trim();
                            string name = parts[1].Trim();
                            var existing = Settings.ShortCuts.Find(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                                                                       x.ShortCut.Equals(keyword, StringComparison.OrdinalIgnoreCase));
                            if (existing != null)
                            {
                                existing.Freq += 500;
                            }
                            else
                            {
                                list.Add(new ShortCutItem
                                {
                                    ShortCut = keyword,
                                    Name = name,
                                    CommandLine = name,
                                    Freq = 500
                                });
                            }
                        }
                    }
                    // 2. 兼容早期以逗号 ',' 切分的老版本
                    else if (line.Contains(','))
                    {
                        string[] parts = line.Split(',');
                        if (parts.Length >= 3)
                        {
                            list.Add(new ShortCutItem
                            {
                                ShortCut = parts[0].Trim(),
                                Name = parts[1].Trim(),
                                CommandLine = string.Join(",", parts.Skip(2)).Trim()
                            });
                        }
                    }
                }

                // 若导入的是 ShortCutList.txt，同时自动检测同目录下是否存在 FavoriteList.txt 并融合加权
                string dir = Path.GetDirectoryName(filePath) ?? "";
                string favPath = Path.Combine(dir, "FavoriteList.txt");
                if (File.Exists(favPath))
                {
                    ApplyFavoriteList(favPath);
                }
            }
            catch { }

            return list;
        }

        private void ApplyFavoriteList(string favPath)
        {
            if (!File.Exists(favPath)) return;
            try
            {
                var lines = File.ReadAllLines(favPath, Encoding.GetEncoding("GB18030"));
                foreach (var line in lines)
                {
                    var parts = line.Split('|');
                    if (parts.Length >= 2)
                    {
                        string keyword = parts[0].Trim();
                        string name = parts[1].Trim();

                        var match = Settings.ShortCuts.Find(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                                                                 x.ShortCut.Equals(keyword, StringComparison.OrdinalIgnoreCase));
                        if (match != null)
                        {
                            match.Freq += 500; // 最爱置顶加权
                        }
                    }
                }
            }
            catch { }
        }

        public void ExportToLegacyFile(string filePath)
        {
            try
            {
                var sb = new StringBuilder();
                foreach (var item in Settings.ShortCuts)
                {
                    sb.AppendLine($"F{item.Freq,-8}|{item.ParamType,-20}|{item.ShortCut,-30}|{item.Name,-30}|{item.CommandLine}");
                }
                File.WriteAllText(filePath, sb.ToString(), Encoding.GetEncoding("GB18030"));
            }
            catch { }
        }

        public static List<ShortCutItem> GenerateModernDefaultShortcuts()
        {
            return new List<ShortCutItem>
            {
                new() { ShortCut = "wt", Name = "Windows Terminal 终端", CommandLine = "wt.exe", Freq = 120 },
                new() { ShortCut = "pwsh", Name = "PowerShell 终端", CommandLine = "powershell.exe", Freq = 90 },
                new() { ShortCut = "cmd", Name = "命令提示符", CommandLine = "cmd.exe", Freq = 80 },
                new() { ShortCut = "calc", Name = "计算器", CommandLine = "calc.exe", Freq = 100 },
                new() { ShortCut = "snip", Name = "截图工具 (剪切板)", CommandLine = "ms-screenclip:", Freq = 95 },
                new() { ShortCut = "task", Name = "任务管理器", CommandLine = "taskmgr.exe", Freq = 85 },
                new() { ShortCut = "pad", Name = "记事本", CommandLine = "notepad.exe", Freq = 75 },
                new() { ShortCut = "reg", Name = "注册表编辑器", CommandLine = "regedit.exe", Freq = 50 },
                new() { ShortCut = "hosts", Name = "编辑 Hosts 文件", CommandLine = "notepad.exe C:\\Windows\\System32\\drivers\\etc\\hosts", Freq = 40 },
                new() { ShortCut = "set", Name = "Windows 设置主页", CommandLine = "ms-settings:", Freq = 90 },
                new() { ShortCut = "app", Name = "已安装应用 (卸载程序)", CommandLine = "ms-settings:appsfeatures", Freq = 80 },
                new() { ShortCut = "net", Name = "网络与 Internet 设置", CommandLine = "ms-settings:network", Freq = 70 },
                new() { ShortCut = "blue", Name = "蓝牙与设备设置", CommandLine = "ms-settings:bluetooth", Freq = 65 },
                new() { ShortCut = "up", Name = "Windows 更新检查", CommandLine = "ms-settings:windowsupdate", Freq = 60 },
                new() { ShortCut = "vol", Name = "声音与音量混合器", CommandLine = "ms-settings:sound", Freq = 55 },
                new() { ShortCut = "g", Name = "Google 搜索", CommandLine = "https://www.google.com/search?q=%p", ParamType = ParamType.ptUTF8Query, Freq = 70 },
                new() { ShortCut = "b", Name = "百度搜索", CommandLine = "https://www.baidu.com/s?wd=%p", ParamType = ParamType.ptURLQuery, Freq = 60 },
                new() { ShortCut = "gh", Name = "GitHub 仓库搜索", CommandLine = "https://github.com/search?q=%p", ParamType = ParamType.ptUTF8Query, Freq = 50 },
                new() { ShortCut = "cb", Name = "百度搜索剪贴板内容", CommandLine = "https://www.baidu.com/s?wd={%c}", Freq = 40 },
                new() { ShortCut = "cg", Name = "Google 搜索剪贴板内容", CommandLine = "https://www.google.com/search?q={%c}", Freq = 40 }
            };
        }
    }
}
