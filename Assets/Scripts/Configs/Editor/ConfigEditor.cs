using UnityEditor;
using UnityEngine;

public class ConfigEditor
{
    public static void Create<T>(string path) where T : ScriptableObject
    {
        path = string.Format("Assets/Resources/{0}.asset", path);
        var target = AssetDatabase.LoadAllAssetsAtPath(path);
        if (target.Length > 0)
        {
            EditorGUIUtility.PingObject(target[0]);
        }
        else
        {
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(asset);
        }
    }
}
