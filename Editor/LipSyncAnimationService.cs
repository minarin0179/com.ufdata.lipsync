using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UtaformatixData.Models;
using UtaformatixData.Services;

namespace UtaformatixData.Editor.LipSync
{
    /// <summary>
    /// リップシンクアニメーション生成のビジネスロジックを担当
    /// </summary>
    public class LipSyncAnimationService
    {
        private const float BlendShapeMaxValue = 100f;

        public struct AnimationSettings
        {
            public float MaxFadeDuration;
            public float FadeTimeRatio;
            public string OutputPath;
            public string TargetFacePath;
            public Dictionary<LipShape, string> VowelToBlendShape;
        }

        public void GenerateAnimation(UFData ufData, int trackIndex, string[] trackNames, string jsonFileName, string modelName, AnimationSettings settings)
        {
            try
            {
                EditorUtility.DisplayProgressBar("リップシンク生成", "歌詞タイミングを処理中...", 0.3f);
                var lyricsTiming = GetLyricsTiming(ufData, trackIndex);

                EditorUtility.DisplayProgressBar("リップシンク生成", "アニメーションクリップを作成中...", 0.5f);
                var animClip = GetOrCreateAnimationClip(trackNames, trackIndex, jsonFileName, modelName, settings.OutputPath);

                EditorUtility.DisplayProgressBar("リップシンク生成", "BlendShapeアニメーションを生成中...", 0.7f);
                GenerateBlendShapeAnimation(animClip.clip, lyricsTiming, settings);

                EditorUtility.DisplayProgressBar("リップシンク生成", "アニメーションを保存中...", 0.9f);
                SaveAnimationClip(animClip.clip, animClip.isNew, animClip.path, settings.OutputPath);

                Debug.Log($"[LipSyncAnimationGenerator] Animation generated successfully: {animClip.path}");
                EditorUtility.DisplayDialog("成功", $"リップシンクアニメーションが正常に生成されました！\n\n保存先: {animClip.path}", "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private List<LyricTiming> GetLyricsTiming(UFData ufData, int trackIndex)
        {
            var calc = new UtaformatixTimingCalculator(ufData.Project);
            return calc.GetLyricsWithTiming(trackIndex);
        }

        private (AnimationClip clip, bool isNew, string path) GetOrCreateAnimationClip(string[] trackNames, int trackIndex, string jsonFileName, string modelName, string outputPath)
        {
            var trackSuffix = trackNames.Length > 0 ? $"_Track{trackIndex}" : "";
            var fileName = $"LipSync_{modelName}_{jsonFileName}{trackSuffix}.anim";
            var assetPath = $"{outputPath}/{fileName}";

            AnimationClip existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            var isNewClip = existingClip == null;

            AnimationClip animClip;
            if (isNewClip)
            {
                animClip = new AnimationClip { name = $"LipSyncAnimation_{modelName}" };
            }
            else
            {
                animClip = existingClip;
                animClip.ClearCurves();
            }

            return (animClip, isNewClip, assetPath);
        }

        private void GenerateBlendShapeAnimation(AnimationClip animClip, List<LyricTiming> lyricsTiming, AnimationSettings settings)
        {
            Dictionary<string, AnimationCurve> blendShapeCurves = InitializeBlendShapeCurves(settings.VowelToBlendShape);

            foreach (LyricTiming timing in lyricsTiming)
            {
                ProcessLyricTiming(blendShapeCurves, timing, settings);
            }

            ApplyCurvesToAnimationClip(animClip, blendShapeCurves, settings.TargetFacePath);
        }

        private Dictionary<string, AnimationCurve> InitializeBlendShapeCurves(Dictionary<LipShape, string> vowelToBlendShape)
        {
            var curves = new Dictionary<string, AnimationCurve>();
            foreach (var blendShapeName in vowelToBlendShape.Values)
            {
                curves[blendShapeName] = new AnimationCurve();
            }
            return curves;
        }

        private void ProcessLyricTiming(Dictionary<string, AnimationCurve> blendShapeCurves, LyricTiming timing, AnimationSettings settings)
        {
            if (!settings.VowelToBlendShape.ContainsKey(timing.Vowel))
            {
                return;
            }

            var targetBlendShape = settings.VowelToBlendShape[timing.Vowel];
            AnimationCurve curve = blendShapeCurves[targetBlendShape];

            var fadeTime = CalculateFadeTime(timing.Duration, settings.MaxFadeDuration, settings.FadeTimeRatio);
            (var actualStartTime, var actualEndTime) = ResolveTimeOverlap(curve, timing, fadeTime);

            AddKeyframesToCurve(curve, timing, actualStartTime, actualEndTime, fadeTime);
        }

        private float CalculateFadeTime(double lyricDuration, float maxFadeDuration, float fadeTimeRatio)
            => Mathf.Min(maxFadeDuration, (float)(lyricDuration * fadeTimeRatio));

        private (float startTime, float endTime) ResolveTimeOverlap(AnimationCurve curve, LyricTiming timing, float fadeTime)
        {
            var actualStartTime = (float)(timing.StartSeconds - fadeTime);
            var actualEndTime = (float)timing.EndSeconds;

            if (curve.keys.Length > 0)
            {
                Keyframe lastKey = curve.keys[curve.keys.Length - 1];
                if (lastKey.time > actualStartTime)
                {
                    var midPoint = (lastKey.time + actualStartTime) / 2f;
                    curve.RemoveKey(curve.keys.Length - 1);
                    curve.AddKey(new Keyframe(midPoint, 0f));
                    actualStartTime = midPoint;
                }
            }

            return (actualStartTime, actualEndTime);
        }

        private void AddKeyframesToCurve(AnimationCurve curve, LyricTiming timing, float actualStartTime, float actualEndTime, float fadeTime)
        {
            // フェードイン
            curve.AddKey(new Keyframe(actualStartTime, 0f));
            curve.AddKey(new Keyframe((float)timing.StartSeconds, BlendShapeMaxValue));

            // 維持（長い歌詞の場合）
            if (timing.Duration > fadeTime * 2)
            {
                curve.AddKey(new Keyframe((float)(timing.EndSeconds - fadeTime), BlendShapeMaxValue));
            }

            // フェードアウト
            curve.AddKey(new Keyframe(actualEndTime, 0f));
        }

        private void ApplyCurvesToAnimationClip(AnimationClip animClip, Dictionary<string, AnimationCurve> blendShapeCurves, string targetFacePath)
        {
            foreach ((var blendShapeName, AnimationCurve curve) in blendShapeCurves)
            {
                if (curve.keys.Length > 0)
                {
                    var binding = new EditorCurveBinding
                    {
                        path = targetFacePath,
                        type = typeof(SkinnedMeshRenderer),
                        propertyName = blendShapeName
                    };

                    AnimationUtility.SetEditorCurve(animClip, binding, curve);
                }
            }
        }

        private void SaveAnimationClip(AnimationClip animClip, bool isNewClip, string assetPath, string outputPath)
        {
            if (isNewClip)
            {
                Directory.CreateDirectory(outputPath);
                AssetDatabase.CreateAsset(animClip, assetPath);
                Debug.Log($"[LipSyncAnimationGenerator] New animation created: {assetPath}");
            }
            else
            {
                EditorUtility.SetDirty(animClip);
                Debug.Log($"[LipSyncAnimationGenerator] Animation updated: {assetPath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
