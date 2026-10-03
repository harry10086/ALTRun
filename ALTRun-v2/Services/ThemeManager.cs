using System;
using System.Collections.Generic;
using System.Linq;
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

    public static class ThemeManager
    {
        public static List<ThemeDefinition> Themes { get; } = new()
        {
            // ========================= 🌙 暗色系列 (Dark Themes) =========================
            // 1. 🔮 黑曜灵动 (Obsidian Violet) - Raycast / Linear 极客黑与魅紫
            new ThemeDefinition
            {
                Id = "obsidian",
                Name = "黑曜灵动",
                Icon = "🔮",
                Description = "极客深空曜黑与高雅紫罗兰，沉稳现代 (推荐)",
                IsDark = true,
                BgColor = Color.FromRgb(0x13, 0x13, 0x18),
                CardBgColor = Color.FromRgb(0x1C, 0x1C, 0x24),
                BorderColor = Color.FromArgb(0x35, 0xA7, 0x8B, 0xFA),
                AccentColor = Color.FromRgb(0x90, 0x65, 0xF6),
                AccentHoverColor = Color.FromRgb(0xA7, 0x8B, 0xFA),
                AccentTextColor = Color.FromRgb(0xFF, 0xFF, 0xFF),
                TextColor = Color.FromRgb(0xF8, 0xFA, 0xFC),
                TextMutedColor = Color.FromRgb(0x94, 0xA3, 0xB8),
                BadgeBgColor = Color.FromArgb(0x28, 0xFF, 0xFF, 0xFF),
                HoverBgColor = Color.FromArgb(0x18, 0xA7, 0x8B, 0xFA),
                HighlightBgColor = Color.FromArgb(0x35, 0x8B, 0x5C, 0xF6),
                SelectionBorderColor = Color.FromRgb(0x90, 0x65, 0xF6)
            },

            // 2. 🌌 东京之夜 (Tokyo Night) - 沉浸靛青夜空与梦幻粉紫
            new ThemeDefinition
            {
                Id = "tokyo",
                Name = "东京之夜",
                Icon = "🌌",
                Description = "经典深靛蓝夜空与梦幻粉紫，极度护眼",
                IsDark = true,
                BgColor = Color.FromRgb(0x1A, 0x1B, 0x26),
                CardBgColor = Color.FromRgb(0x24, 0x28, 0x3B),
                BorderColor = Color.FromArgb(0x35, 0x7A, 0xA2, 0xF7),
                AccentColor = Color.FromRgb(0x7A, 0xA2, 0xF7),
                AccentHoverColor = Color.FromRgb(0xBB, 0x9A, 0xF7),
                AccentTextColor = Color.FromRgb(0x0F, 0x17, 0x2A),
                TextColor = Color.FromRgb(0xC0, 0xCA, 0xF5),
                TextMutedColor = Color.FromRgb(0x79, 0x82, 0xA9),
                BadgeBgColor = Color.FromArgb(0x25, 0xFF, 0xFF, 0xFF),
                HoverBgColor = Color.FromArgb(0x18, 0x7A, 0xA2, 0xF7),
                HighlightBgColor = Color.FromArgb(0x35, 0x3D, 0x59, 0xA1),
                SelectionBorderColor = Color.FromRgb(0x7A, 0xA2, 0xF7)
            },

            // 3. 🌿 赛博薄荷 (Cyber Mint) - 极客纯黑搭配霓虹薄荷绿
            new ThemeDefinition
            {
                Id = "cyber",
                Name = "赛博薄荷",
                Icon = "🌿",
                Description = "极客曜黑与清新霓虹翠绿，锋芒科技感",
                IsDark = true,
                BgColor = Color.FromRgb(0x0D, 0x11, 0x17),
                CardBgColor = Color.FromRgb(0x16, 0x1B, 0x22),
                BorderColor = Color.FromArgb(0x30, 0x10, 0xB9, 0x81),
                AccentColor = Color.FromRgb(0x10, 0xB9, 0x81),
                AccentHoverColor = Color.FromRgb(0x34, 0xD3, 0x99),
                AccentTextColor = Color.FromRgb(0x06, 0x4E, 0x3B),
                TextColor = Color.FromRgb(0xF0, 0xF6, 0xFC),
                TextMutedColor = Color.FromRgb(0x8B, 0x94, 0x9E),
                BadgeBgColor = Color.FromArgb(0x25, 0xFF, 0xFF, 0xFF),
                HoverBgColor = Color.FromArgb(0x15, 0x10, 0xB9, 0x81),
                HighlightBgColor = Color.FromArgb(0x30, 0x06, 0x4E, 0x3B),
                SelectionBorderColor = Color.FromRgb(0x10, 0xB9, 0x81)
            },

            // 4. ☕ 暖咖摩卡 (Warm Mocha) - Catppuccin 柔和护眼暖咖
            new ThemeDefinition
            {
                Id = "mocha",
                Name = "暖咖摩卡",
                Icon = "☕",
                Description = "柔和温润的暖调咖啡与珊瑚粉，细腻护眼",
                IsDark = true,
                BgColor = Color.FromRgb(0x1E, 0x1E, 0x2E),
                CardBgColor = Color.FromRgb(0x28, 0x28, 0x3B),
                BorderColor = Color.FromArgb(0x35, 0xF3, 0x8B, 0xA8),
                AccentColor = Color.FromRgb(0xF3, 0x8B, 0xA8),
                AccentHoverColor = Color.FromRgb(0xF5, 0xC2, 0xE7),
                AccentTextColor = Color.FromRgb(0x1E, 0x1E, 0x2E),
                TextColor = Color.FromRgb(0xCD, 0xD6, 0xF4),
                TextMutedColor = Color.FromRgb(0x93, 0x99, 0xB2),
                BadgeBgColor = Color.FromArgb(0x25, 0xFF, 0xFF, 0xFF),
                HoverBgColor = Color.FromArgb(0x18, 0xF3, 0x8B, 0xA8),
                HighlightBgColor = Color.FromArgb(0x35, 0x58, 0x33, 0x48),
                SelectionBorderColor = Color.FromRgb(0xF3, 0x8B, 0xA8)
            },

            // 5. 🔷 经典系统 (Classic Fluent) - Windows 11 原生深色
            new ThemeDefinition
            {
                Id = "classic",
                Name = "经典系统",
                Icon = "🔷",
                Description = "Windows 11 经典原生 Fluent 风格",
                IsDark = true,
                BgColor = Color.FromRgb(0x20, 0x20, 0x20),
                CardBgColor = Color.FromRgb(0x2B, 0x2B, 0x2B),
                BorderColor = Color.FromArgb(0x35, 0x60, 0xCD, 0xFF),
                AccentColor = Color.FromRgb(0x60, 0xCD, 0xFF),
                AccentHoverColor = Color.FromRgb(0x99, 0xEB, 0xFF),
                AccentTextColor = Color.FromRgb(0x00, 0x2B, 0x40),
                TextColor = Color.FromRgb(0xFF, 0xFF, 0xFF),
                TextMutedColor = Color.FromRgb(0xA6, 0xA6, 0xA6),
                BadgeBgColor = Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF),
                HoverBgColor = Color.FromArgb(0x18, 0x60, 0xCD, 0xFF),
                HighlightBgColor = Color.FromArgb(0x35, 0x00, 0x5A, 0x9E),
                SelectionBorderColor = Color.FromRgb(0x60, 0xCD, 0xFF)
            },

            // ========================= ☀️ 浅色系列 (Light Themes) =========================
            // 6. ☀️ 珍珠晨曦 (Pearl Azure) - 清爽极简象牙白与蔚蓝海风
            new ThemeDefinition
            {
                Id = "pearl",
                Name = "珍珠晨曦",
                Icon = "☀️",
                Description = "纯净雅白与清爽天青海风，明亮通透",
                IsDark = false,
                BgColor = Color.FromRgb(0xFA, 0xFA, 0xFD),
                CardBgColor = Color.FromRgb(0xF0, 0xF2, 0xF6),
                BorderColor = Color.FromArgb(0x40, 0x02, 0x84, 0xC7),
                AccentColor = Color.FromRgb(0x02, 0x84, 0xC7),
                AccentHoverColor = Color.FromRgb(0x03, 0x69, 0xA1),
                AccentTextColor = Color.FromRgb(0xFF, 0xFF, 0xFF),
                TextColor = Color.FromRgb(0x0F, 0x17, 0x2A),
                TextMutedColor = Color.FromRgb(0x64, 0x74, 0x8B),
                BadgeBgColor = Color.FromArgb(0x18, 0x02, 0x84, 0xC7),
                HoverBgColor = Color.FromArgb(0x0E, 0x02, 0x84, 0xC7),
                HighlightBgColor = Color.FromArgb(0x25, 0x02, 0x84, 0xC7),
                SelectionBorderColor = Color.FromRgb(0x02, 0x84, 0xC7)
            },

            // 7. 🌸 樱花粉雪 (Sakura Blossom) - 浪漫柔白与樱粉
            new ThemeDefinition
            {
                Id = "sakura",
                Name = "樱花粉雪",
                Icon = "🌸",
                Description = "浪漫柔白与淡雅樱粉高光，治愈甜美",
                IsDark = false,
                BgColor = Color.FromRgb(0xFD, 0xF4, 0xF6),
                CardBgColor = Color.FromRgb(0xFF, 0xEE, 0xF2),
                BorderColor = Color.FromArgb(0x45, 0xFB, 0x71, 0x85),
                AccentColor = Color.FromRgb(0xE1, 0x1D, 0x48),
                AccentHoverColor = Color.FromRgb(0xBE, 0x12, 0x3C),
                AccentTextColor = Color.FromRgb(0xFF, 0xFF, 0xFF),
                TextColor = Color.FromRgb(0x4C, 0x05, 0x19),
                TextMutedColor = Color.FromRgb(0x9F, 0x12, 0x39),
                BadgeBgColor = Color.FromArgb(0x20, 0xFB, 0x71, 0x85),
                HoverBgColor = Color.FromArgb(0x14, 0xFB, 0x71, 0x85),
                HighlightBgColor = Color.FromArgb(0x26, 0xFB, 0x71, 0x85),
                SelectionBorderColor = Color.FromRgb(0xE1, 0x1D, 0x48)
            },

            // 8. 🍵 浅山抹茶 (Matcha Nature) - 护眼柔和草木抹茶青
            new ThemeDefinition
            {
                Id = "matcha",
                Name = "浅山抹茶",
                Icon = "🍵",
                Description = "柔和米白与草木抹茶绿，极其护眼自然",
                IsDark = false,
                BgColor = Color.FromRgb(0xF4, 0xF9, 0xF5),
                CardBgColor = Color.FromRgb(0xE8, 0xF4, 0xEB),
                BorderColor = Color.FromArgb(0x45, 0x10, 0xB9, 0x81),
                AccentColor = Color.FromRgb(0x05, 0x96, 0x69),
                AccentHoverColor = Color.FromRgb(0x04, 0x78, 0x57),
                AccentTextColor = Color.FromRgb(0xFF, 0xFF, 0xFF),
                TextColor = Color.FromRgb(0x06, 0x4E, 0x3B),
                TextMutedColor = Color.FromRgb(0x3B, 0x7A, 0x57),
                BadgeBgColor = Color.FromArgb(0x20, 0x10, 0xB9, 0x81),
                HoverBgColor = Color.FromArgb(0x12, 0x10, 0xB9, 0x81),
                HighlightBgColor = Color.FromArgb(0x25, 0x10, 0xB9, 0x81),
                SelectionBorderColor = Color.FromRgb(0x05, 0x96, 0x69)
            },

            // 9. 🍯 暖阳琥珀 (Warm Amber) - 温暖羊皮纸本与蜜金
            new ThemeDefinition
            {
                Id = "amber",
                Name = "暖阳琥珀",
                Icon = "🍯",
                Description = "暖感羊皮纸与琥珀蜜金，温馨雅致",
                IsDark = false,
                BgColor = Color.FromRgb(0xFD, 0xFB, 0xF7),
                CardBgColor = Color.FromRgb(0xF5, 0xEF, 0xE5),
                BorderColor = Color.FromArgb(0x45, 0xF5, 0x9E, 0x0B),
                AccentColor = Color.FromRgb(0xD9, 0x77, 0x06),
                AccentHoverColor = Color.FromRgb(0xB4, 0x53, 0x09),
                AccentTextColor = Color.FromRgb(0xFF, 0xFF, 0xFF),
                TextColor = Color.FromRgb(0x45, 0x1A, 0x03),
                TextMutedColor = Color.FromRgb(0x92, 0x40, 0x0E),
                BadgeBgColor = Color.FromArgb(0x20, 0xF5, 0x9E, 0x0B),
                HoverBgColor = Color.FromArgb(0x14, 0xF5, 0x9E, 0x0B),
                HighlightBgColor = Color.FromArgb(0x26, 0xF5, 0x9E, 0x0B),
                SelectionBorderColor = Color.FromRgb(0xD9, 0x77, 0x06)
            },

            // 10. ❄️ 极光白昼 (Nordic Frost) - 北欧冷灰雅白与极光靛
            new ThemeDefinition
            {
                Id = "nordic",
                Name = "极光白昼",
                Icon = "❄️",
                Description = "北欧冷灰雅白与极光靛蓝，现代高级",
                IsDark = false,
                BgColor = Color.FromRgb(0xF1, 0xF5, 0xF9),
                CardBgColor = Color.FromRgb(0xE2, 0xE8, 0xF0),
                BorderColor = Color.FromArgb(0x45, 0x63, 0x66, 0xF1),
                AccentColor = Color.FromRgb(0x4F, 0x46, 0xE5),
                AccentHoverColor = Color.FromRgb(0x43, 0x38, 0xCA),
                AccentTextColor = Color.FromRgb(0xFF, 0xFF, 0xFF),
                TextColor = Color.FromRgb(0x0F, 0x17, 0x2A),
                TextMutedColor = Color.FromRgb(0x64, 0x74, 0x8B),
                BadgeBgColor = Color.FromArgb(0x18, 0x4F, 0x46, 0xE5),
                HoverBgColor = Color.FromArgb(0x10, 0x4F, 0x46, 0xE5),
                HighlightBgColor = Color.FromArgb(0x24, 0x63, 0x66, 0xF1),
                SelectionBorderColor = Color.FromRgb(0x4F, 0x46, 0xE5)
            },

            // 11. 🪻 紫藤花语 (Lavender Mist) - 优雅薰衣草浅紫
            new ThemeDefinition
            {
                Id = "lavender",
                Name = "紫藤花语",
                Icon = "🪻",
                Description = "优雅薰衣草浅紫与丁香高光，唯美柔和",
                IsDark = false,
                BgColor = Color.FromRgb(0xF8, 0xF6, 0xFD),
                CardBgColor = Color.FromRgb(0xF0, 0xEB, 0xFA),
                BorderColor = Color.FromArgb(0x45, 0x8B, 0x5C, 0xF6),
                AccentColor = Color.FromRgb(0x7C, 0x3A, 0xED),
                AccentHoverColor = Color.FromRgb(0x6D, 0x28, 0xD9),
                AccentTextColor = Color.FromRgb(0xFF, 0xFF, 0xFF),
                TextColor = Color.FromRgb(0x2E, 0x10, 0x65),
                TextMutedColor = Color.FromRgb(0x7E, 0x22, 0xCE),
                BadgeBgColor = Color.FromArgb(0x20, 0x8B, 0x5C, 0xF6),
                HoverBgColor = Color.FromArgb(0x12, 0x8B, 0x5C, 0xF6),
                HighlightBgColor = Color.FromArgb(0x25, 0x8B, 0x5C, 0xF6),
                SelectionBorderColor = Color.FromRgb(0x7C, 0x3A, 0xED)
            },

            // 12. 🍊 蜜柑苏打 (Citrus Soda) - 活力橙色与纯白
            new ThemeDefinition
            {
                Id = "citrus",
                Name = "蜜柑苏打",
                Icon = "🍊",
                Description = "元气蜜柑暖橙与晨曦白，活力满满",
                IsDark = false,
                BgColor = Color.FromRgb(0xFF, 0xFB, 0xF5),
                CardBgColor = Color.FromRgb(0xFF, 0xF2, 0xE2),
                BorderColor = Color.FromArgb(0x45, 0xFB, 0x92, 0x3C),
                AccentColor = Color.FromRgb(0xEA, 0x58, 0x0C),
                AccentHoverColor = Color.FromRgb(0xC2, 0x41, 0x0C),
                AccentTextColor = Color.FromRgb(0xFF, 0xFF, 0xFF),
                TextColor = Color.FromRgb(0x43, 0x14, 0x07),
                TextMutedColor = Color.FromRgb(0x9A, 0x34, 0x12),
                BadgeBgColor = Color.FromArgb(0x20, 0xFB, 0x92, 0x3C),
                HoverBgColor = Color.FromArgb(0x14, 0xFB, 0x92, 0x3C),
                HighlightBgColor = Color.FromArgb(0x26, 0xFB, 0x92, 0x3C),
                SelectionBorderColor = Color.FromRgb(0xEA, 0x58, 0x0C)
            }
        };

        public static ThemeDefinition GetTheme(string? themeId, bool isDarkDefault = true)
        {
            if (!string.IsNullOrEmpty(themeId))
            {
                var match = Themes.FirstOrDefault(x => x.Id.Equals(themeId, StringComparison.OrdinalIgnoreCase));
                if (match != null) return match;
            }

            return isDarkDefault ? Themes[0] : Themes.First(x => !x.IsDark);
        }

        public static void ApplyTheme(Window window, ThemeDefinition theme)
        {
            var res = window.Resources;

            // 1. MainWindow 资源键
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

            // 2. ManageWindow 资源键
            res["WindowBg"] = new SolidColorBrush(theme.BgColor);
            res["CardBg"] = new SolidColorBrush(theme.CardBgColor);
            res["TextPrimary"] = new SolidColorBrush(theme.TextColor);
            res["TextSecondary"] = new SolidColorBrush(theme.TextMutedColor);
            res["InputBg"] = new SolidColorBrush(theme.CardBgColor);
            res["InputBorder"] = new SolidColorBrush(theme.BorderColor);
            res["TableSelectBg"] = new SolidColorBrush(theme.HighlightBgColor);
            res["TableSelectBorder"] = new SolidColorBrush(theme.SelectionBorderColor);
            res["TableSelectText"] = new SolidColorBrush(theme.TextColor);
            res["TableSelectSubText"] = new SolidColorBrush(theme.AccentHoverColor);

            // 3. 同步 DWM 窗口暗色标题栏特性
            try
            {
                DwmHelper.ApplyModernWindowStyles(window, theme.IsDark);
            }
            catch { }
        }

        public static void ApplyThemeToAll(ThemeDefinition theme)
        {
            if (Application.Current != null)
            {
                foreach (Window win in Application.Current.Windows)
                {
                    ApplyTheme(win, theme);
                }
            }
        }
    }
}
