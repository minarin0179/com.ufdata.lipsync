using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UtaformatixData.Models;

namespace UtaformatixData.Editor.LipSync
{
    /// <summary>
    /// VRMモデル設定とBlendShape検出結果表示を担当
    /// </summary>
    public class VRMSettingsUI
    {
        private VRMBlendShapeDetector.BlendShapeDetectionResult _detectionResult;
        private ManualBlendShapeSelector _manualSelector;
        private LipSyncGeneratorSettings _settings;

        public GameObject VrmModel => _settings?.VrmModel;
        public VRMBlendShapeDetector.BlendShapeDetectionResult DetectionResult => _detectionResult;
        public string TargetFacePath => _detectionResult?.HasValidMappings == true
            ? _detectionResult.TargetPath
            : _settings?.TargetFacePath ?? "Face";
        public Dictionary<LipShape, string> VowelToBlendShape => _detectionResult?.HasValidMappings == true
            ? _detectionResult.DetectedBlendShapes
            : _settings?.GetManualBlendShapeMapping() ?? new Dictionary<LipShape, string>();

        public VRMSettingsUI()
        {
            _manualSelector = new ManualBlendShapeSelector();
        }

        public void Initialize(LipSyncGeneratorSettings settings)
        {
            _settings = settings;

            // 保存された設定から復元
            if (_settings.VrmModel != null)
            {
                AnalyzeVrmModel();
            }
        }

        public void Draw()
        {
            if (_settings == null)
            {
                return;
            }

            EditorGUILayout.LabelField("VRMモデル設定", EditorStyles.boldLabel);

            var newVrmModel = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("VRMモデル", "VRMモデルのPrefabまたはシーン内のGameObjectを選択"),
                _settings.VrmModel,
                typeof(GameObject),
                true
            );

            if (newVrmModel != _settings.VrmModel)
            {
                _settings.VrmModel = newVrmModel;
                _settings.SaveSettings();
                AnalyzeVrmModel();
            }

            if (_settings.VrmModel != null && _detectionResult != null)
            {
                DrawBlendShapeDetectionResults();
            }

            EditorGUILayout.Space();
        }

        private void AnalyzeVrmModel()
        {
            if (_settings?.VrmModel == null)
            {
                _detectionResult = null;
                _manualSelector.ClearAvailableBlendShapes();
                return;
            }

            _detectionResult = VRMBlendShapeDetector.DetectLipSyncBlendShapes(_settings.VrmModel);

            // デバッグ用：すべてのBlendShape名をログ出力
            List<VRMBlendShapeDetector.RendererInfo> detailedInfo = VRMBlendShapeDetector.GetDetailedBlendShapeInfo(_settings.VrmModel);
            foreach (var info in detailedInfo)
            {
                Debug.Log($"[VRM BlendShape Debug] パス: {info.Path}");
                Debug.Log($"[VRM BlendShape Debug] BlendShape名: {string.Join(", ", info.BlendShapeNames)}");
            }

            // 手動選択用のBlendShape名リストを準備
            _manualSelector.PrepareAvailableBlendShapes(detailedInfo);

            if (_detectionResult.HasValidMappings)
            {
                Debug.Log($"[LipSyncAnimationGenerator] VRMモデルから{_detectionResult.DetectedBlendShapes.Count}個のBlendShapeを検出: {_detectionResult.TargetPath}");
                _settings.TargetFacePath = _detectionResult.TargetPath;
                _settings.UseManualBlendShapeSelection = false;
                _settings.SaveSettings();
            }
            else
            {
                Debug.LogWarning("[LipSyncAnimationGenerator] VRMモデルから音素に対応するBlendShapeが見つかりませんでした。");
                _manualSelector.InitializeManualSelection();
                _settings.UseManualBlendShapeSelection = true;
                _settings.SaveSettings();
            }
        }

        private void DrawBlendShapeDetectionResults()
        {
            if (_detectionResult.HasValidMappings)
            {
                EditorGUILayout.LabelField($"対象パス: {_detectionResult.TargetPath}", EditorStyles.miniBoldLabel);

                EditorGUILayout.LabelField("検出されたマッピング:", EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;
                foreach (var mapping in _detectionResult.DetectedBlendShapes)
                {
                    EditorGUILayout.LabelField($"{mapping.Key}: {mapping.Value}");
                }
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.HelpBox("音素に対応するBlendShapeが見つかりませんでした。手動設定で対応してください。", MessageType.Warning);

                EditorGUILayout.Space();
                _manualSelector.Draw();

                // 手動設定の変更を保存
                var manualMapping = _manualSelector.VowelToBlendShape;
                if (manualMapping != null)
                {
                    _settings.SetManualBlendShapeMapping(manualMapping);
                    _settings.TargetFacePath = _manualSelector.TargetFacePath ?? "Face";
                    _settings.SaveSettings();
                }

                if (GUILayout.Button("詳細情報を表示", GUILayout.Height(20)))
                {
                    ShowDetailedBlendShapeInfo();
                }
            }
        }

        private void ShowDetailedBlendShapeInfo()
        {
            if (_settings?.VrmModel == null) return;

            List<VRMBlendShapeDetector.RendererInfo> detailedInfo = VRMBlendShapeDetector.GetDetailedBlendShapeInfo(_settings.VrmModel);
            Dictionary<LipShape, string[]> supportedPatterns = VRMBlendShapeDetector.GetSupportedPatterns();

            var message = "=== VRMモデル BlendShape 詳細情報 ===\n\n";

            foreach (VRMBlendShapeDetector.RendererInfo info in detailedInfo)
            {
                message += $"パス: {info.Path}\n";
                message += $"BlendShape数: {info.BlendShapeCount}\n";
                message += $"検出された音素: {info.DetectedVowelCount}\n";
                message += $"BlendShape名: {string.Join(", ", info.BlendShapeNames)}\n\n";
            }

            message += "=== サポートされるパターン ===\n";
            foreach (var pattern in supportedPatterns)
            {
                message += $"{pattern.Key}: {string.Join(", ", pattern.Value)}\n";
            }

            Debug.Log(message);
            EditorUtility.DisplayDialog("BlendShape詳細情報", "コンソールに詳細情報を出力しました。", "OK");
        }

        public bool ValidateInputs()
        {
            if (_settings?.VrmModel == null)
            {
                EditorUtility.DisplayDialog("エラー", "VRMモデルを選択してください。", "OK");
                return false;
            }

            // VRMモデルが選択されているが自動検出に失敗した場合は手動設定を確認
            if (_detectionResult == null || !_detectionResult.HasValidMappings)
            {
                return _manualSelector.ValidateInputs();
            }

            return true;
        }
    }
}
