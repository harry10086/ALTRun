using System;
using System.Collections.Generic;
using System.Text;

namespace ALTRun.Services
{
    public static class PinyinHelper
    {
        private static readonly Encoding? GbEncoding;

        // GB2312 一级常用字（共 3755 字，严格按拼音首字母正序排列）的 23 个声母分区边界点
        private static readonly int[] SecPosValueList = new int[]
        {
            0xB0A1, 0xB0C5, 0xB2C1, 0xB4EE, 0xB6EA, 0xB7A2, 0xB8C1, 0xB9FE,
            0xBBF7, 0xBFA6, 0xC0AC, 0xC2E8, 0xC4FF, 0xC5B6, 0xC5BE, 0xC6DA,
            0xC8BB, 0xC8F6, 0xCBFA, 0xCDDA, 0xCEF4, 0xD1B9, 0xD4D1, 0xD7FA
        };

        private static readonly char[] FirstLetterList = new char[]
        {
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h',
            'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q',
            'r', 's', 't', 'w', 'x', 'y', 'z'
        };

        // 常见特殊汉字、高频二级字、专有名词直通常用字典
        private static readonly Dictionary<char, char> SpecialCharMap = new()
        {
            { '哔', 'b' }, { '哩', 'l' }, { '钉', 'd' }, { '酷', 'k' }, { '易', 'y' },
            { '腾', 't' }, { '讯', 'x' }, { '微', 'w' }, { '信', 'x' }, { '截', 'j' },
            { '图', 't' }, { '计', 'j' }, { '算', 's' }, { '器', 'q' }, { '端', 'd' },
            { '设', 's' }, { '置', 'z' }, { '管', 'g' }, { '理', 'l' }, { '工', 'g' },
            { '具', 'j' }, { '控', 'k' }, { '制', 'z' }, { '任', 'r' }, { '务', 'w' },
            { '浏', 'l' }, { '览', 'l' }, { '编', 'b' }, { '辑', 'j' }, { '画', 'h' },
            { '播', 'b' }, { '放', 'f' }, { '相', 'x' }, { '机', 'j' }, { '天', 't' },
            { '气', 'q' }, { '日', 'r' }, { '历', 'l' }, { '笔', 'b' }, { '记', 'j' },
            { '邮', 'y' }, { '件', 'j' }, { '游', 'y' }, { '戏', 'x' }, { '商', 's' },
            { '店', 'd' }, { '系', 'x' }, { '统', 't' }, { '视', 's' }, { '频', 'p' },
            { '音', 'y' }, { '乐', 'y' }, { '百', 'b' }, { '度', 'd' }, { '歌', 'g' },
            { '谷', 'g' }, { '网', 'w' }, { '络', 'l' }, { '蓝', 'l' }, { '牙', 'y' },
            { '声', 's' }, { '狼', 'l' }, { '毫', 'h' }, { '小', 'x' }, { '抖', 'd' },
            { '拼', 'p' }, { '奇', 'q' }, { '艺', 'y' }, { '淘', 't' }, { '宝', 'b' },
            { '阿', 'a' }, { '里', 'l' }, { '暴', 'b' }, { '风', 'f' }, { '狗', 'g' }
        };

        static PinyinHelper()
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                GbEncoding = Encoding.GetEncoding("GB18030");
            }
            catch
            {
                try
                {
                    GbEncoding = Encoding.GetEncoding(936);
                }
                catch
                {
                    GbEncoding = null;
                }
            }
        }

        /// <summary>
        /// 提取中文文本拼音首字母序列 (例如 "计算器" -> "jsq", "截图工具" -> "jtgj")
        /// </summary>
        public static string GetInitials(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var sb = new StringBuilder();
            try
            {
                foreach (char c in text)
                {
                    // 1. 英文字母与数字直接保留小写
                    if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                    {
                        sb.Append(c);
                    }
                    else if (c >= 'A' && c <= 'Z')
                    {
                        sb.Append(char.ToLowerInvariant(c));
                    }
                    // 2. 特配与二级字直接查表
                    else if (SpecialCharMap.TryGetValue(c, out char mapped))
                    {
                        sb.Append(mapped);
                    }
                    // 3. GB2312 一级字库区间快速精确定位 (3755 个标准汉字)
                    else if (c >= 0x4E00 && c <= 0x9FA5 && GbEncoding != null)
                    {
                        byte[] bytes = GbEncoding.GetBytes(new char[] { c });
                        if (bytes.Length >= 2)
                        {
                            int code = (bytes[0] << 8) + bytes[1];
                            char letter = ' ';
                            for (int i = 0; i < 23; i++)
                            {
                                if (code >= SecPosValueList[i] && code < SecPosValueList[i + 1])
                                {
                                    letter = FirstLetterList[i];
                                    break;
                                }
                            }
                            if (letter != ' ')
                            {
                                sb.Append(letter);
                            }
                        }
                    }
                }
            }
            catch
            {
                // 异常保底
            }
            return sb.ToString();
        }
    }
}
