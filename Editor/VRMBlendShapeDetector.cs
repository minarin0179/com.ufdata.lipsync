using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using UtaformatixData.Models;

namespace UtaformatixData.Editor
{
    /// <summary>
    /// VRMモデルからリップシンク用のBlendShapeを自動検出するクラス
    /// </summary>
    public static class VRMBlendShapeDetector
    {
        /// <summary>
        /// VRM標準のBlendShape名パターン（音素別）
        /// </summary>
        private static readonly Dictionary<LipShape, string[]> _vrmBlendShapePatterns = new()
        {
            {LipShape.A, new string[] {"A", "a", "Fcl_MTH_A", "blendShape.A", "blendShape.Fcl_MTH_A", "vrc.v_aa", "MTH_A"}},
            {LipShape.I, new string[] {"I", "i", "Fcl_MTH_I", "blendShape.I", "blendShape.Fcl_MTH_I", "vrc.v_ih", "MTH_I"}},
            {LipShape.U, new string[] {"U", "u", "Fcl_MTH_U", "blendShape.U", "blendShape.Fcl_MTH_U", "vrc.v_ou", "MTH_U"}},
            {LipShape.E, new string[] {"E", "e", "Fcl_MTH_E", "blendShape.E", "blendShape.Fcl_MTH_E", "vrc.v_eh", "MTH_E"}},
            {LipShape.O, new string[] {"O", "o", "Fcl_MTH_O", "blendShape.O", "blendShape.Fcl_MTH_O", "vrc.v_oh", "MTH_O"}}
        };

        /// <summary>
        /// BlendShape検出結果を格納するクラス
        /// </summary>
        public class BlendShapeDetectionResult
        {
            public Dictionary<LipShape, string> DetectedBlendShapes { get; set; } = new();
            public string TargetPath { get; set; } = "";
            public SkinnedMeshRenderer[] AvailableRenderers { get; set; } = new SkinnedMeshRenderer[0];
            public bool HasValidMappings => DetectedBlendShapes.Count > 0;
        }

        /// <summary>
        /// VRMモデルからリップシンク用BlendShapeを検出
        /// </summary>
        /// <param name="vrmModel">検出対象のVRMモデル</param>
        /// <returns>検出結果</returns>
        public static BlendShapeDetectionResult DetectLipSyncBlendShapes(GameObject vrmModel)
        {
            var result = new BlendShapeDetectionResult();

            if (vrmModel == null)
            {
                return result;
            }

            // モデル内のSkinnedMeshRendererを取得
            SkinnedMeshRenderer[] renderers = vrmModel.GetComponentsInChildren<SkinnedMeshRenderer>()
                .Where(r => r.sharedMesh != null && r.sharedMesh.blendShapeCount > 0)
                .ToArray();

            result.AvailableRenderers = renderers;

            // 最も多くの音素BlendShapeを持つRendererを検索
            var bestMapping = new Dictionary<LipShape, string>();
            var bestPath = "";

            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                List<string> blendShapeNames = GetBlendShapeNames(renderer);
                Dictionary<LipShape, string> foundMappings = FindBlendShapeMappings(blendShapeNames);

                if (foundMappings.Count > bestMapping.Count)
                {
                    bestMapping = foundMappings;
                    bestPath = GetGameObjectPath(renderer.gameObject, vrmModel);
                }
            }

            result.DetectedBlendShapes = bestMapping;
            result.TargetPath = bestPath;

            return result;
        }

        /// <summary>
        /// SkinnedMeshRendererからBlendShape名リストを取得
        /// </summary>
        private static List<string> GetBlendShapeNames(SkinnedMeshRenderer renderer)
        {
            var names = new List<string>();
            if (renderer.sharedMesh == null)
            {
                return names;
            }

            for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
            {
                names.Add(renderer.sharedMesh.GetBlendShapeName(i));
            }

            return names;
        }

        /// <summary>
        /// BlendShape名リストから音素マッピングを検索
        /// </summary>
        private static Dictionary<LipShape, string> FindBlendShapeMappings(List<string> blendShapeNames)
        {
            var mappings = new Dictionary<LipShape, string>();

            foreach (LipShape vowel in _vrmBlendShapePatterns.Keys)
            {
                var possibleNames = _vrmBlendShapePatterns[vowel];

                // 完全一致のみで検索
                foreach (var pattern in possibleNames)
                {
                    var foundName = blendShapeNames.FirstOrDefault(name =>
                        string.Equals(name, pattern, StringComparison.OrdinalIgnoreCase));

                    if (!string.IsNullOrEmpty(foundName))
                    {
                        mappings[vowel] = $"blendShape.{foundName}";
                        break;
                    }
                }
            }

            return mappings;
        }


        /// <summary>
        /// GameObjectの階層パスを取得
        /// </summary>
        private static string GetGameObjectPath(GameObject target, GameObject root)
        {
            if (target == root)
            {
                return target.name;
            }

            var path = target.name;
            Transform current = target.transform.parent;

            while (current != null && current.gameObject != root)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        /// <summary>
        /// 検出可能な音素BlendShapeパターンの説明を取得
        /// </summary>
        public static Dictionary<LipShape, string[]> GetSupportedPatterns() => new(_vrmBlendShapePatterns);

        /// <summary>
        /// VRMモデルの詳細BlendShape情報を取得
        /// </summary>
        public static List<RendererInfo> GetDetailedBlendShapeInfo(GameObject vrmModel)
        {
            var infos = new List<RendererInfo>();

            if (vrmModel == null)
            {
                return infos;
            }

            SkinnedMeshRenderer[] renderers = vrmModel.GetComponentsInChildren<SkinnedMeshRenderer>();

            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                if (renderer.sharedMesh == null || renderer.sharedMesh.blendShapeCount == 0)
                {
                    continue;
                }

                var info = new RendererInfo
                {
                    Renderer = renderer,
                    Path = GetGameObjectPath(renderer.gameObject, vrmModel),
                    BlendShapeNames = GetBlendShapeNames(renderer),
                    DetectedMappings = FindBlendShapeMappings(GetBlendShapeNames(renderer))
                };

                infos.Add(info);
            }

            return infos;
        }

        /// <summary>
        /// SkinnedMeshRenderer情報クラス
        /// </summary>
        public class RendererInfo
        {
            public SkinnedMeshRenderer Renderer { get; set; }
            public string Path { get; set; }
            public List<string> BlendShapeNames { get; set; } = new();
            public Dictionary<LipShape, string> DetectedMappings { get; set; } = new();
            public int BlendShapeCount => BlendShapeNames.Count;
            public int DetectedVowelCount => DetectedMappings.Count;
        }
    }
}
