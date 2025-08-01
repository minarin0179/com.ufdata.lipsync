using System.Collections.Generic;
using System.Linq;
using UtaformatixData.Models;
using UtaformatixData.Utils;

namespace UtaformatixData.Services
{
    /// <summary>
    /// UFDataのタイミング計算サービス。Tick単位のタイミングを秒単位に変換し、歌詞タイミングデータを抽出します。
    /// </summary>
    public class UtaformatixTimingCalculator
    {
        private readonly Project _project;
        private readonly List<Tempo> _tempos;

        public UtaformatixTimingCalculator(Project project)
        {
            this._project = project ?? throw new System.ArgumentNullException(nameof(project), "プロジェクトがnullです");

            if (project.Tempos == null || project.Tempos.Count == 0)
            {
                throw new System.ArgumentException("プロジェクトにはテンポ情報が必要です", nameof(project));
            }

            _tempos = project.Tempos.OrderBy(t => t.TickPosition).ToList();
        }

        /// <summary>
        /// 指定したTick位置を秒数に変換します。テンポ変化を考慮した正確な変換を行います。
        /// </summary>
        /// <param name="targetTick">変換したいTick位置</param>
        /// <returns>対応する時間（秒）</returns>
        public double TickToSeconds(long targetTick)
        {
            var seconds = 0.0;

            for (var i = 0; i < _tempos.Count; i++)
            {
                Tempo currentTempo = _tempos[i];
                var startTick = currentTempo.TickPosition;
                var endTick = (i + 1 < _tempos.Count) ? _tempos[i + 1].TickPosition : long.MaxValue;

                if (targetTick < startTick)
                {
                    break;
                }

                long ticksInThisTempo;
                if (targetTick < endTick)
                {
                    ticksInThisTempo = targetTick - startTick;
                    seconds += TicksToSeconds(ticksInThisTempo, currentTempo.Bpm);
                    break;
                }
                else
                {
                    ticksInThisTempo = endTick - startTick;
                    seconds += TicksToSeconds(ticksInThisTempo, currentTempo.Bpm);
                }
            }

            return seconds;
        }

        /// <summary>
        /// プロジェクトから歌詞とタイミング情報を抽出し、LyricTimingオブジェクトのリストとして返します。
        /// </summary>
        /// <returns>歌詞タイミング情報のリスト</returns>
        public List<LyricTiming> GetLyricsWithTiming()
        {
            var result = new List<LyricTiming>();

            if (_project.Tracks == null)
            {
                return result;
            }

            foreach (Track track in _project.Tracks)
            {
                if (track.Notes == null)
                {
                    continue;
                }

                var notes = track.Notes.OrderBy(n => n.TickOn).ToList();
                ProcessNotesIntoLyricTiming(notes, result);
            }

            return result;
        }

        /// <summary>
        /// 指定したトラックから歌詞とタイミング情報を抽出し、LyricTimingオブジェクトのリストとして返します。
        /// </summary>
        /// <param name="trackIndex">対象トラックのインデックス</param>
        /// <returns>歌詞タイミング情報のリスト</returns>
        public List<LyricTiming> GetLyricsWithTiming(int trackIndex)
        {
            var result = new List<LyricTiming>();

            if (_project.Tracks == null || trackIndex < 0 || trackIndex >= _project.Tracks.Count)
            {
                return result;
            }

            Track targetTrack = _project.Tracks[trackIndex];
            if (targetTrack.Notes == null)
            {
                return result;
            }

            var notes = targetTrack.Notes.OrderBy(n => n.TickOn).ToList();
            ProcessNotesIntoLyricTiming(notes, result);

            return result;
        }

        /// <summary>
        /// ノート情報を処理して歌詞タイミングデータに変換します。長音記号「ー」の処理も行います。
        /// </summary>
        /// <param name="notes">時間順にソートされたノートリスト</param>
        /// <param name="result">結果を追加するリスト</param>
        private void ProcessNotesIntoLyricTiming(List<Note> notes, List<LyricTiming> result)
        {
            for (var i = 0; i < notes.Count; i++)
            {
                Note note = notes[i];
                var startSeconds = TickToSeconds(note.TickOn);
                var endSeconds = TickToSeconds(note.TickOff);

                // Handle long vowel mark "ー" by extending previous note
                if (note.Lyric == "ー" && result.Count > 0)
                {
                    LyricTiming lastTiming = result[^1];
                    result[^1] = new LyricTiming(lastTiming.StartSeconds, endSeconds, lastTiming.Vowel);
                }
                else
                {
                    LipShape lipShape = LipShapeUtil.GetVowelFromNote(note);
                    result.Add(new LyricTiming(startSeconds, endSeconds, lipShape));
                }
            }
        }

        /// <summary>
        /// 指定したBPMでTick数を秒数に変換します。内部計算用のプライベートメソッドです。
        /// </summary>
        /// <param name="ticks">変換するTick数</param>
        /// <param name="bpm">基準となるBPM（テンポ）</param>
        /// <returns>対応する時間（秒）</returns>
        private double TicksToSeconds(long ticks, int bpm) =>
            // 精度を保つため、乗算を先に行い除算を後に行う
            ticks * 60.0 / (bpm * UFConstants.TicksPerQuarterNote);
    }
}
