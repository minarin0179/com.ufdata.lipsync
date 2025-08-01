using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UtaformatixData.Models;

namespace UtaformatixData.Editor.LipSync
{
    /// <summary>
    /// アバターモデル設定とBlendShape検出結果表示を担当
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
            _manualSelector.Initialize(_settings);

            // Initialize時は前回の設定を復元するのみ。自動検出は実行しない
            if (_settings.VrmModel != null)
            {
                // 前回の設定を復元するために詳細情報を準備
                EditorApplication.delayCall += () => {
                    if (_settings?.VrmModel != null)
                    {
                        RestorePreviousSettings();
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

            EditorGUILayout.LabelField("アバターモデル設定", EditorStyles.boldLabel);

            var newVrmModel = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("アバターモデル", "VRM、Unitychan、VRChatアバターなどのPrefabまたはシーン内のGameObjectを選択"),
                _settings.VrmModel,
                typeof(GameObject),
                true
            );

            if (newVrmModel != _settings.VrmModel)
            {
                _settings.VrmModel = newVrmModel;
                
                // アバターモデルがアタッチされたタイミングで自動検出を実行
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
                    EditorGUILayout.HelpBox("BlendShapeを検出するにはボタンを押してください。", MessageType.Info);
                    
                    // 手動検出ボタンを表示
                    if (GUILayout.Button("BlendShapeを検出", GUILayout.Height(25)))
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

        private void RestorePreviousSettings()
        {
            if (_settings?.VrmModel == null)
            {
                return;
            }

            // 詳細情報を取得（手動選択用）
            List<AvatarBlendShapeDetector.RendererInfo> detailedInfo = AvatarBlendShapeDetector.GetDetailedBlendShapeInfo(_settings.VrmModel);
            _manualSelector.PrepareAvailableBlendShapes(detailedInfo);

            // 前回の設定に基づいて復元
            if (_settings.UseManualBlendShapeSelection)
            {
                // 手動設定を復元
                var savedMapping = _settings.GetManualBlendShapeMapping();
                _manualSelector.RestoreManualMappingWithIndices(savedMapping);
                
                // 手動設定がある場合は検出結果ありとして扱う（UIに手動設定を表示するため）
                _detectionResult = new AvatarBlendShapeDetector.BlendShapeDetectionResult();
            }
            else
            {
                // 前回の設定がない場合は、検出ボタンを表示するためnullのまま
                _detectionResult = null;
            }
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
                _settings.SaveSettings();
                
                // 検出結果をマニュアルセレクターにデフォルト選択として設定
                _manualSelector.SetDetectionResults(_detectionResult.DetectedBlendShapes, _detectionResult.TargetPath);
            }
            else
            {
                // 失敗時：保存された手動設定があれば復元、なければ初期化
                if (_settings.UseManualBlendShapeSelection)
                {
                    var savedMapping = _settings.GetManualBlendShapeMapping();
                    _manualSelector.RestoreManualMappingWithIndices(savedMapping);
                }
                else
                {
                    _manualSelector.InitializeManualSelection();
                }
            }
        }

        public bool ValidateInputs()
        {
            if (_settings?.VrmModel == null)
            {
                EditorUtility.DisplayDialog("エラー", "アバターモデルを選択してください。", "OK");
                return false;
            }

            // アバターモデルが選択されているが自動検出に失敗した場合は手動設定を確認
            if (_detectionResult == null || !_detectionResult.HasValidMappings)
            {
                return _manualSelector.ValidateInputs();
            }

            return true;
        }
    }
}
