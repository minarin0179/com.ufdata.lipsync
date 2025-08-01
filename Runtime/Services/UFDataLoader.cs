using Newtonsoft.Json;
using UnityEngine;
using UtaformatixData.Models;

namespace UtaformatixData.Services
{
    /// <summary>
    /// UFDataローダーユーティリティクラス
    /// </summary>
    public static class UFDataLoader
    {
        /// <summary>
        /// UnityのTextAssetからUFDataを読み込みます。エディタでインポートしたUFDataファイルを指定してください。
        /// </summary>
        /// <param name="jsonFile">UFDataファイルのTextAsset</param>
        /// <returns>パースされたUFDataオブジェクト、失敗時はnull</returns>
        public static UFData LoadFromTextAsset(TextAsset jsonFile)
        {
            if (jsonFile == null)
            {
                Debug.LogError("[UFDataLoader] JSONファイルが指定されていません。");
                return null;
            }

            return LoadFromString(jsonFile.text);
        }

        /// <summary>
        /// UFData形式の文字列からUFDataオブジェクトを作成します。内部でデータ検証も実行されます。
        /// </summary>
        /// <param name="jsonText">UFData形式の文字列</param>
        /// <returns>パースされたUFDataオブジェクト、失敗時はnull</returns>
        public static UFData LoadFromString(string jsonText)
        {
            if (string.IsNullOrEmpty(jsonText))
            {
                Debug.LogError("[UFDataLoader] JSONテキストがnullまたは空です。");
                return null;
            }

            try
            {
                UFData ufData = JsonConvert.DeserializeObject<UFData>(jsonText);
                if (ValidateUFData(ufData))
                {
                    return ufData;
                }
                return null;
            }
            catch (JsonException ex)
            {
                Debug.LogError($"[UFDataLoader] JSONのデシリアライズに失敗しました: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// ファイルシステムから直接UFDataファイルを読み込みます。Unityエディタでのみ使用可能です。
        /// </summary>
        /// <param name="filePath">UFDataファイルの絶対パスまたは相対パス</param>
        /// <returns>パースされたUFDataオブジェクト、失敗時はnull</returns>
        public static UFData LoadFromFile(string filePath)
        {
#if UNITY_EDITOR
            if (!System.IO.File.Exists(filePath))
            {
                Debug.LogError($"[UFDataLoader] ファイルが見つかりません: {filePath}");
                return null;
            }

            var jsonText = System.IO.File.ReadAllText(filePath);
            return LoadFromString(jsonText);
#else
            Debug.LogWarning("[UFDataLoader] LoadFromFileはUnityエディタでのみ使用できます。");
            return null;
#endif
        }

        /// <summary>
        /// UFDataオブジェクトをUFData形式の文字列に変換します。デバッグやファイル出力に使用できます。
        /// </summary>
        /// <param name="ufData">変換するUFDataオブジェクト</param>
        /// <param name="formatting">出力フォーマット（デフォルト：読みやすいインデント付き）</param>
        /// <returns>UFData形式の文字列、失敗時はnull</returns>
        public static string SerializeToString(UFData ufData, Formatting formatting = Formatting.Indented)
        {
            if (ufData == null)
            {
                Debug.LogError("[UFDataLoader] UFDataがnullです。");
                return null;
            }

            try
            {
                return JsonConvert.SerializeObject(ufData, formatting);
            }
            catch (JsonException ex)
            {
                Debug.LogError($"[UFDataLoader] UFDataのシリアライズに失敗しました: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// UFDataオブジェクトの基本的な整合性をチェックします。必要なフィールドの存在やテンポ情報の有無を検証します。
        /// </summary>
        /// <param name="ufData">検証対象のUFDataオブジェクト</param>
        /// <returns>有効なUFDataの場合true、無効な場合false</returns>
        private static bool ValidateUFData(UFData ufData)
        {
            if (ufData == null)
            {
                Debug.LogError("[UFDataLoader] デシリアライズ後にUFDataがnullです。");
                return false;
            }

            if (ufData.Project == null)
            {
                Debug.LogError("[UFDataLoader] UFData内のProjectがnullです。");
                return false;
            }

            if (ufData.Project.Tracks == null || ufData.Project.Tracks.Count == 0)
            {
                Debug.LogWarning("[UFDataLoader] プロジェクト内にトラックが見つかりません。");
                return true; // Warning only, still valid
            }

            if (ufData.Project.Tempos == null || ufData.Project.Tempos.Count == 0)
            {
                Debug.LogError("[UFDataLoader] プロジェクト内にテンポ情報が見つかりません。");
                return false;
            }

            return true;
        }
    }
}
