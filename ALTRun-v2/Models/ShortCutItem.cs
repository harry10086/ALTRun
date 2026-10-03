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

    public class ShortCutItem
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

        [JsonIgnore]
        public ImageSource? IconSource { get; set; }

        [JsonIgnore]
        public string DisplayIndex { get; set; } = string.Empty;

        [JsonIgnore]
        public int MatchScore { get; set; } = 0;

        [JsonIgnore]
        public bool IsSpecialCommand { get; set; } = false;

        public ShortCutItem Clone()
        {
            return new ShortCutItem
            {
                Id = this.Id,
                ShortCut = this.ShortCut,
                Name = this.Name,
                CommandLine = this.CommandLine,
                WorkingDir = this.WorkingDir,
                Freq = this.Freq,
                ParamType = this.ParamType,
                PinyinInitials = this.PinyinInitials,
                PinyinShortcut = this.PinyinShortcut,
                PinyinFull = this.PinyinFull
            };
        }
    }
}
