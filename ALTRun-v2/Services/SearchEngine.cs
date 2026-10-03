using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ALTRun.Models;

namespace ALTRun.Services
{
    public class SearchResult
    {
        public List<ShortCutItem> Items { get; set; } = new();
        public string ExtractedParam { get; set; } = string.Empty;
        public string RawQuery { get; set; } = string.Empty;
    }

    public class SearchEngine
    {
        private static readonly DataTable EvalTable = new();

        public static SearchResult Search(string rawInput, List<ShortCutItem> allItems)
        {
            var result = new SearchResult { RawQuery = rawInput };
            if (string.IsNullOrWhiteSpace(rawInput))
            {
                // 空输入时，展示最常用的 Top 10
                var topList = allItems
                    .OrderByDescending(x => x.Freq)
                    .Take(10)
                    .Select(x => x.Clone())
                    .ToList();

                AssignDisplayIndex(topList);
                result.Items = topList;
                return result;
            }

            string input = rawInput.Trim();

            // 1. 命令行直达模式: 以 > 开头
            if (input.StartsWith(">"))
            {
                string cmdToRun = input.Substring(1).Trim();
                var cmdItem = new ShortCutItem
                {
                    ShortCut = ">",
                    Name = $"执行终端命令: {cmdToRun}",
                    CommandLine = $"RUNCMD:{cmdToRun}",
                    DisplayIndex = "1",
                    IsSpecialCommand = true
                };
                result.Items.Add(cmdItem);
                return result;
            }

            // 2. 即时计算器模式: 以 = 开头，或包含算术运算符
            if (input.StartsWith("=") || IsMathExpression(input))
            {
                string expr = input.StartsWith("=") ? input.Substring(1).Trim() : input;
                string? calcVal = TryEvaluateMath(expr);
                if (!string.IsNullOrEmpty(calcVal))
                {
                    var calcItem = new ShortCutItem
                    {
                        ShortCut = "=",
                        Name = $"计算结果: {calcVal} (回车复制)",
                        CommandLine = $"COPY:{calcVal}",
                        DisplayIndex = "1",
                        IsSpecialCommand = true
                    };
                    result.Items.Add(calcItem);
                }
            }

            // 3. 分离关键词与参数 (例如 "g rust" -> 关键词 "g", 参数 "rust")
            string keyword = input;
            string param = string.Empty;
            int spaceIndex = input.IndexOf(' ');
            if (spaceIndex > 0)
            {
                keyword = input.Substring(0, spaceIndex).Trim();
                param = input.Substring(spaceIndex + 1).Trim();
            }
            result.ExtractedParam = param;

            string lowerKeyword = keyword.ToLowerInvariant();

            // 4. 对所有快捷项进行加权打分
            var matchedList = new List<ShortCutItem>();
            foreach (var item in allItems)
            {
                int score = CalculateMatchScore(item, lowerKeyword);
                if (score > 0)
                {
                    var copy = item.Clone();
                    copy.MatchScore = score;
                    matchedList.Add(copy);
                }
            }

            // 排序: 匹配分最高优先，其次按使用频次 Freq
            var finalItems = matchedList
                .OrderByDescending(x => x.MatchScore)
                .ThenByDescending(x => x.Freq)
                .Take(10)
                .ToList();

            // 如果已有计算器条目，插在最前
            if (result.Items.Count > 0)
            {
                finalItems.InsertRange(0, result.Items);
            }

            AssignDisplayIndex(finalItems);
            result.Items = finalItems;
            return result;
        }

