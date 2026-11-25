using System;
using System.Collections.Generic;
using System.Globalization;
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
        public static SearchResult Search(string rawInput, List<ShortCutItem> allItems)
        {
            var result = new SearchResult { RawQuery = rawInput };
            if (string.IsNullOrWhiteSpace(rawInput))
            {
                // 空输入时，展示最常用的 Top 10
                var topList = allItems
                    .OrderByDescending(x => x.Freq)
                    .Take(10)
                    .Select(x => x.CopyForDisplay())
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
                    var copy = item.CopyForDisplay();
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
            // 优先使用预计算缓存；临时构造项（计算器/终端命令）缓存为空时实时计算兜底。
            // 配置被手工编辑成 "ShortCut": null 时旧写法的 ToLowerInvariant() 会抛空引用，因此统一用 ?? "" 兜底。
            string lowerShortcut = string.IsNullOrEmpty(item.LowerShortCut)
                ? (item.ShortCut ?? "").ToLowerInvariant() : item.LowerShortCut;
            string lowerName = string.IsNullOrEmpty(item.LowerName)
                ? (item.Name ?? "").ToLowerInvariant() : item.LowerName;
            string pinyinName = string.IsNullOrEmpty(item.LowerPinyinInitials)
                ? (item.PinyinInitials ?? "").ToLowerInvariant() : item.LowerPinyinInitials;
            string pinyinShortcut = string.IsNullOrEmpty(item.LowerPinyinShortcut)
                ? (item.PinyinShortcut ?? "").ToLowerInvariant() : item.LowerPinyinShortcut;

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
            // 手写递归下降解析器，零 System.Data 依赖，避免 DataTable.Compute 的表达式注入风险。
            return MathExpressionEvaluator.TryEvaluate(expr, out double value)
                ? value.ToString("G", CultureInfo.InvariantCulture)
                : null;
        }

        /// <summary>
        /// 轻量四则运算求值器：支持 + - * / %、括号、一元正负号与小数。
        /// 递归下降文法：expr := term {('+'|'-') term}；term := factor {('*'|'/'|'%') factor}；
        /// factor := ('+'|'-')? primary；primary := number | '(' expr ')'。
        /// </summary>
        private class MathExpressionEvaluator
        {
            /// <summary>递归深度上限。正常算式远达不到；纯粹用于阻挡 "(((..." / "----..." 这类恶意的超深嵌套。</summary>
            private const int MaxNestingDepth = 64;

            private readonly string _text;
            private int _pos;
            private int _depth;

            private MathExpressionEvaluator(string text) => _text = text;

            public static bool TryEvaluate(string expression, out double result)
            {
                result = 0;
                if (string.IsNullOrWhiteSpace(expression)) return false;
                try
                {
                    var parser = new MathExpressionEvaluator(expression);
                    double value = parser.ParseExpr();
                    parser.SkipWhiteSpace();
                    // 必须消耗完整输入，否则视为非法表达式（如 "1+2abc"）
                    if (parser._pos != parser._text.Length) return false;
                    if (double.IsNaN(value) || double.IsInfinity(value)) return false;
                    result = value;
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            /// <summary>
            /// 进入一层递归。超过 MaxNestingDepth 立即抛出 FormatException——
            /// 递归下降解析器的栈帧无法通过 try/catch 从 StackOverflow 恢复（后者会直接终止进程），
            /// 因此必须在耗尽栈之前主动中断。
            /// </summary>
            private void EnterRecursion()
            {
                if (++_depth > MaxNestingDepth)
                {
                    _depth--;
                    throw new FormatException("表达式嵌套层级过深");
                }
            }

            private void SkipWhiteSpace()
            {
                while (_pos < _text.Length && char.IsWhiteSpace(_text[_pos])) _pos++;
            }

            private bool TryConsume(char c)
            {
                SkipWhiteSpace();
                if (_pos < _text.Length && _text[_pos] == c) { _pos++; return true; }
                return false;
            }

            // expr := term {('+'|'-') term}
            private double ParseExpr()
            {
                EnterRecursion();
                try
                {
                    double left = ParseTerm();
                    while (true)
                    {
                        SkipWhiteSpace();
                        if (_pos >= _text.Length) break;
                        char op = _text[_pos];
                        if (op != '+' && op != '-') break;
                        _pos++;
                        double right = ParseTerm();
                        left = op == '+' ? left + right : left - right;
                    }
                    return left;
                }
                finally
                {
                    _depth--;
                }
            }

            // term := factor {('*'|'/'|'%') factor}
            private double ParseTerm()
            {
                double left = ParseFactor();
                while (true)
                {
                    SkipWhiteSpace();
                    if (_pos >= _text.Length) break;
                    char op = _text[_pos];
                    if (op != '*' && op != '/' && op != '%') break;
                    _pos++;
                    double right = ParseFactor();
                    if ((op == '/' || op == '%') && right == 0)
                        throw new DivideByZeroException();
                    left = op switch
                    {
                        '*' => left * right,
                        '/' => left / right,
                        _ => left % right
                    };
                }
                return left;
            }

            // factor := ('+'|'-')? primary
            private double ParseFactor()
            {
                EnterRecursion();
                try
                {
                    if (TryConsume('+')) return ParseFactor();
                    if (TryConsume('-')) return -ParseFactor();
                    return ParsePrimary();
                }
                finally
                {
                    _depth--;
                }
            }

            // primary := number | '(' expr ')'
            private double ParsePrimary()
            {
                SkipWhiteSpace();
                if (TryConsume('('))
                {
                    double value = ParseExpr();
                    if (!TryConsume(')')) throw new FormatException("缺少右括号");
                    return value;
                }

                int start = _pos;
                while (_pos < _text.Length && (char.IsDigit(_text[_pos]) || _text[_pos] == '.')) _pos++;
                if (start == _pos) throw new FormatException("缺少操作数");
                string number = _text.Substring(start, _pos - start);
                if (!double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                    throw new FormatException("非法数字");
                return result;
            }
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
