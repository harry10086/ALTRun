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
        public double WindowWidth { get; set; } = 660;
        public double WindowHeight { get; set; } = 460;
        public bool SingleClickToRun { get; set; } = true;
        public bool EnableNumberKeyDirect { get; set; } = true;
        public bool HideWhenLostFocus { get; set; } = true;
        public bool RunAsAdminShortcut { get; set; } = true;
        /// <summary>上次检测到新版本的月份（yyyy-MM），用于每月只检查一次更新</summary>
        public string LastUpdateCheckMonth { get; set; } = string.Empty;
        public List<ShortCutItem> ShortCuts { get; set; } = new();
    }

    public class ConfigManager
    {
        private readonly string _appDir;
        private readonly string _jsonConfigPath;

        // 延迟保存：频次等高频小改动先标脏，合并到一次写盘，避免每次执行都全量序列化
        private System.Windows.Threading.DispatcherTimer? _saveTimer;
        private bool _isDirty;
        private const int SaveDelayMs = 1500;

        public AppSettings Settings { get; private set; } = new();
        public bool IsFirstRun { get; private set; } = false;

        public ConfigManager()
        {
            // .NET Core / .NET 8 默认不注册 GB18030 / GBK 代码页。
            // 必须在读取旧版 ShortCutList.txt（GBK 编码）之前注册，否则会静默退回 UTF-8，中文变乱码。
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            }
            catch (Exception ex)
            {
                AppLog.Write($"[ALTRun] ConfigManager.cs:49 静默异常: {ex.Message}");
            }

            _appDir = AppDomain.CurrentDomain.BaseDirectory;
            _jsonConfigPath = Path.Combine(_appDir, "ALTRun_Config.json");
        }

        /// <summary>本次启动是否因配置文件损坏而做了备份；null 表示未发生。</summary>
        public string? CorruptConfigBackupPath { get; private set; }

        private enum ConfigLoadResult
        {
            /// <summary>成功读取现代 JSON 配置</summary>
            Loaded,
            /// <summary>没有配置文件</summary>
            Missing,
            /// <summary>文件存在但内容损坏/被截断</summary>
            Broken,
            /// <summary>文件被其他进程长期占用，本次只读降级</summary>
            Locked
        }

        public void Load()
        {
            var state = TryLoadJsonConfig();
            switch (state)
            {
                case ConfigLoadResult.Loaded:
                    return;

                case ConfigLoadResult.Broken:
                    // 关键防护：解析失败时先把原文件重命名备份，绝不让后面的默认初始化直接覆盖用户数据。
                    BackupCorruptConfig();
                    // 磁盘上原本存在配置文件，说明不是首次运行，不能弹首次使用向导
                    IsFirstRun = false;
                    break;

                case ConfigLoadResult.Locked:
                    // 文件被占用（杀软扫描 / 网盘同步 / 断电恢复中）：只读降级，本次不写盘，避免把真实数据写坏。
                    IsFirstRun = false;
                    if (Settings.ShortCuts.Count == 0)
                    {
                        Settings.ShortCuts = GenerateModernDefaultShortcuts();
                        RefreshPinyinCache();
                    }
                    return;

                case ConfigLoadResult.Missing:
                    break;
            }

            // 2. 自动检测旧版原版 ShortCutList.txt 或 ShortCut.ini
            string[] possiblePaths = new[]
            {
                Path.Combine(_appDir, "ShortCutList.txt"),
                Path.Combine(Directory.GetParent(_appDir)?.FullName ?? _appDir, "ShortCutList.txt"),
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

                        // 确保系统快捷指令开箱可用
                        EnsureSystemShortcuts(Settings.ShortCuts);

                        RefreshPinyinCache();
                        Save();
                        return;
                    }
                }
            }

            // 3. 首次启动开箱即用：直接加载内置 Win10/11 现代快捷列表
            // 只有"确实没有配置文件"才算首次运行；配置损坏已被备份走的场景不能弹首次使用向导。
            if (state == ConfigLoadResult.Missing)
            {
                IsFirstRun = true;
            }
            Settings.ShortCuts = GenerateModernDefaultShortcuts();
            RefreshPinyinCache();
            Save();
        }

        /// <summary>
        /// 读取现代 JSON 配置。把"文件不存在 / 内容损坏 / 被占用"三种情况区分开，
        /// 避免像旧实现那样一律吞掉异常然后无条件用默认值覆盖用户数据。
        /// </summary>
        private ConfigLoadResult TryLoadJsonConfig()
        {
            if (!File.Exists(_jsonConfigPath)) return ConfigLoadResult.Missing;

            // 微小重试：防止开机瞬间文件被杀软或网盘同步锁定
            for (int attempt = 0; attempt < 3; attempt++)
            {
                string json;

                // ⚠ 必须先把文本读进内存并释放文件句柄，再走 Save()。
                // 否则后续的原子写（File.Replace / File.Move）会因为"目标是本进程自己持有句柄"而失败，
                // 表现为配置写不进去、只留下一个 .tmp 文件。
                try
                {
                    using (var fs = new FileStream(_jsonConfigPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fs, Encoding.UTF8))
                    {
                        json = reader.ReadToEnd();
                    }
                }
                catch (IOException ex)
                {
                    AppLog.Write($"[ALTRun] ConfigManager 配置读取被占用，第 {attempt + 1}/3 次重试: {ex.Message}");
                    System.Threading.Thread.Sleep(50);
                    continue;
                }
                catch (Exception ex)
                {
                    AppLog.Write($"[ALTRun] ConfigManager 配置读取失败: {ex.Message}");
                    return ConfigLoadResult.Broken;
                }

                // ── 以下处理阶段文件句柄已释放，可以安全写盘 ──
                if (string.IsNullOrWhiteSpace(json))
                {
                    AppLog.Write("[ALTRun] ConfigManager 配置文件为空，按损坏处理");
                    return ConfigLoadResult.Broken;
                }

                AppSettings? settings;
                try
                {
                    settings = JsonSerializer.Deserialize<AppSettings>(json);
                }
                catch (Exception ex)
                {
                    AppLog.Write($"[ALTRun] ConfigManager 配置解析失败: {ex.Message}");
                    return ConfigLoadResult.Broken;
                }

                // "ShortCuts": [] 表示用户主动清空了列表，应当接受；
                // 而字段整个缺失或为 null，则说明文件被截断（断电/杀软中断），必须按损坏处理并备份。
                bool hasShortCutsProperty = json.IndexOf("\"ShortCuts\"", StringComparison.Ordinal) >= 0;
                if (settings == null || settings.ShortCuts == null || (settings.ShortCuts.Count == 0 && !hasShortCutsProperty))
                {
                    AppLog.Write("[ALTRun] ConfigManager 配置结构不完整，按损坏处理");
                    return ConfigLoadResult.Broken;
                }

                bool configChanged = false;
                if (string.IsNullOrEmpty(settings.Theme))
                {
                    settings.Theme = settings.DarkMode ? "obsidian" : "pearl";
                    configChanged = true;
                }
                Settings = settings;

                // 确保系统级现代快捷方式开箱即用，无感修复历史遗留失效配置
                configChanged |= EnsureSystemShortcuts(Settings.ShortCuts);

                RefreshPinyinCache();

                // 只有本次读取确实修正了内容才写盘：避免每次启动都白白重写一遍配置
                if (configChanged)
                {
                    Save();
                }
                return ConfigLoadResult.Loaded;
            }

            AppLog.Write("[ALTRun] ConfigManager 配置文件持续被占用，本次以只读降级方式启动（不写盘）");
            return ConfigLoadResult.Locked;
        }

        /// <summary>
        /// 把损坏的配置文件重命名为 ALTRun_Config.json.corrupt-&lt;时间戳&gt;，保证用户原始数据可人工恢复。
        /// </summary>
        private void BackupCorruptConfig()
        {
            try
            {
                if (!File.Exists(_jsonConfigPath)) return;

                string backupPath = Path.Combine(_appDir, $"ALTRun_Config.json.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}");
                int suffix = 1;
                while (File.Exists(backupPath))
                {
                    backupPath = Path.Combine(_appDir, $"ALTRun_Config.json.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}-{suffix++}");
                }

                File.Move(_jsonConfigPath, backupPath);
                CorruptConfigBackupPath = backupPath;
                AppLog.Write($"[ALTRun] 配置文件损坏，已备份到: {backupPath}");
            }
            catch (Exception ex)
            {
                AppLog.Write($"[ALTRun] ConfigManager 备份损坏配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 立即保存（设置变更等需要立刻落盘的场景）。会取消挂起的延迟保存。
        /// </summary>
        public void Save()
        {
            // 计时器属于创建它的 UI 线程；后台线程（如向导自动导入）调用 Save 时直接 Stop 会抛
            // InvalidOperationException，因此这里必须容错——即使停不掉，OnSaveTimerTick 也会因 _isDirty
            // 已置 false 而什么都不做。
            try
            {
                _saveTimer?.Stop();
            }
            catch (Exception ex)
            {
                AppLog.Write($"[ALTRun] ConfigManager 停止延迟保存计时器失败: {ex.Message}");
            }

            _isDirty = false;
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(Settings, options);
                WriteAllTextAtomic(_jsonConfigPath, json);
            }
            catch (Exception ex)
            {
                AppLog.Write($"[ALTRun] ConfigManager.cs:164 静默异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 原子写盘：先写同目录临时文件，再一次性替换目标文件。
        /// 避免"写到一半被断电/杀软中断"生成半截 JSON——那正是下次启动被判为配置损坏的根因。
        /// </summary>
        private static void WriteAllTextAtomic(string path, string content)
        {
            string tempPath = path + ".tmp";
            File.WriteAllText(tempPath, content, Encoding.UTF8);

            if (File.Exists(path))
            {
                try
                {
                    // File.Replace 是真正的原子替换（同时保留原文件 ACL）
                    File.Replace(tempPath, path, null);
                    return;
                }
                catch (Exception ex)
                {
                    // FAT32 / 部分网络盘不支持 Replace，退回 Move 覆盖
                    AppLog.Write($"[ALTRun] ConfigManager File.Replace 失败，改用 Move: {ex.Message}");
                }
            }

            try
            {
                File.Move(tempPath, path, overwrite: true);
            }
            catch (Exception)
            {
                // 彻底写不进去（目标被独占、目录只读等）：清掉临时文件，避免目录里堆积 .tmp
                try
                {
                    File.Delete(tempPath);
                }
                catch (Exception cleanupEx)
                {
                    AppLog.Write($"[ALTRun] ConfigManager 清理临时文件失败: {cleanupEx.Message}");
                }
                throw;
            }
        }

        /// <summary>
        /// 标记配置已变更但不立即写盘，合并短时间内的多次改动为一次保存。用于频次自增等高频场景。
        /// </summary>
        public void MarkDirty()
        {
            _isDirty = true;
            _saveTimer ??= new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(SaveDelayMs)
            };
            _saveTimer.Tick -= OnSaveTimerTick;
            _saveTimer.Tick += OnSaveTimerTick;
            _saveTimer.Stop();
            _saveTimer.Start();
        }

        private void OnSaveTimerTick(object? sender, EventArgs e)
        {
            if (_isDirty)
            {
                Save();
            }
        }

        /// <summary>
        /// 程序退出前调用：若有挂起的延迟保存则立即落盘，避免频次改动丢失。
        /// </summary>
        public void FlushPendingSave()
        {
            if (_isDirty)
            {
                Save();
            }
        }

        public void RefreshPinyinCache()
        {
            foreach (var item in Settings.ShortCuts)
            {
                // 一次性预计算小写形式与拼音，搜索时直接复用，避免逐字符重复分配字符串
                item.LowerShortCut = (item.ShortCut ?? "").ToLowerInvariant();
                item.LowerName = (item.Name ?? "").ToLowerInvariant();

                try
                {
                    item.PinyinInitials = PinyinHelper.GetInitials(item.Name ?? "");
                    item.PinyinShortcut = PinyinHelper.GetInitials(item.ShortCut ?? "");
                }
                catch (Exception ex)
                {
                    // 拼音库异常时退回小写名称，功能降级但不影响搜索可用性
                    AppLog.Write($"[ALTRun] ConfigManager 拼音缓存计算失败 ({item.Name}): {ex.Message}");
                    item.PinyinInitials = item.LowerName;
                    item.PinyinShortcut = item.LowerShortCut;
                }

                // GetInitials 内部已输出小写，这里再兜底一次，保证缓存不依赖其实现细节
                item.LowerPinyinInitials = item.PinyinInitials.ToLowerInvariant();
                item.LowerPinyinShortcut = item.PinyinShortcut.ToLowerInvariant();
            }
        }

        public List<ShortCutItem> ImportFromLegacyFile(string filePath)
        {
            var list = new List<ShortCutItem>();
            if (!File.Exists(filePath)) return list;

            try
            {
                // 旧版配置是 GBK/ANSI，新版导出可能是 UTF-8，按内容自动判断，避免用错编码产生乱码
                string[] lines = File.ReadAllLines(filePath, DetectLegacyEncoding(filePath));

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
                            var existing = Settings.ShortCuts.Find(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase) ||
                                                                       string.Equals(x.ShortCut, keyword, StringComparison.OrdinalIgnoreCase));
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
            catch (Exception ex)
            {
                AppLog.Write($"[ALTRun] ConfigManager.cs:346 静默异常: {ex.Message}");
            }

            return list;
        }

        /// <summary>
        /// 自动识别旧版配置文本编码：能用严格 UTF-8 解码就当 UTF-8，否则按 GB18030（GBK）读取。
        /// </summary>
        private static Encoding DetectLegacyEncoding(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            var strictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            try
            {
                strictUtf8.GetString(bytes); // 非法 UTF-8 字节会抛 DecoderFallbackException
                return strictUtf8;
            }
            catch (DecoderFallbackException)
            {
                return Encoding.GetEncoding("GB18030");
            }
        }

        private void ApplyFavoriteList(string favPath)
        {
            if (!File.Exists(favPath)) return;
            try
            {
                var lines = File.ReadAllLines(favPath, DetectLegacyEncoding(favPath));
                foreach (var line in lines)
                {
                    var parts = line.Split('|');
                    if (parts.Length >= 2)
                    {
                        string keyword = parts[0].Trim();
                        string name = parts[1].Trim();

                        var match = Settings.ShortCuts.Find(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase) ||
                                                                 string.Equals(x.ShortCut, keyword, StringComparison.OrdinalIgnoreCase));
                        if (match != null)
                        {
                            match.Freq += 500; // 最爱置顶加权
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Write($"[ALTRun] ConfigManager.cs:392 静默异常: {ex.Message}");
            }
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
            catch (Exception ex)
            {
                AppLog.Write($"[ALTRun] ConfigManager.cs:406 静默异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 无感修复历史遗留失效指令，并补齐系统必备快捷项。
        /// </summary>
        /// <returns>是否真的改动了列表（没改动就不必再写一次配置）。</returns>
        public static bool EnsureSystemShortcuts(List<ShortCutItem> shortcuts)
        {
            if (shortcuts == null) return false;

            bool changed = false;

            // 1. 自动将旧版已失效的 WinCtl / SetSuspendState 指令无感升级为现代原生协议
            foreach (var item in shortcuts)
            {
                if (string.IsNullOrWhiteSpace(item.CommandLine)) continue;
                string cmd = item.CommandLine.Trim();
                string? upgraded = null;

                if (cmd.Contains("WinCtl", StringComparison.OrdinalIgnoreCase))
                {
                    string lower = cmd.ToLowerInvariant();
                    if (lower.Contains("minall"))
                        upgraded = "win:desktop";
                    else if (lower.Contains("max all") || lower.Contains("maxall"))
                        upgraded = "win:maxall";
                    else if (lower.Contains("close all") || lower.Contains("closeall"))
                        upgraded = "win:closeall";
                    else if (lower.Contains("untop"))
                        upgraded = "win:untop";
                    else if (lower.Contains("top"))
                        upgraded = "win:top";
                    else if (lower.Contains("min"))
                        upgraded = "win:min";
                    else if (lower.Contains("max"))
                        upgraded = "win:max";
                    else if (lower.Contains("close"))
                        upgraded = "win:close";
                }
                else if (cmd.Contains("SetSuspendState", StringComparison.OrdinalIgnoreCase) && cmd.Contains("powrprof.dll", StringComparison.OrdinalIgnoreCase))
                {
                    upgraded = "sys:sleep";
                }
                else if (cmd.StartsWith("::{"))
                {
                    upgraded = $"explorer.exe shell:{cmd}";
                }

                if (upgraded != null && !string.Equals(upgraded, item.CommandLine, StringComparison.Ordinal))
                {
                    item.CommandLine = upgraded;
                    changed = true;
                }
            }

            // 2. 检查关键系统级快捷方式是否已存在，如缺失则自动智能补齐
            var essentials = GetEssentialSystemShortcuts();
            foreach (var def in essentials)
            {
                bool exists = shortcuts.Any(x =>
                    (x.ShortCut?.Equals(def.ShortCut, StringComparison.OrdinalIgnoreCase) == true) ||
                    (x.CommandLine?.Equals(def.CommandLine, StringComparison.OrdinalIgnoreCase) == true));

                if (!exists)
                {
                    shortcuts.Add(def);
                    changed = true;
                }
            }

            return changed;
        }

        public static List<ShortCutItem> GetEssentialSystemShortcuts()
        {
            return new List<ShortCutItem>
            {
                new() { ShortCut = "desktop", Name = "显示桌面", CommandLine = "win:desktop", Freq = 130 },
                new() { ShortCut = "top", Name = "置顶当前窗口", CommandLine = "win:top", Freq = 95 },
                new() { ShortCut = "untop", Name = "取消置顶当前窗口", CommandLine = "win:untop", Freq = 60 },
                new() { ShortCut = "minall", Name = "最小化所有窗口", CommandLine = "win:minall", Freq = 85 },
                new() { ShortCut = "maxall", Name = "还原所有窗口", CommandLine = "win:maxall", Freq = 75 },
                new() { ShortCut = "closewin", Name = "关闭当前窗口", CommandLine = "win:close", Freq = 65 },
                new() { ShortCut = "closeall", Name = "关闭全部普通窗口", CommandLine = "win:closeall", Freq = 50 },
                new() { ShortCut = "lock", Name = "锁定屏幕", CommandLine = "sys:lock", Freq = 90 },
                new() { ShortCut = "sleep", Name = "系统睡眠", CommandLine = "sys:sleep", Freq = 80 },
                new() { ShortCut = "r", Name = "运行 (直接回车打开运行框)", CommandLine = "{%p}", Freq = 85 }
            };
        }

        public static List<ShortCutItem> GenerateModernDefaultShortcuts()
        {
            return new List<ShortCutItem>
            {
                // ── 系统窗口与桌面管理 ──
                new() { ShortCut = "desktop", Name = "显示桌面", CommandLine = "win:desktop", Freq = 130 },
                new() { ShortCut = "top", Name = "置顶当前窗口", CommandLine = "win:top", Freq = 95 },
                new() { ShortCut = "untop", Name = "取消置顶当前窗口", CommandLine = "win:untop", Freq = 60 },
                new() { ShortCut = "minall", Name = "最小化所有窗口", CommandLine = "win:minall", Freq = 85 },
                new() { ShortCut = "maxall", Name = "还原所有窗口", CommandLine = "win:maxall", Freq = 75 },
                new() { ShortCut = "closewin", Name = "关闭当前窗口", CommandLine = "win:close", Freq = 65 },
                new() { ShortCut = "closeall", Name = "关闭全部普通窗口", CommandLine = "win:closeall", Freq = 50 },

                // ── 电源与系统安全 ──
                new() { ShortCut = "lock", Name = "锁定屏幕", CommandLine = "sys:lock", Freq = 90 },
                new() { ShortCut = "sleep", Name = "系统睡眠", CommandLine = "sys:sleep", Freq = 80 },
                new() { ShortCut = "shutdown", Name = "立即关机", CommandLine = "shutdown.exe /s /t 0", Freq = 50 },
                new() { ShortCut = "reboot", Name = "立即重启", CommandLine = "shutdown.exe /r /t 0", Freq = 50 },

                // ── 原生工具与常用终端 ──
                new() { ShortCut = "r", Name = "运行 (直接回车打开运行框)", CommandLine = "{%p}", Freq = 85 },
                new() { ShortCut = "wt", Name = "Windows Terminal 终端", CommandLine = "wt.exe", Freq = 120 },
                new() { ShortCut = "pwsh", Name = "PowerShell 终端", CommandLine = "powershell.exe", Freq = 90 },
                new() { ShortCut = "cmd", Name = "命令提示符", CommandLine = "cmd.exe", Freq = 80 },
                new() { ShortCut = "calc", Name = "计算器", CommandLine = "calc.exe", Freq = 100 },
                new() { ShortCut = "snip", Name = "截图工具 (剪切板)", CommandLine = "ms-screenclip:", Freq = 95 },
                new() { ShortCut = "task", Name = "任务管理器", CommandLine = "taskmgr.exe", Freq = 90 },
                new() { ShortCut = "pad", Name = "记事本", CommandLine = "notepad.exe", Freq = 75 },
                new() { ShortCut = "reg", Name = "注册表编辑器", CommandLine = "regedit.exe", Freq = 60 },
                new() { ShortCut = "dev", Name = "设备管理器", CommandLine = "devmgmt.msc", Freq = 55 },
                new() { ShortCut = "hosts", Name = "编辑 Hosts 文件", CommandLine = "notepad.exe C:\\Windows\\System32\\drivers\\etc\\hosts", Freq = 50 },

                // ── Windows 10/11 现代设置 ──
                new() { ShortCut = "set", Name = "Windows 设置主页", CommandLine = "ms-settings:", Freq = 90 },
                new() { ShortCut = "app", Name = "已安装应用 (卸载程序)", CommandLine = "ms-settings:appsfeatures", Freq = 80 },
                new() { ShortCut = "net", Name = "网络与 Internet 设置", CommandLine = "ms-settings:network", Freq = 70 },
                new() { ShortCut = "blue", Name = "蓝牙与设备设置", CommandLine = "ms-settings:bluetooth", Freq = 65 },
                new() { ShortCut = "up", Name = "Windows 更新检查", CommandLine = "ms-settings:windowsupdate", Freq = 60 },
                new() { ShortCut = "vol", Name = "声音与音量混合器", CommandLine = "ms-settings:sound", Freq = 55 },

                // ── 常用系统路径与控制面板 ──
                new() { ShortCut = "pc", Name = "此电脑", CommandLine = "explorer.exe shell:::{20D04FE0-3AEA-1069-A2D8-08002B30309D}", Freq = 100 },
                new() { ShortCut = "down", Name = "下载文件夹", CommandLine = "shell:Downloads", Freq = 80 },
                new() { ShortCut = "startup", Name = "系统启动文件夹", CommandLine = "shell:startup", Freq = 60 },
                new() { ShortCut = "recent", Name = "最近打开文件", CommandLine = "shell:Recent", Freq = 55 },
                new() { ShortCut = "control", Name = "控制面板", CommandLine = "control.exe", Freq = 65 },
                new() { ShortCut = "sys", Name = "系统属性 (高级环境变量)", CommandLine = "Sysdm.cpl", Freq = 50 },

                // ── 网络搜索与剪贴板增强 ──
                new() { ShortCut = "g", Name = "Google 搜索", CommandLine = "https://www.google.com/search?q=%p", ParamType = ParamType.ptUTF8Query, Freq = 70 },
                new() { ShortCut = "b", Name = "百度搜索", CommandLine = "https://www.baidu.com/s?wd=%p", ParamType = ParamType.ptURLQuery, Freq = 60 },
                new() { ShortCut = "gh", Name = "GitHub 仓库搜索", CommandLine = "https://github.com/search?q=%p", ParamType = ParamType.ptUTF8Query, Freq = 50 },
                new() { ShortCut = "cg", Name = "Google 搜索剪贴板内容", CommandLine = "https://www.google.com/search?q={%c}", Freq = 40 },
                new() { ShortCut = "cb", Name = "百度搜索剪贴板内容", CommandLine = "https://www.baidu.com/s?wd={%c}", Freq = 40 }
            };
        }
    }
}
