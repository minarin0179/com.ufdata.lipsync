using System.Linq;
using UnityEngine;
using UnityEditor;
using UtaformatixData.Models;
using UtaformatixData.Services;

namespace UtaformatixData.Editor.LipSync
{
    /// <summary>
    /// 基本設定（UFDataファイル、トラック選択、出力パス）のUI表示を担当
    /// </summary>
    public class BasicSettingsUI
    {
        private TextAsset _jsonFile;
        private UFData _loadedUFData;
        private string[] _trackNames = new string[0];
        private LipSyncGeneratorSettings _settings;

        public TextAsset JsonFile => _jsonFile;
        public string[] TrackNames => _trackNames;
        public int SelectedTrackIndex => _settings?.SelectedTrackIndex ?? 0;
        public string OutputPath => _settings?.OutputPath ?? "Assets/Animations";
        public UFData LoadedUFData => _loadedUFData;

        public void Initialize(LipSyncGeneratorSettings settings)
        {
            _settings = settings;

            // 保存された設定からJSONファイルを復元
            if (!string.IsNullOrEmpty(_settings.JsonFilePath))
            {
                _jsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>(_settings.JsonFilePath);
                if (_jsonFile != null)
                {
                    LoadTracksFromUFData();
                }
            }
        }

        public void Draw()
        {
            if (_settings == null)
            {
                return;
            }

            EditorGUILayout.LabelField("基本設定", EditorStyles.boldLabel);

            var newJsonFile = (TextAsset)EditorGUILayout.ObjectField(
                new GUIContent("UFDataファイル", "歌詞タイミングデータを含む.ufdataファイルを選択"),
                _jsonFile,
                typeof(TextAsset),
                false
            );

            if (newJsonFile != _jsonFile)
            {
                _jsonFile = newJsonFile;
                _settings.JsonFilePath = _jsonFile != null ? AssetDatabase.GetAssetPath(_jsonFile) : "";
                _settings.SaveSettings();
                LoadTracksFromUFData();
            }

            DrawTrackSelection();

            EditorGUILayout.Space();
        }

        private void DrawTrackSelection()
        {
            if (_settings == null)
            {
                return;
            }

            if (_trackNames.Length > 0)
            {
                EditorGUILayout.Space();

                var oldSelectedIndex = _settings.SelectedTrackIndex;
                var newSelectedIndex = EditorGUILayout.Popup(
                    new GUIContent("対象トラック", "アニメーション生成に使用するトラックを選択"),
                    _settings.SelectedTrackIndex,
                    _trackNames
                );

                if (oldSelectedIndex != newSelectedIndex)
                {
                    _settings.SelectedTrackIndex = newSelectedIndex;
                    _settings.SaveSettings();
                }
            }
            else if (_jsonFile != null)
            {
                EditorGUILayout.HelpBox("UFDataファイルからトラック情報を読み込み中...", MessageType.Info);
            }
        }

        private void LoadTracksFromUFData()
        {
            if (_jsonFile == null)
            {
                _trackNames = new string[0];
                if (_settings != null)
                {
                    _settings.SelectedTrackIndex = 0;
                    _settings.SaveSettings();
                }
                _loadedUFData = null;
                return;
            }

            try
            {
                _loadedUFData = UFDataLoader.LoadFromTextAsset(_jsonFile);
                if (_loadedUFData?.Project?.Tracks != null)
                {
                    _trackNames = _loadedUFData.Project.Tracks
                        .Select((track, index) => $"{index}: {(string.IsNullOrEmpty(track.Name) ? "Untitled" : track.Name)}")
                        .ToArray();
                    
                    // 保存されたトラック選択を維持。範囲外の場合のみ0にリセット
                    if (_settings != null && _settings.SelectedTrackIndex >= _trackNames.Length)
                    {
                        _settings.SelectedTrackIndex = 0;
                        _settings.SaveSettings();
                    }
                }
                else
                {
                    _trackNames = new string[0];
                    if (_settings != null)
                    {
                        _settings.SelectedTrackIndex = 0;
                        _settings.SaveSettings();
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LipSyncAnimationGenerator] UFData読み込みエラー: {e.Message}");
                _trackNames = new string[0];
                if (_settings != null)
                {
                    _settings.SelectedTrackIndex = 0;
                    _settings.SaveSettings();
                }
                _loadedUFData = null;
            }
        }
    }
}
