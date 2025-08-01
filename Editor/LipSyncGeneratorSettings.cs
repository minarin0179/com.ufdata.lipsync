using System.Collections.Generic;

using UnityEngine;

using UtaformatixData.Models;

namespace UtaformatixData.Editor.LipSync
{
    /// <summary>
    /// リップシンクアニメーション生成ツールの設定を保存するScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "LipSyncGeneratorSettings", menuName = "UtaformatixData/LipSync Generator Settings")]
    public class LipSyncGeneratorSettings : ScriptableObject
    {
        [Header("基本設定")]
        [SerializeField] private string _jsonFilePath = "";
        [SerializeField] private int _selectedTrackIndex = 0;

        [Header("VRMモデル設定")]
        [SerializeField] private GameObject _vrmModel;
        [SerializeField] private string _targetFacePath = "Face";
        [SerializeField] private bool _useManualBlendShapeSelection = false;
        [SerializeField] private string _manualA = "";
        [SerializeField] private string _manualI = "";
        [SerializeField] private string _manualU = "";
        [SerializeField] private string _manualE = "";
        [SerializeField] private string _manualO = "";

        [Header("詳細設定")]
        [SerializeField] private bool _showAdvancedSettings = false;
        [SerializeField] private string _outputPath = "Assets/Animations";
        [SerializeField] private float _maxFadeDuration = 0.3f;
        [SerializeField] private float _fadeTimeRatio = 0.3f;

        // 基本設定
        public string JsonFilePath
        {
            get => _jsonFilePath;
            set => _jsonFilePath = value;
        }

        public int SelectedTrackIndex
        {
            get => _selectedTrackIndex;
            set => _selectedTrackIndex = value;
        }

        public string OutputPath
        {
            get => _outputPath;
            set => _outputPath = value;
        }

        // VRMモデル設定
        public GameObject VrmModel
        {
            get => _vrmModel;
            set => _vrmModel = value;
        }

        public string TargetFacePath
        {
            get => _targetFacePath;
            set => _targetFacePath = value;
        }

        public bool UseManualBlendShapeSelection
        {
            get => _useManualBlendShapeSelection;
            set => _useManualBlendShapeSelection = value;
        }

        public Dictionary<LipShape, string> GetManualBlendShapeMapping()
        {
            return new Dictionary<LipShape, string>
            {
                { LipShape.A, _manualA },
                { LipShape.I, _manualI },
                { LipShape.U, _manualU },
                { LipShape.E, _manualE },
                { LipShape.O, _manualO }
            };
        }

        public void SetManualBlendShapeMapping(Dictionary<LipShape, string> mapping)
        {
            if (mapping == null)
            {
                return;
            }

            _manualA = mapping.ContainsKey(LipShape.A) ? mapping[LipShape.A] : "";
            _manualI = mapping.ContainsKey(LipShape.I) ? mapping[LipShape.I] : "";
            _manualU = mapping.ContainsKey(LipShape.U) ? mapping[LipShape.U] : "";
            _manualE = mapping.ContainsKey(LipShape.E) ? mapping[LipShape.E] : "";
            _manualO = mapping.ContainsKey(LipShape.O) ? mapping[LipShape.O] : "";
        }

        // 詳細設定
        public bool ShowAdvancedSettings
        {
            get => _showAdvancedSettings;
            set => _showAdvancedSettings = value;
        }

        public float MaxFadeDuration
        {
            get => _maxFadeDuration;
            set => _maxFadeDuration = value;
        }

        public float FadeTimeRatio
        {
            get => _fadeTimeRatio;
            set => _fadeTimeRatio = value;
        }

        /// <summary>
        /// 設定を保存
        /// </summary>
        public void SaveSettings()
        {
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// デフォルト設定でインスタンスを作成
        /// </summary>
        public static LipSyncGeneratorSettings CreateDefault()
        {
            var settings = CreateInstance<LipSyncGeneratorSettings>();
            settings._outputPath = "Assets/Animations";
            settings._targetFacePath = "Face";
            settings._maxFadeDuration = 0.3f;
            settings._fadeTimeRatio = 0.3f;
            return settings;
        }
    }
}
