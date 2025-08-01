using UtaformatixData.Models;

namespace UtaformatixData.Utils
{
    /// <summary>
    /// 日本語の歌詞や音素から口の形（母音）を判定するユーティリティクラス。リップシンクアニメーションの基礎処理を行います。
    /// </summary>
    public static class LipShapeUtil
    {
        /// <summary>
        /// Noteオブジェクトから母音（口の形）を取得します。Phoneme情報を優先し、ない場合は歌詞から判定します。
        /// </summary>
        /// <param name="note">処理対象のNoteオブジェクト</param>
        /// <returns>対応する口の形（LipShape）</returns>
        public static LipShape GetVowelFromNote(Note note)
        {
            if (note == null)
            {
                return LipShape.N;
            }

            // Prioritize phoneme if available
            if (!string.IsNullOrEmpty(note.Phoneme))
            {
                return ExtractVowelFromPhoneme(note.Phoneme);
            }

            // Fall back to lyric analysis
            return GetVowelForLyric(note.Lyric);
        }

        /// <summary>
        /// 歌詞テキストから母音（口の形）を取得します。日本語のひらがな・カタカナに対応しています。
        /// </summary>
        /// <param name="lyric">処理対象の歌詞テキスト</param>
        /// <returns>対応する口の形（LipShape）</returns>
        public static LipShape GetVowelForLyric(string lyric)
        {
            if (string.IsNullOrEmpty(lyric))
            {
                return LipShape.N;
            }

            // Analyze the last character for Japanese vowel determination
            var lastChar = lyric[lyric.Length - 1];
            return ExtractVowelFromCharacter(lastChar);
        }

        /// <summary>
        /// 音素文字列から母音を抽出します。一般的な音素表記（子音+母音）に対応しています。
        /// </summary>
        /// <param name="phoneme">音素文字列（例：ka, si, tuなど）</param>
        /// <returns>対応する口の形（LipShape）</returns>
        private static LipShape ExtractVowelFromPhoneme(string phoneme)
        {
            if (string.IsNullOrEmpty(phoneme))
            {
                return LipShape.N;
            }

            // Common phoneme patterns (last character usually indicates vowel)
            var lastChar = phoneme[^1];

            return lastChar switch
            {
                'a' => LipShape.A,
                'i' => LipShape.I,
                'u' or 'M' => LipShape.U,
                'e' => LipShape.E,
                'o' => LipShape.O,
                'N' or 'n' or 'm' or 'j' => LipShape.N,
                _ => ExtractVowelFromPhonemeAdvanced(phoneme)
            };
        }

        /// <summary>
        /// 複雑な音素パターンに対する高度な解析。基本的なパターンマッチングで処理できない場合に使用します。
        /// </summary>
        /// <param name="phoneme">解析対象の音素文字列</param>
        /// <returns>対応する口の形（LipShape）</returns>
        private static LipShape ExtractVowelFromPhonemeAdvanced(string phoneme)
        {
            var lowerPhoneme = phoneme.ToLower();

            return lowerPhoneme switch
            {
                var p when p.Contains("a") => LipShape.A,
                var p when p.Contains("i") => LipShape.I,
                var p when p.Contains("u") || p.Contains("M") => LipShape.U,
                var p when p.Contains("e") => LipShape.E,
                var p when p.Contains("o") => LipShape.O,
                var p when p.Contains("n") || p.Contains("N") => LipShape.N,
                _ => LipShape.N
            };
        }

        /// <summary>
        /// 日本語文字（ひらがな・カタカナ）から母音を抽出します。あかさたなの五十音表に対応しています。
        /// </summary>
        /// <param name="character">処理対象の日本語文字</param>
        /// <returns>対応する口の形（LipShape）</returns>
        private static LipShape ExtractVowelFromCharacter(char character)
        {
            return character switch
            {
                // A sounds (あ行)
                'あ' or 'か' or 'が' or 'さ' or 'ざ' or 'た' or 'だ' or 'な' or 'は' or 'ば' or 'ぱ' or 'ま' or 'や' or 'ら' or 'わ' or
                'ア' or 'カ' or 'ガ' or 'サ' or 'ザ' or 'タ' or 'ダ' or 'ナ' or 'ハ' or 'バ' or 'パ' or 'マ' or 'ヤ' or 'ラ' or 'ワ' => LipShape.A,

                // I sounds (い行)
                'い' or 'き' or 'ぎ' or 'し' or 'じ' or 'ち' or 'ぢ' or 'に' or 'ひ' or 'び' or 'ぴ' or 'み' or 'り' or
                'イ' or 'キ' or 'ギ' or 'シ' or 'ジ' or 'チ' or 'ヂ' or 'ニ' or 'ヒ' or 'ビ' or 'ピ' or 'ミ' or 'リ' => LipShape.I,

                // U sounds (う行)
                'う' or 'く' or 'ぐ' or 'す' or 'ず' or 'つ' or 'づ' or 'ぬ' or 'ふ' or 'ぶ' or 'ぷ' or 'む' or 'ゆ' or 'る' or
                'ウ' or 'ク' or 'グ' or 'ス' or 'ズ' or 'ツ' or 'ヅ' or 'ヌ' or 'フ' or 'ブ' or 'プ' or 'ム' or 'ユ' or 'ル' => LipShape.U,

                // E sounds (え行)
                'え' or 'け' or 'げ' or 'せ' or 'ぜ' or 'て' or 'で' or 'ね' or 'へ' or 'べ' or 'ぺ' or 'め' or 'れ' or
                'エ' or 'ケ' or 'ゲ' or 'セ' or 'ゼ' or 'テ' or 'デ' or 'ネ' or 'ヘ' or 'ベ' or 'ペ' or 'メ' or 'レ' => LipShape.E,

                // O sounds (お行)
                'お' or 'こ' or 'ご' or 'そ' or 'ぞ' or 'と' or 'ど' or 'の' or 'ほ' or 'ぼ' or 'ぽ' or 'も' or 'よ' or 'ろ' or 'を' or
                'オ' or 'コ' or 'ゴ' or 'ソ' or 'ゾ' or 'ト' or 'ド' or 'ノ' or 'ホ' or 'ボ' or 'ポ' or 'モ' or 'ヨ' or 'ロ' or 'ヲ' => LipShape.O,

                // N sounds
                'ん' or 'ン' => LipShape.N,

                // Default
                _ => LipShape.N
            };
        }
    }
}
