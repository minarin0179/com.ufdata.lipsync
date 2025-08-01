using System;

namespace UtaformatixData.Models
{
    /// <summary>
    /// 日本語の母音を表す口の形の列挙型。リップシンクアニメーションで使用します。
    /// </summary>
    public enum LipShape
    {
        A, I, U, E, O, N
    }

    /// <summary>
    /// 歌詞のタイミング情報と対応する口の形を表すクラス。リップシンクアニメーション生成の基礎データです。
    /// </summary>
    public class LyricTiming
    {
        public double StartSeconds { get; }
        public double EndSeconds { get; }
        public LipShape Vowel { get; }

        public LyricTiming(double startSeconds, double endSeconds, LipShape vowel)
        {
            StartSeconds = startSeconds;
            EndSeconds = endSeconds;
            Vowel = vowel;
        }

        /// <summary>
        /// 歌詞の継続時間（秒）。EndSeconds - StartSecondsで計算されます。
        /// </summary>
        public double Duration => EndSeconds - StartSeconds;

        /// <summary>
        /// パターンマッチング用のDeconstructメソッド。タプル分解で使用できます。
        /// </summary>
        public void Deconstruct(out double startSeconds, out double endSeconds, out LipShape vowel)
        {
            startSeconds = StartSeconds;
            endSeconds = EndSeconds;
            vowel = Vowel;
        }

        public override string ToString()
        {
            return $"歌詞タイミング(開始: {StartSeconds:F2}秒, 終了: {EndSeconds:F2}秒, 母音: {Vowel}, 継続: {Duration:F2}秒)";
        }
    }
}
