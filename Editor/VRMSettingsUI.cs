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
        private AvatarBlendShapeDetector.BlendShapeDetectionResult _detectionResult;
        private ManualBlendShapeSelector _manualSelector;
        private LipSyncGeneratorSettings _settings;

        public GameObject VrmModel => _settings?.VrmModel;
        public AvatarBlendShapeDetector.BlendShapeDetectionResult DetectionResult => _detectionResult;
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

            // Initialize時にVRMモデルが既に設定されている場合は自動検出を実行
            if (_settings.VrmModel != null)
            {
                // EditorApplication.delayCallを使用してGUIループを回避
                EditorApplication.delayCall += () => {
                    if (_settings?.VrmModel != null)
                    {
                        AnalyzeVrmModel();
                    }
                };
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
                
                // VRMモデルがアタッチされたタイミングで自動検出を実行
                if (newVrmModel != null)
                {
                    // EditorApplication.delayCallを使用してGUIループを回避
                    EditorApplication.delayCall += () => {
                        if (_settings.VrmModel == newVrmModel)  // モデルが変更されていないか確認
                        {
                            AnalyzeVrmModel();
                        }
                    };
                }
                else
                {
                    _detectionResult = null;
                }
            }

            if (_settings.VrmModel != null)
            {
                if (_detectionResult == null)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox("BlendShapeを自動検出中...", MessageType.Info);
                    
                    // 手動検出ボタンも表示（念のため）
                    if (GUILayout.Button("手動で再検出", GUILayout.Height(20)))
                    {
                        AnalyzeVrmModel();
                    }
                }
                else
                {
                    // 検出結果に関係なく、常にプルダウンメニューを表示
                    EditorGUILayout.Space();
                    
                    if (_detectionResult.HasValidMappings)
                    {
                        EditorGUILayout.HelpBox($"自動検出成功！{_detectionResult.DetectedBlendShapes.Count}個のBlendShapeを検出しました。", MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("自動検出に失敗しました。手動でBlendShapeを選択してください。", MessageType.Warning);
                    }
                    
                    if (GUILayout.Button("再検出", GUILayout.Height(20)))
                    {
                        AnalyzeVrmModel();
                    }
                    
                    EditorGUILayout.Space();
                    
                    // 常にプルダウンメニューを表示
                    _manualSelector.Draw();
                }
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

            _detectionResult = AvatarBlendShapeDetector.DetectLipSyncBlendShapes(_settings.VrmModel);

            // Debug.Log("[VRMSettingsUI] DetectLipSyncBlendShapes完了");

            // 詳細情報を取得（手動選択用）
            List<AvatarBlendShapeDetector.RendererInfo> detailedInfo = AvatarBlendShapeDetector.GetDetailedBlendShapeInfo(_settings.VrmModel);
            _manualSelector.PrepareAvailableBlendShapes(detailedInfo);

            if (_detectionResult.HasValidMappings)
            {
                // 成功時のみ設定を更新
                _settings.TargetFacePath = _detectionResult.TargetPath;
                _settings.UseManualBlendShapeSelection = false;
                _settings.SaveSettings(); // SaveSettings復元
                
                // 検出結果をマニュアルセレクターにデフォルト選択として設定（TargetFacePathも一緒に設定）
                _manualSelector.SetDetectionResults(_detectionResult.DetectedBlendShapes, _detectionResult.TargetPath);
            }
            else
            {
                // 失敗時は手動選択を初期化するが設定変更は行わない
                _manualSelector.InitializeManualSelection();
                // _settings.UseManualBlendShapeSelection = true;
                // _settings.SaveSettings();
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

            List<AvatarBlendShapeDetector.RendererInfo> detailedInfo = AvatarBlendShapeDetector.GetDetailedBlendShapeInfo(_settings.VrmModel);
            Dictionary<LipShape, string[]> supportedPatterns = AvatarBlendShapeDetector.GetSupportedPatterns();

            var message = "=== VRMモデル BlendShape 詳細情報 ===\n\n";

            foreach (AvatarBlendShapeDetector.RendererInfo info in detailedInfo)
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
