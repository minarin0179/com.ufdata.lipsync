using UnityEngine;
using UnityEditor;
using UtaformatixData.Editor.LipSync;
using System.IO;

namespace UtaformatixData.Editor
{
    /// <summary>
    /// リップシンクアニメーション生成ツールのメインウィンドウ
    /// </summary>
    public class LipSyncAnimationGeneratorWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private BasicSettingsUI _basicSettings;
        private VRMSettingsUI _vrmSettings;
        private AdvancedSettingsUI _advancedSettings;
        private LipSyncAnimationService _animationService;
        private LipSyncGeneratorSettings _settings;

        private const string SETTINGS_PATH = "Assets/Editor/LipSync/LipSyncGeneratorSettings.asset";

        [MenuItem("Tools/LipSync Animation Generator")]
        public static void ShowWindow()
        {
            LipSyncAnimationGeneratorWindow window = GetWindow<LipSyncAnimationGeneratorWindow>("リップシンク生成");
            window.minSize = new Vector2(400, 300);
        }

        private void OnEnable()
        {
            LoadOrCreateSettings();
            
            _basicSettings = new BasicSettingsUI();
            _vrmSettings = new VRMSettingsUI();
            _advancedSettings = new AdvancedSettingsUI();
            _animationService = new LipSyncAnimationService();

            _basicSettings.Initialize(_settings);
            _advancedSettings.Initialize(_settings);
            _vrmSettings.Initialize(_settings);
        }

        private void LoadOrCreateSettings()
        {
            _settings = AssetDatabase.LoadAssetAtPath<LipSyncGeneratorSettings>(SETTINGS_PATH);

            if (_settings == null)
            {
                _settings = LipSyncGeneratorSettings.CreateDefault();

                // ディレクトリが存在しない場合は作成
                var directory = Path.GetDirectoryName(SETTINGS_PATH);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                AssetDatabase.CreateAsset(_settings, SETTINGS_PATH);
                AssetDatabase.SaveAssets();
                Debug.Log($"[LipSyncAnimationGenerator] 設定ファイルを作成しました: {SETTINGS_PATH}");
            }
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            _basicSettings.Draw();
            _vrmSettings.Draw();
            _advancedSettings.Draw();
            DrawGenerateButton();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space();
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("リップシンク アニメーション生成", headerStyle);
            EditorGUILayout.Space();
        }

        private void DrawGenerateButton()
        {
            var canGenerate = _basicSettings.JsonFile != null &&
                              _basicSettings.TrackNames.Length > 0 &&
                              _vrmSettings.VrmModel != null;

            EditorGUI.BeginDisabledGroup(!canGenerate);

            if (GUILayout.Button("リップシンクアニメーションを生成", GUILayout.Height(30)))
            {
                GenerateLipSyncAnimation();
            }

            EditorGUI.EndDisabledGroup();

            // エラーメッセージ表示
            if (_basicSettings.JsonFile == null)
            {
                EditorGUILayout.HelpBox("アニメーションを生成するにはUFDataファイルを選択してください。", MessageType.Warning);
            }
            else if (_basicSettings.TrackNames.Length == 0)
            {
                EditorGUILayout.HelpBox("選択されたUFDataファイルにトラックが見つかりません。", MessageType.Warning);
            }
            else if (_vrmSettings.VrmModel == null)
            {
                EditorGUILayout.HelpBox("アバターモデルを選択してください。", MessageType.Warning);
            }
        }

        public void GenerateLipSyncAnimation()
        {
            if (!ValidateInputs())
            {
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("リップシンク生成", "UFDataを読み込み中...", 0.1f);

                var ufData = _basicSettings.LoadedUFData;
                if (ufData == null)
                {
                    EditorUtility.DisplayDialog("エラー", "選択されたファイルからUFDataの読み込みに失敗しました。", "OK");
                    return;
                }

                var settings = new LipSyncAnimationService.AnimationSettings
                {
                    MaxFadeDuration = _advancedSettings.MaxFadeDuration,
                    FadeTimeRatio = _advancedSettings.FadeTimeRatio,
                    OutputPath = _basicSettings.OutputPath,
                    TargetFacePath = _vrmSettings.TargetFacePath,
                    VowelToBlendShape = _vrmSettings.VowelToBlendShape
                };

                var modelName = _vrmSettings.VrmModel.name;
                _animationService.GenerateAnimation(
                    ufData,
                    _basicSettings.SelectedTrackIndex,
                    _basicSettings.TrackNames,
                    _basicSettings.JsonFile.name,
                    modelName,
                    settings
                );
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LipSyncAnimationGenerator] Error generating animation: {e.Message}");
                EditorUtility.DisplayDialog("エラー", $"アニメーションの生成に失敗しました:\n{e.Message}", "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private bool ValidateInputs()
        {
            if (_basicSettings.JsonFile == null)
            {
                EditorUtility.DisplayDialog("エラー", "UFDataファイルを選択してください。", "OK");
                return false;
            }

            return _vrmSettings.ValidateInputs();
        }
    }
}
