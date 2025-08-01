using UnityEngine;
using UnityEditor;
using UnityEditor.AssetImporters;
using System.IO;

[ScriptedImporter(1, "ufdata")]
public class UFDataImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        // ファイルの内容を読み取り
        var fileContent = File.ReadAllText(ctx.assetPath);

        // TextAssetとして作成
        var textAsset = new TextAsset(fileContent);

        // アセットとして登録
        ctx.AddObjectToAsset("main obj", textAsset);
        ctx.SetMainObject(textAsset);
    }
}
