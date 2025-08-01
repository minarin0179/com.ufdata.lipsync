using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UtaformatixData.Models;

namespace UtaformatixData.Editor.LipSync
{
    /// <summary>
    /// 手動BlendShape選択機能を担当
    /// </summary>
    public class ManualBlendShapeSelector
    {
        private string _targetFacePath = "Face";
        private Dictionary<LipShape, int> _manualBlendShapeIndices = new();
        private string[] _availableBlendShapeNames = new string[0];
        private Dictionary<LipShape, string> _vowelToBlendShape = new();
        private LipSyncGeneratorSettings _settings;

        public string TargetFacePath => _targetFacePath;
        public Dictionary<LipShape, string> VowelToBlendShape => _vowelToBlendShape;

        public void Initialize(LipSyncGeneratorSettings settings)
        {
            _settings = settings;
            
            // 保存された設定を復元
            if (_settings != null)
            {
                _targetFacePath = _settings.TargetFacePath;
                
                // 手動BlendShapeマッピングが設定されている場合は復元
                if (_settings.UseManualBlendShapeSelection)
                {
                    var savedMapping = _settings.GetManualBlendShapeMapping();
                    RestoreManualMapping(savedMapping);
                }
            }
        }

        public void Draw()
        {
            EditorGUILayout.LabelField("手動設定", EditorStyles.miniBoldLabel);

            var newTargetFacePath = EditorGUILayout.TextField(
                new GUIContent("対象フェイスパス", "BlendShapeを持つGameObjectへのパス (例: 'Face')"),
                _targetFacePath
            );

            if (newTargetFacePath != _targetFacePath)
            {
                _targetFacePath = newTargetFacePath;
                SaveManualSettings();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("BlendShapeマッピング", EditorStyles.miniBoldLabel);

            if (_availableBlendShapeNames.Length > 0)
            {
                // Nを除外した母音のみ表示
                var displayVowels = ((LipShape[])System.Enum.GetValues(typeof(LipShape)))
                    .Where(vowel => vowel != LipShape.N)
                    .ToArray();

                foreach (LipShape vowel in displayVowels)
                {
                    if (!_manualBlendShapeIndices.ContainsKey(vowel))
                    {
                        _manualBlendShapeIndices[vowel] = 0;
                    }

                    int oldIndex = _manualBlendShapeIndices[vowel];
                    int newIndex = EditorGUILayout.Popup(
                        vowel.ToString(),
                        oldIndex,
                        _availableBlendShapeNames
                    );

                    if (newIndex != oldIndex)
                    {
                        _manualBlendShapeIndices[vowel] = newIndex;
                        UpdateManualMapping();
                        SaveManualSettings();
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("BlendShapeが見つかりません。", MessageType.Warning);
            }
        }

        public void PrepareAvailableBlendShapes(List<AvatarBlendShapeDetector.RendererInfo> detailedInfo)
        {
            var allBlendShapes = new HashSet<string> { "なし" }; // "なし"を最初に追加

            foreach (var info in detailedInfo)
            {
                foreach (string blendShapeName in info.BlendShapeNames)
                {
                    allBlendShapes.Add(blendShapeName);
                }
            }

            _availableBlendShapeNames = allBlendShapes.ToArray();
        }

        public void ClearAvailableBlendShapes()
        {
            _availableBlendShapeNames = new string[0];
            _vowelToBlendShape.Clear();
            _manualBlendShapeIndices.Clear();
        }

        public void InitializeManualSelection()
        {
            _manualBlendShapeIndices.Clear();
            // Nを除外した母音のみ初期化
            var displayVowels = ((LipShape[])System.Enum.GetValues(typeof(LipShape)))
                .Where(vowel => vowel != LipShape.N);
            
            foreach (LipShape vowel in displayVowels)
            {
                _manualBlendShapeIndices[vowel] = 0; // 0 = "なし"
            }
            UpdateManualMapping();
        }

        /// <summary>
        /// 検出結果をデフォルト選択として設定
        /// </summary>
        public void SetDetectionResults(Dictionary<LipShape, string> detectedBlendShapes, string targetFacePath = null)
        {
            if (detectedBlendShapes == null || _availableBlendShapeNames.Length == 0)
            {
                InitializeManualSelection();
                return;
            }

            // 対象フェイスパスが指定されている場合は設定
            if (!string.IsNullOrEmpty(targetFacePath))
            {
                _targetFacePath = targetFacePath;
            }

            _manualBlendShapeIndices.Clear();
            
            // Nを除外した母音のみ処理
            var displayVowels = ((LipShape[])System.Enum.GetValues(typeof(LipShape)))
                .Where(vowel => vowel != LipShape.N);
            
            foreach (LipShape vowel in displayVowels)
            {
                int selectedIndex = 0; // デフォルトは "なし"
                
                if (detectedBlendShapes.ContainsKey(vowel))
                {
                    string detectedBlendShape = detectedBlendShapes[vowel];
                    
                    // blendShape.プレフィックスを除去して検索
                    string searchName = detectedBlendShape.StartsWith("blendShape.") 
                        ? detectedBlendShape.Substring("blendShape.".Length) 
                        : detectedBlendShape;
                    
                    // 利用可能なBlendShape名から該当するものを探す
                    for (int i = 0; i < _availableBlendShapeNames.Length; i++)
                    {
                        if (_availableBlendShapeNames[i] == searchName)
                        {
                            selectedIndex = i;
                            break;
                        }
                    }
                }
                
                _manualBlendShapeIndices[vowel] = selectedIndex;
            }
            
            UpdateManualMapping();
        }

        private void UpdateManualMapping()
        {
            _vowelToBlendShape.Clear();

            foreach (var mapping in _manualBlendShapeIndices)
            {
                if (mapping.Value > 0 && mapping.Value < _availableBlendShapeNames.Length)
                {
                    string selectedBlendShape = _availableBlendShapeNames[mapping.Value];
                    _vowelToBlendShape[mapping.Key] = selectedBlendShape;
                }
            }
        }

        private void SaveManualSettings()
        {
            if (_settings != null)
            {
                _settings.SetManualBlendShapeMapping(_vowelToBlendShape);
                _settings.TargetFacePath = _targetFacePath;
                _settings.UseManualBlendShapeSelection = true;
                _settings.SaveSettings();
            }
        }

        private void RestoreManualMapping(Dictionary<LipShape, string> savedMapping)
        {
            if (savedMapping == null) return;

            _vowelToBlendShape.Clear();
            _manualBlendShapeIndices.Clear();

            // 保存されたマッピングを復元
            foreach (var mapping in savedMapping)
            {
                if (!string.IsNullOrEmpty(mapping.Value))
                {
                    _vowelToBlendShape[mapping.Key] = mapping.Value;
                }
            }
        }

        public void RestoreManualMappingWithIndices(Dictionary<LipShape, string> savedMapping)
        {
            if (savedMapping == null || _availableBlendShapeNames.Length == 0) return;

            _vowelToBlendShape.Clear();
            _manualBlendShapeIndices.Clear();

            // 保存されたマッピングを復元し、対応するインデックスを設定
            var displayVowels = ((LipShape[])System.Enum.GetValues(typeof(LipShape)))
                .Where(vowel => vowel != LipShape.N);

            foreach (LipShape vowel in displayVowels)
            {
                int selectedIndex = 0; // デフォルトは "なし"

                if (savedMapping.ContainsKey(vowel) && !string.IsNullOrEmpty(savedMapping[vowel]))
                {
                    string savedBlendShape = savedMapping[vowel];
                    
                    // 利用可能なBlendShape名から該当するものを探す
                    for (int i = 0; i < _availableBlendShapeNames.Length; i++)
                    {
                        if (_availableBlendShapeNames[i] == savedBlendShape)
                        {
                            selectedIndex = i;
                            _vowelToBlendShape[vowel] = savedBlendShape;
                            break;
                        }
                    }
                }

                _manualBlendShapeIndices[vowel] = selectedIndex;
            }
        }

        public bool ValidateInputs()
        {
            if (string.IsNullOrEmpty(_targetFacePath))
            {
                EditorUtility.DisplayDialog("エラー", "アバターモデルから音素BlendShapeを自動検出できませんでした。手動で対象フェイスパスを指定してください。", "OK");
                return false;
            }

            if (_vowelToBlendShape.Count == 0)
            {
                EditorUtility.DisplayDialog("エラー", "BlendShapeマッピングが設定されていません。手動でBlendShapeを選択してください。", "OK");
                return false;
            }

            return true;
        }
    }
}
