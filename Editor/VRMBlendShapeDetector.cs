using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;

using UtaformatixData.Models;

namespace UtaformatixData.Editor
{
    /// <summary>
    /// アバターモデル（VRM、Unitychan、VRChatアバターなど）からリップシンク用のBlendShapeを自動検出するクラス
    /// </summary>
    public static class AvatarBlendShapeDetector
    {
        /// <summary>
        /// アバターモデルのBlendShape名パターン（音素別）
        /// VRM、Unitychan、VRChat、MMDなど各種アバター形式に対応
        /// </summary>
        private static readonly Dictionary<LipShape, string[]> _avatarBlendShapePatterns = new()
        {
            {LipShape.A, new string[] {"A", "a", "Fcl_MTH_A", "blendShape.A", "blendShape.Fcl_MTH_A", "vrc.v_aa", "MTH_A", "mouth_a", "A02", "Mouth_A", "あ", "Viseme_AA"}},
            {LipShape.I, new string[] {"I", "i", "Fcl_MTH_I", "blendShape.I", "blendShape.Fcl_MTH_I", "vrc.v_ih", "MTH_I", "mouth_i", "I02", "Mouth_I", "い", "Viseme_IH"}},
            {LipShape.U, new string[] {"U", "u", "Fcl_MTH_U", "blendShape.U", "blendShape.Fcl_MTH_U", "vrc.v_ou", "MTH_U", "mouth_u", "U02", "Mouth_U", "う", "Viseme_OU"}},
            {LipShape.E, new string[] {"E", "e", "Fcl_MTH_E", "blendShape.E", "blendShape.Fcl_MTH_E", "vrc.v_e", "MTH_E", "mouth_e", "E02", "Mouth_E", "え", "Viseme_EH"}},
            {LipShape.O, new string[] {"O", "o", "Fcl_MTH_O", "blendShape.O", "blendShape.Fcl_MTH_O", "vrc.v_oh", "MTH_O", "mouth_o", "O02", "Mouth_O", "お", "Viseme_OH"}}
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
        /// アバターモデルからリップシンク用BlendShapeを検出
        /// </summary>
        /// <param name="avatarModel">検出対象のアバターモデル（VRM、Unitychan、VRChatアバターなど）</param>
        /// <returns>検出結果</returns>
        public static BlendShapeDetectionResult DetectLipSyncBlendShapes(GameObject avatarModel)
        {
            // UnityEngine.Debug.Log($"[AvatarBlendShapeDetector] 検出開始: {avatarModel?.name ?? "null"}");
            
            var result = new BlendShapeDetectionResult();

            if (avatarModel == null)
            {
                // UnityEngine.Debug.LogWarning("[AvatarBlendShapeDetector] avatarModelがnullです");
                return result;
            }

            try
            {
                // モデル内のSkinnedMeshRendererを取得（再帰的に全階層を検索）
                // UnityEngine.Debug.Log("[AvatarBlendShapeDetector] SkinnedMeshRenderer取得中...");
                SkinnedMeshRenderer[] renderers = avatarModel.GetComponentsInChildren<SkinnedMeshRenderer>()
                    .Where(r => r.sharedMesh != null && r.sharedMesh.blendShapeCount > 0)
                    .ToArray();

                // UnityEngine.Debug.Log($"[AvatarBlendShapeDetector] 見つかったRenderer数: {renderers.Length}");
                result.AvailableRenderers = renderers;

                // より効率的な検索: 顔関連オブジェクトを優先し、完全マッチを最優先
                var bestMapping = new Dictionary<LipShape, string>();
                var bestPath = "";
                var bestScore = 0;

                // 顔関連オブジェクト名のパターン（優先度順）
                var faceObjectPatterns = new string[] { "face", "head", "Face", "Head", "顔", "頭" };

                foreach (SkinnedMeshRenderer renderer in renderers)
                {
                    try
                    {
                        // UnityEngine.Debug.Log($"[AvatarBlendShapeDetector] Renderer処理中: {renderer.gameObject.name}");
                        
                        List<string> blendShapeNames = GetBlendShapeNames(renderer);
                        Dictionary<LipShape, string> foundMappings = FindBlendShapeMappings(blendShapeNames);

                        // UnityEngine.Debug.Log($"[AvatarBlendShapeDetector] 見つかったマッピング数: {foundMappings.Count}");

                        if (foundMappings.Count > 0)
                        {
                            // スコア計算: マッピング数 + 顔関連オブジェクトボーナス + 階層の浅さボーナス
                            int score = foundMappings.Count * 10;
                            
                            // 顔関連オブジェクト名が含まれる場合はボーナス
                            string objName = renderer.gameObject.name.ToLower();
                            if (faceObjectPatterns.Any(pattern => objName.Contains(pattern.ToLower())))
                            {
                                score += 20;
                            }
                            
                            // 階層が浅いほどボーナス（より直接的なアクセス）
                            int depth = GetObjectDepth(renderer.gameObject, avatarModel);
                            score += Math.Max(0, 10 - depth);

                            // 全ての音素が揃っている場合は大きなボーナス
                            if (foundMappings.Count == _avatarBlendShapePatterns.Count)
                            {
                                score += 50;
                            }

                            // UnityEngine.Debug.Log($"[AvatarBlendShapeDetector] スコア: {score}");

                            if (score > bestScore)
                            {
                                bestMapping = foundMappings;
                                bestPath = GetGameObjectPath(renderer.gameObject, avatarModel);
                                bestScore = score;
                                // UnityEngine.Debug.Log($"[AvatarBlendShapeDetector] 新しいベスト: {bestPath}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // UnityEngine.Debug.LogError($"[AvatarBlendShapeDetector] Renderer処理エラー: {ex.Message}");
                    }
                }

                result.DetectedBlendShapes = bestMapping;
                result.TargetPath = bestPath;

                // UnityEngine.Debug.Log($"[AvatarBlendShapeDetector] 検出完了. 最終マッピング数: {bestMapping.Count}");
                return result;
            }
            catch (Exception ex)
            {
                // UnityEngine.Debug.LogError($"[AvatarBlendShapeDetector] 検出処理でエラー: {ex.Message}");
                return result;
            }
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

            foreach (LipShape vowel in _avatarBlendShapePatterns.Keys)
            {
                var possibleNames = _avatarBlendShapePatterns[vowel];

                // 汎用的なパターンマッチング（完全一致、末尾一致、正規表現）
                foreach (var pattern in possibleNames)
                {
                    var foundName = FindBlendShapeByPattern(blendShapeNames, pattern);

                    if (!string.IsNullOrEmpty(foundName))
                    {
                        // BlendShape名の形式を判定：既にblendShape.プレフィックスがある場合はそのまま、ない場合は追加
                        mappings[vowel] = foundName.StartsWith("blendShape.") ? foundName : $"blendShape.{foundName}";
                        break;
                    }
                }
            }

            return mappings;
        }

        /// <summary>
        /// 汎用的なBlendShape名パターンマッチング
        /// </summary>
        private static string FindBlendShapeByPattern(List<string> blendShapeNames, string pattern)
        {
            try
            {
                // UnityEngine.Debug.Log($"[BlendShape検索] パターン: '{pattern}' で検索中...");

                if (string.IsNullOrEmpty(pattern) || blendShapeNames == null || blendShapeNames.Count == 0)
                {
                    // UnityEngine.Debug.Log("[BlendShape検索] パターンまたはBlendShape名リストが無効");
                    return null;
                }

                // 1. 完全一致を優先
                var foundName = blendShapeNames.FirstOrDefault(name =>
                    string.Equals(name, pattern, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(foundName))
                {
                    // UnityEngine.Debug.Log($"[BlendShape検索] 完全一致で発見: '{foundName}'");
                    return foundName;
                }

                // 2. 末尾一致（区切り文字対応）
                foundName = blendShapeNames.FirstOrDefault(name =>
                    name.EndsWith("." + pattern, StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith("_" + pattern, StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith("-" + pattern, StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith(" " + pattern, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(foundName))
                {
                    // UnityEngine.Debug.Log($"[BlendShape検索] 末尾一致で発見: '{foundName}'");
                    return foundName;
                }

                // 3. より安全な部分一致（正規表現を使わない）
                foundName = blendShapeNames.FirstOrDefault(name =>
                    name.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0);

                if (!string.IsNullOrEmpty(foundName))
                {
                    // UnityEngine.Debug.Log($"[BlendShape検索] 部分一致で発見: '{foundName}'");
                    return foundName;
                }

                // UnityEngine.Debug.Log($"[BlendShape検索] パターン '{pattern}' は見つかりませんでした");
                return null;
            }
            catch (Exception ex)
            {
                // UnityEngine.Debug.LogError($"[BlendShape検索] エラー発生: {ex.Message}");
                return null;
            }
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
        /// GameObjectのルートからの階層の深さを取得
        /// </summary>
        private static int GetObjectDepth(GameObject target, GameObject root)
        {
            if (target == root)
            {
                return 0;
            }

            int depth = 0;
            Transform current = target.transform.parent;

            while (current != null && current.gameObject != root)
            {
                depth++;
                current = current.parent;
            }

            return depth + 1; // 自分自身の分も加算
        }

        /// <summary>
        /// 検出可能な音素BlendShapeパターンの説明を取得
        /// </summary>
        public static Dictionary<LipShape, string[]> GetSupportedPatterns() => new(_avatarBlendShapePatterns);

        /// <summary>
        /// デバッグ用：指定されたアバターモデルのBlendShape検出プロセスをログ出力
        /// </summary>
        public static void DebugBlendShapeDetection(GameObject avatarModel)
        {
            if (avatarModel == null)
            {
                UnityEngine.Debug.Log("アバターモデルがnullです");
                return;
            }

            UnityEngine.Debug.Log($"=== BlendShape検出デバッグ: {avatarModel.name} ===");

            SkinnedMeshRenderer[] renderers = avatarModel.GetComponentsInChildren<SkinnedMeshRenderer>()
                .Where(r => r.sharedMesh != null && r.sharedMesh.blendShapeCount > 0)
                .ToArray();

            UnityEngine.Debug.Log($"BlendShapeを持つRenderer数: {renderers.Length}");

            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                UnityEngine.Debug.Log($"\n--- Renderer: {renderer.gameObject.name} ---");
                UnityEngine.Debug.Log($"パス: {GetGameObjectPath(renderer.gameObject, avatarModel)}");
                
                List<string> blendShapeNames = GetBlendShapeNames(renderer);
                UnityEngine.Debug.Log($"BlendShape数: {blendShapeNames.Count}");
                UnityEngine.Debug.Log($"BlendShape名: [{string.Join(", ", blendShapeNames)}]");
                
                Dictionary<LipShape, string> foundMappings = FindBlendShapeMappings(blendShapeNames);
                UnityEngine.Debug.Log($"検出された音素マッピング: {foundMappings.Count}個");
                
                foreach (var mapping in foundMappings)
                {
                    UnityEngine.Debug.Log($"  {mapping.Key} -> {mapping.Value}");
                }
            }

            UnityEngine.Debug.Log("=== デバッグ終了 ===");
        }

        /// <summary>
        /// アバターモデルの詳細BlendShape情報を取得
        /// </summary>
        public static List<RendererInfo> GetDetailedBlendShapeInfo(GameObject avatarModel)
        {
            var infos = new List<RendererInfo>();

            if (avatarModel == null)
            {
                return infos;
            }

            SkinnedMeshRenderer[] renderers = avatarModel.GetComponentsInChildren<SkinnedMeshRenderer>();

            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                if (renderer.sharedMesh == null || renderer.sharedMesh.blendShapeCount == 0)
                {
                    continue;
                }

                var info = new RendererInfo
                {
                    Renderer = renderer,
                    Path = GetGameObjectPath(renderer.gameObject, avatarModel),
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
