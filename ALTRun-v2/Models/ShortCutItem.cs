using System;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace ALTRun.Models
{
    public enum ParamType
    {
        ptNone,
        ptNoEncoding,
        ptURLQuery,
        ptUTF8Query
    }

    /// <summary>
    /// 快捷项数据模型。改用 record class：`with` 表达式按值复制所有字段，
    /// 新增字段时无需再手动维护拷贝方法的字段列表，避免漏拷。
    /// </summary>
    public record ShortCutItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string ShortCut { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CommandLine { get; set; } = string.Empty;
        public string WorkingDir { get; set; } = string.Empty;
        public int Freq { get; set; } = 0;
        public ParamType ParamType { get; set; } = ParamType.ptNone;

        [JsonIgnore]
        public string PinyinInitials { get; set; } = string.Empty;

        [JsonIgnore]
        public string PinyinShortcut { get; set; } = string.Empty;

        [JsonIgnore]
        public string PinyinFull { get; set; } = string.Empty;

        // 预计算的小写缓存：搜索时直接复用，避免每个字符都执行 ToLowerInvariant 分配新字符串
        [JsonIgnore]
        public string LowerShortCut { get; set; } = string.Empty;

        [JsonIgnore]
        public string LowerName { get; set; } = string.Empty;

        [JsonIgnore]
        public string LowerPinyinInitials { get; set; } = string.Empty;

        [JsonIgnore]
        public string LowerPinyinShortcut { get; set; } = string.Empty;

        [JsonIgnore]
        public ImageSource? IconSource { get; set; }

        [JsonIgnore]
        public string DisplayIndex { get; set; } = string.Empty;

        [JsonIgnore]
        public int MatchScore { get; set; } = 0;

        [JsonIgnore]
        public bool IsSpecialCommand { get; set; } = false;

        /// <summary>
        /// 复制出一个用于展示/搜索的副本。用 record 的 `with` 按值复制所有持久化字段；
        /// 运行时字段（图标、序号、匹配分、特殊命令标记）显式重置，保持与原手写拷贝逻辑一致——
        /// 这些字段属于展示态，不应从源项继承，需由搜索/渲染流程重新计算。
        /// </summary>
        public ShortCutItem CopyForDisplay()
        {
            return this with
            {
                IconSource = null,
                DisplayIndex = string.Empty,
                MatchScore = 0,
                IsSpecialCommand = false
            };
        }
    }
}