        private static int CalculateMatchScore(ShortCutItem item, string lowerKeyword)
        {
            string lowerShortcut = item.ShortCut.ToLowerInvariant();
            string lowerName = item.Name.ToLowerInvariant();
            string pinyinName = item.PinyinInitials.ToLowerInvariant();
            string pinyinShortcut = item.PinyinShortcut.ToLowerInvariant();

            int baseScore = 0;

            // 1. 完全精确匹配 (ShortCut 或 拼音首字母，如输入 jsq 完全命中 计算器)
            if (lowerShortcut == lowerKeyword)
            {
                baseScore = 10000;
            }
            else if (!string.IsNullOrEmpty(pinyinName) && pinyinName == lowerKeyword)
            {
                baseScore = 9000;
            }
            else if (!string.IsNullOrEmpty(pinyinShortcut) && pinyinShortcut == lowerKeyword)
            {
                baseScore = 8500;
            }
            // 2. 前缀匹配 (如输入 js 前缀命中 计算器 jsq)
            else if (lowerShortcut.StartsWith(lowerKeyword))
            {
                baseScore = 7000 - (lowerShortcut.Length - lowerKeyword.Length) * 10;
            }
            else if (!string.IsNullOrEmpty(pinyinName) && pinyinName.StartsWith(lowerKeyword))
            {
                baseScore = 6500 - (pinyinName.Length - lowerKeyword.Length) * 10;
            }
            else if (!string.IsNullOrEmpty(pinyinShortcut) && pinyinShortcut.StartsWith(lowerKeyword))
            {
                baseScore = 6400 - (pinyinShortcut.Length - lowerKeyword.Length) * 10;
            }
            // 3. 包含匹配 (如输入 q 包含命中 计算器 jsq)
            else if (lowerShortcut.Contains(lowerKeyword))
            {
                baseScore = 3500;
            }
            else if (!string.IsNullOrEmpty(pinyinName) && pinyinName.Contains(lowerKeyword))
            {
                baseScore = 3200;
            }
            else if (!string.IsNullOrEmpty(pinyinShortcut) && pinyinShortcut.Contains(lowerKeyword))
            {
                baseScore = 3100;
            }
            // 4. 名称中文直接包含匹配 (如输入 "计算" 包含命中 "计算器")
            else if (lowerName.Contains(lowerKeyword))
            {
                baseScore = 3000;
            }
            // 5. 模糊跳字匹配 (Fuzzy Match: 如 "calc" 匹配 "clc", "jsq" 匹配 "jq")
            else if (IsFuzzyMatch(lowerShortcut, lowerKeyword) ||
                     IsFuzzyMatch(lowerName, lowerKeyword) ||
                     (!string.IsNullOrEmpty(pinyinName) && IsFuzzyMatch(pinyinName, lowerKeyword)) ||
                     (!string.IsNullOrEmpty(pinyinShortcut) && IsFuzzyMatch(pinyinShortcut, lowerKeyword)))
            {
                baseScore = 1500;
            }

            if (baseScore > 0)
            {
                // 结合历史频次加权
                return baseScore + Math.Min(item.Freq * 5, 2000);
            }

            return 0;
        }

        private static bool IsFuzzyMatch(string target, string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) return true;
            if (string.IsNullOrEmpty(target)) return false;

            int patternIdx = 0;
            for (int i = 0; i < target.Length; i++)
            {
                if (target[i] == pattern[patternIdx])
                {
                    patternIdx++;
                    if (patternIdx == pattern.Length) return true;
                }
            }
            return false;
        }

        private static bool IsMathExpression(string text)
        {
            if (text.Length < 3) return false;
            bool hasOp = false;
            foreach (char c in text)
            {
                if (c == '+' || c == '-' || c == '*' || c == '/' || c == '%')
                    hasOp = true;
                else if (!char.IsDigit(c) && c != '.' && c != '(' && c != ')' && c != ' ')
                    return false;
            }
            return hasOp;
        }

        private static string? TryEvaluateMath(string expr)
        {
            try
            {
                var val = EvalTable.Compute(expr, null);
                if (val != null)
                {
                    return Convert.ToDouble(val).ToString("G");
                }
            }
            catch
            {
                // 忽略非合法数学式
            }
            return null;
        }

        private static void AssignDisplayIndex(List<ShortCutItem> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                // 1~9 以及 0
                items[i].DisplayIndex = ((i + 1) % 10).ToString();
            }
        }
    }
}
