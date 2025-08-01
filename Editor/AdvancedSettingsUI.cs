using UnityEngine;
using UnityEditor;

namespace UtaformatixData.Editor.LipSync
{
    /// <summary>
    /// 詳細設定（フェード時間設定）のUI表示を担当
    /// </summary>
    public class AdvancedSettingsUI
    {
        private LipSyncGeneratorSettings _settings;

        public float MaxFadeDuration => _settings?.MaxFadeDuration ?? 0.3f;
        public float FadeTimeRatio => _settings?.FadeTimeRatio ?? 0.3f;

        public void Initialize(LipSyncGeneratorSettings settings)
        {
            _settings = settings;
        }

        public void Draw()
        {
            if (_settings == null)
            {
                return;
            }

            var newShowAdvancedSettings = EditorGUILayout.Foldout(_settings.ShowAdvancedSettings, "詳細設定", EditorStyles.foldoutHeader);

            if (newShowAdvancedSettings != _settings.ShowAdvancedSettings)
            {
                _settings.ShowAdvancedSettings = newShowAdvancedSettings;
                _settings.SaveSettings();
            }

            if (_settings.ShowAdvancedSettings)
            {
                EditorGUI.indentLevel++;

                var newOutputPath = EditorGUILayout.TextField(
                    new GUIContent("出力パス", "アニメーションファイルが保存されるディレクトリ"),
                    _settings.OutputPath
                );

                if (newOutputPath != _settings.OutputPath)
                {
                    _settings.OutputPath = newOutputPath;
                    _settings.SaveSettings();
                }

                EditorGUILayout.Space();

                var newMaxFadeDuration = EditorGUILayout.Slider(
                    new GUIContent("最大フェード時間", "最大フェード時間（秒）"),
                    _settings.MaxFadeDuration,
                    0.1f,
                    1.0f
                );

                if (newMaxFadeDuration != _settings.MaxFadeDuration)
                {
                    _settings.MaxFadeDuration = newMaxFadeDuration;
                    _settings.SaveSettings();
                }

                var newFadeTimeRatio = EditorGUILayout.Slider(
                    new GUIContent("フェード時間比率", "歌詞継続時間に対するフェード時間の比率"),
                    _settings.FadeTimeRatio,
                    0.1f,
                    0.5f
                );

                if (newFadeTimeRatio != _settings.FadeTimeRatio)
                {
                    _settings.FadeTimeRatio = newFadeTimeRatio;
                    _settings.SaveSettings();
                }

                EditorGUILayout.Space();

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
        }
    }
}
