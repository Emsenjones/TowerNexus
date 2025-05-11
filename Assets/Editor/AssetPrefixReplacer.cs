#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

public class AssetPrefixReplacer : EditorWindow
{
    private string _oldPrefix = "3_enemies_1_attack";
    private string _newPrefix = "Monster01_Attack";

    [MenuItem("Tools/Batch replace the prefix of resources")]
    public static void ShowWindow()
    {
        GetWindow<AssetPrefixReplacer>("Batch replace the prefix of resources.");
    }

    private void OnGUI()
    {
        GUILayout.Label("Batch replace the prefix of resources", EditorStyles.boldLabel);
        _oldPrefix = EditorGUILayout.TextField("Old prefix", _oldPrefix);
        _newPrefix = EditorGUILayout.TextField("New prefix", _newPrefix);

        if (GUILayout.Button("Start relace"))
        {
            ReplacePrefixes();
        }
    }

    private void ReplacePrefixes()
    {
        Object[] selectedAssets = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);

        int renamedCount = 0;

        foreach (var asset in selectedAssets)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            string filename = System.IO.Path.GetFileNameWithoutExtension(path);
            string extension = System.IO.Path.GetExtension(path);

            if (filename.StartsWith(_oldPrefix))
            {
                string suffix = filename.Substring(_oldPrefix.Length); // 提取编号部分
                string newName = _newPrefix + suffix;

                AssetDatabase.RenameAsset(path, newName);
                renamedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Replacement complete，it has replaced {renamedCount} resources。");
    }
}
#endif
