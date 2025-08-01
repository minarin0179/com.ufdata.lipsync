using Newtonsoft.Json;
using System.Collections.Generic;

namespace UtaformatixData.Models
{
    /// <summary>
    /// UFDataのルートオブジェクト。フォーマットバージョンとプロジェクト情報を保持します。
    /// </summary>
    public class UFData
    {
        [JsonProperty("formatVersion")]
        public int FormatVersion { get; set; }

        [JsonProperty("project")]
        public Project Project { get; set; }
    }

    /// <summary>
    /// プロジェクト情報。トラック、拍子記号、テンポ情報などを含みます。
    /// </summary>
    public class Project
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("tracks")]
        public List<Track> Tracks { get; set; }

        [JsonProperty("timeSignatures")]
        public List<TimeSignature> TimeSignatures { get; set; }

        [JsonProperty("tempos")]
        public List<Tempo> Tempos { get; set; }

        [JsonProperty("measurePrefixCount")]
        public int MeasurePrefixCount { get; set; }
    }

    /// <summary>
    /// 音楽トラック情報。ノートやピッチ情報を含みます。
    /// </summary>
    public class Track
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("notes")]
        public List<Note> Notes { get; set; }

        [JsonProperty("pitch")]
        public Pitch Pitch { get; set; }
    }

    /// <summary>
    /// 音符（歌詞）情報。音程、開始・終了タイミング、歌詞、音素情報を保持します。
    /// </summary>
    public class Note
    {
        [JsonProperty("key")]
        public int Key { get; set; }

        [JsonProperty("tickOn")]
        public long TickOn { get; set; }

        [JsonProperty("tickOff")]
        public long TickOff { get; set; }

        [JsonProperty("lyric")]
        public string Lyric { get; set; }

        [JsonProperty("phoneme")]
        public string Phoneme { get; set; }
    }

    /// <summary>
    /// ピッチ情報。変化したポイントのみが含まれます。
    /// </summary>
    public class Pitch
    {
        [JsonProperty("ticks")]
        public List<long> Ticks { get; set; }

        [JsonProperty("values")]
        public List<double?> Values { get; set; }

        [JsonProperty("isAbsolute")]
        public bool IsAbsolute { get; set; }
    }

    /// <summary>
    /// 拍子記号情報。小節位置、分子、分母を保持します。
    /// </summary>
    public class TimeSignature
    {
        [JsonProperty("measurePosition")]
        public int MeasurePosition { get; set; }

        [JsonProperty("numerator")]
        public int Numerator { get; set; }

        [JsonProperty("denominator")]
        public int Denominator { get; set; }
    }

    /// <summary>
    /// テンポ情報。Tick位置とBPMを保持します。
    /// </summary>
    public class Tempo
    {
        [JsonProperty("tickPosition")]
        public long TickPosition { get; set; }

        [JsonProperty("bpm")]
        public int Bpm { get; set; }
    }
}
