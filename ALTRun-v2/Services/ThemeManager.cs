using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using Application = System.Windows.Application;

namespace ALTRun.Services
{
    public class ThemeDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsDark { get; set; } = true;

        public Color BgColor { get; set; }
        public Color CardBgColor { get; set; }
        public Color BorderColor { get; set; }
        public Color AccentColor { get; set; }
        public Color AccentHoverColor { get; set; }
        public Color AccentTextColor { get; set; } = Color.FromRgb(0xFF, 0xFF, 0xFF);
        public Color TextColor { get; set; }
        public Color TextMutedColor { get; set; }
        public Color BadgeBgColor { get; set; }
        public Color HoverBgColor { get; set; }
        public Color HighlightBgColor { get; set; }
        public Color SelectionBorderColor { get; set; }

        public override string ToString() => $"{Icon} {Name} ({(IsDark ? "暗色" : "浅色")})";
    }

    /// <summary>
    /// 主题 JSON 的数据传输对象：颜色以 #AARRGGBB 字符串保存，便于用户直接编辑配色。
    /// </summary>
    internal class ThemeDefinitionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsDark { get; set; } = true;
        public string BgColor { get; set; } = "#FF000000";
        public string CardBgColor { get; set; } = "#FF000000";
        public string BorderColor { get; set; } = "#FF000000";
        public string AccentColor { get; set; } = "#FF000000";
        public string AccentHoverColor { get; set; } = "#FF000000";
        public string AccentTextColor { get; set; } = "#FFFFFFFF";
        public string TextColor { get; set; } = "#FFFFFFFF";
        public string TextMutedColor { get; set; } = "#FF808080";
        public string BadgeBgColor { get; set; } = "#20FFFFFF";
        public string HoverBgColor { get; set; } = "#10FFFFFF";
        public string HighlightBgColor { get; set; } = "#20FFFFFF";
        public string SelectionBorderColor { get; set; } = "#FF000000";

        public ThemeDefinition ToTheme()
        {
            return new ThemeDefinition
            {
                Id = Id,
                Name = Name,
                Icon = Icon,
                Description = Description,
                IsDark = IsDark,
                BgColor = ThemeColorParser.Parse(BgColor),
                CardBgColor = ThemeColorParser.Parse(CardBgColor),
                BorderColor = ThemeColorParser.Parse(BorderColor),
                AccentColor = ThemeColorParser.Parse(AccentColor),
                AccentHoverColor = ThemeColorParser.Parse(AccentHoverColor),
                AccentTextColor = ThemeColorParser.Parse(AccentTextColor),
                TextColor = ThemeColorParser.Parse(TextColor),
                TextMutedColor = ThemeColorParser.Parse(TextMutedColor),
                BadgeBgColor = ThemeColorParser.Parse(BadgeBgColor),
                HoverBgColor = ThemeColorParser.Parse(HoverBgColor),
                HighlightBgColor = ThemeColorParser.Parse(HighlightBgColor),
                SelectionBorderColor = ThemeColorParser.Parse(SelectionBorderColor)
            };
        }
    }

    internal class ThemesFileDto
    {
        public List<ThemeDefinitionDto> Themes { get; set; } = new();
    }

    /// <summary>
    /// 解析 #RRGGBB / #AARRGGBB 颜色字符串。非法格式回退为不透明黑。
    /// </summary>
    internal static class ThemeColorParser
    {
        public static Color Parse(string? hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return Color.FromArgb(0xFF, 0x00, 0x00, 0x00);
            string s = hex.Trim().TrimStart('#');
            try
            {
                if (s.Length == 6) s = "FF" + s;
                if (s.Length != 8) return Color.FromArgb(0xFF, 0x00, 0x00, 0x00);
                byte a = Convert.ToByte(s.Substring(0, 2), 16);
                byte r = Convert.ToByte(s.Substring(2, 2), 16);
                byte g = Convert.ToByte(s.Substring(4, 2), 16);
                byte b = Convert.ToByte(s.Substring(6, 2), 16);
                return Color.FromArgb(a, r, g, b);
            }
            catch
            {
                return Color.FromArgb(0xFF, 0x00, 0x00, 0x00);
            }
        }
    }

    public static class ThemeManager
    {
        private static readonly string ThemesFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Themes", "themes.json");

        /// <summary>
        /// 主题列表：优先从 exe 同目录 Themes/themes.json 加载（用户可直接改配色、无需重新编译）；
        /// 文件缺失或损坏时回退到内置的黑曜灵动主题，保证程序始终可用。
        /// </summary>
        public static List<ThemeDefinition> Themes { get; } = LoadThemes();

        private static List<ThemeDefinition> LoadThemes()
        {
            try
            {
                if (File.Exists(ThemesFilePath))
                {
                    string json = File.ReadAllText(ThemesFilePath);
                    var file = JsonSerializer.Deserialize<ThemesFileDto>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    });
                    if (file?.Themes != null && file.Themes.Count > 0)
                    {
                        var loaded = file.Themes.Select(x => x.ToTheme()).ToList();
                        // 至少要有一套暗色和一套浅色，否则视为无效文件、回退内置
                        if (loaded.Any(t => t.IsDark) && loaded.Any(t => !t.IsDark))
                            return loaded;
                    }
                }
            }
            catch (Exception ex)
            {
                // 主题文件损坏时静默回退内置主题，不影响启动（但保留日志便于排查"自定义主题为何不生效"）
                AppLog.Write($"[ALTRun] ThemeManager.cs:148 加载主题文件失败，回退内置主题: {ex.Message}");
            }

            // 内置兜底主题（黑曜灵动）：即使没有 themes.json 也能正常显示
            return new List<ThemeDefinition>
            {
                new ThemeDefinition
                {
                    Id = "obsidian",
                    Name = "黑曜灵动",
                    Icon = "🟣",
                    Description = "极客深空曜黑与高雅紫罗兰，沉稳现代 (推荐)",
                    IsDark = true,
                    BgColor = Hex("#FF131318"), CardBgColor = Hex("#FF1C1C24"),
                    BorderColor = Hex("#35A78BFA"), AccentColor = Hex("#FF9065F6"),
                    AccentHoverColor = Hex("#FFA78BFA"), AccentTextColor = Hex("#FFFFFFFF"),
                    TextColor = Hex("#FFF8FAFC"), TextMutedColor = Hex("#FF94A3B8"),
                    BadgeBgColor = Hex("#28FFFFFF"), HoverBgColor = Hex("#18A78BFA"),
                    HighlightBgColor = Hex("#358B5CF6"), SelectionBorderColor = Hex("#FF9065F6")
                }
            };
        }

        private static Color Hex(string hex) => ThemeColorParser.Parse(hex);

        public static ThemeDefinition GetTheme(string? themeId, bool isDarkDefault = true)
        {
            if (!string.IsNullOrEmpty(themeId))
            {
                var match = Themes.FirstOrDefault(x => x.Id.Equals(themeId, StringComparison.OrdinalIgnoreCase));
                if (match != null) return match;
            }

            if (isDarkDefault)
                return Themes.FirstOrDefault(t => t.IsDark) ?? Themes[0];

            return Themes.FirstOrDefault(t => !t.IsDark) ?? Themes[0];
        }

        public static void ApplyTheme(ResourceDictionary res, ThemeDefinition theme)
        {
            // 1. MainWindow 核心资源画刷
            res["BgBrush"] = new SolidColorBrush(theme.BgColor);
            res["CardBgBrush"] = new SolidColorBrush(theme.CardBgColor);
            res["BorderBrush"] = new SolidColorBrush(theme.BorderColor);
            res["AccentBrush"] = new SolidColorBrush(theme.AccentColor);
            res["AccentHoverBrush"] = new SolidColorBrush(theme.AccentHoverColor);
            res["AccentTextBrush"] = new SolidColorBrush(theme.AccentTextColor);
            res["TextBrush"] = new SolidColorBrush(theme.TextColor);
            res["TextMutedBrush"] = new SolidColorBrush(theme.TextMutedColor);
            res["BadgeBgBrush"] = new SolidColorBrush(theme.BadgeBgColor);
            res["HoverBgBrush"] = new SolidColorBrush(theme.HoverBgColor);
            res["HighlightBgBrush"] = new SolidColorBrush(theme.HighlightBgColor);

            // 2. ManageWindow / ScanAppsWindow 控件画刷映射
            res["WindowBg"] = new SolidColorBrush(theme.BgColor);
            res["CardBg"] = new SolidColorBrush(theme.CardBgColor);
            res["TextPrimary"] = new SolidColorBrush(theme.TextColor);
            res["TextSecondary"] = new SolidColorBrush(theme.TextMutedColor);
            res["TextMuted"] = new SolidColorBrush(theme.TextMutedColor);
            res["InputBg"] = new SolidColorBrush(theme.CardBgColor);
            res["InputBorder"] = new SolidColorBrush(theme.BorderColor);
            res["BtnBg"] = new SolidColorBrush(theme.CardBgColor);
            res["BtnFg"] = new SolidColorBrush(theme.TextColor);
            res["BtnBorder"] = new SolidColorBrush(theme.BorderColor);
            res["TableSelectBg"] = new SolidColorBrush(theme.HighlightBgColor);
            res["TableSelectBorder"] = new SolidColorBrush(theme.SelectionBorderColor);
            res["TableSelectText"] = new SolidColorBrush(theme.TextColor);
            res["TableSelectSubText"] = new SolidColorBrush(theme.AccentHoverColor);

            // 扫描窗口表格：表头、隔行、悬停、选中均由卡片底色派生，保持同色系但深浅不同。
            // 表头最深、悬停次之，避免暗/浅主题下出现纯黑块。
            res["HeaderBg"] = new SolidColorBrush(Shade(theme.CardBgColor, -0.12));
            res["RowAltBg"] = new SolidColorBrush(Shade(theme.CardBgColor, -0.03));
            res["RowHoverBg"] = new SolidColorBrush(Shade(theme.CardBgColor, -0.06));
            res["RowSelectedBg"] = new SolidColorBrush(Shade(theme.CardBgColor, -0.14));
        }

        /// <summary>
        /// 按比例调整颜色明暗。amount 为负数变暗、正数变亮，取值范围 -1 ~ 1。
        /// </summary>
        private static Color Shade(Color color, double amount)
        {
            double ratio = Math.Max(-1, Math.Min(1, amount));

            byte Mix(byte channel)
            {
                double value = ratio < 0
                    ? channel * (1 + ratio)
                    : channel + (255 - channel) * ratio;
                return (byte)Math.Max(0, Math.Min(255, Math.Round(value)));
            }

            return Color.FromArgb(color.A, Mix(color.R), Mix(color.G), Mix(color.B));
        }

        public static void ApplyTheme(Window window, ThemeDefinition theme)
        {
            ApplyTheme(window.Resources, theme);

            // 同步 DWM 窗口暗色标题栏特性
            try
            {
                DwmHelper.ApplyModernWindowStyles(window, theme.IsDark);
            }
            catch (Exception ex)
            {
                AppLog.Write($"[ALTRun] ThemeManager.cs:255 静默异常: {ex.Message}");
            }
        }

        public static void ApplyThemeToAll(ThemeDefinition theme)
        {
            if (Application.Current != null)
            {
                // 先同步更新 Application.Current.Resources，让所有无父级窗口的托盘 ContextMenu 与浮动 Popup 立即获得正确主题
                ApplyTheme(Application.Current.Resources, theme);

                foreach (Window win in Application.Current.Windows)
                {
                    ApplyTheme(win, theme);
                }
            }
        }
    }
}
