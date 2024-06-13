using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ConfigMapBlocks))]
public class ConfigMapBlocksEditor : Editor
{
    private class Writer
    {
        public readonly List<byte> data = new List<byte>();
        public void Write(byte value)
        {
            data.Add(value);
        }
        public void Write(int value)
        {
            data.AddRange(System.BitConverter.GetBytes(value));
        }
    }
    private int width, height;
    private void OnEnable()
    {
        var blocks = target as ConfigMapBlocks;
        var path = AssetDatabase.GetAssetPath(target);
        if (!string.IsNullOrEmpty(path) && blocks)
        {
            var data = AssetDatabase.LoadAssetAtPath<TextAsset>(path.Replace(".asset", ConfigMapBlocks.path_data + ".bytes"));
            if (data) blocks.Load(data.bytes);
            else blocks.Resize(32, 32);
        }
        else blocks.Resize(32, 32);
        width = blocks.Width;
        height = blocks.Height;
    }
    private void OnDisable()
    {
        var blocks = target as ConfigMapBlocks;
        var path = AssetDatabase.GetAssetPath(target);
        if (!string.IsNullOrEmpty(path) && blocks)
        {
            var writer = new Writer();
            writer.Write(blocks.Width);
            writer.Write(blocks.Height);
            for (var w = 0; w < blocks.Width; w++)
                for (var h = 0; h < blocks.Height; h++)
                {
                    for (var x = 0; x < ConfigMapBlockInfo.width; x++)
                        for (var y = 0; y < ConfigMapBlockInfo.height; y++)
                            writer.Write(blocks[w, h].splates[x, y]);
                    for (var x = 0; x < ConfigMapBlockInfo.width - 1; x++)
                        for (var y = 0; y < ConfigMapBlockInfo.height - 1; y++)
                            writer.Write(blocks[w, h].extends[x, y]);
                }
            path = path.Replace(".asset", ConfigMapBlocks.path_data + ".bytes");
            path = Application.dataPath.Replace("Assets", path);
            File.WriteAllBytes(path, writer.data.ToArray());
            AssetDatabase.Refresh();
        }
    }
    private static int GetRandomSplate(IReadOnlyList<ConfigSplatInfo> splates, int weight)
    {
        var value = Random.value * weight;
        for (int i = 0; i < splates.Count; i++)
            if (value < splates[i].weight) return i;
            else value -= splates[i].weight;
        return 0;
    }
    private static void GenBlock(ref ConfigMapBlockInfo block, int sx, int sy, IReadOnlyList<ConfigSplatInfo> splates, int weight)
    {
        for (var x = 0; x < ConfigMapBlockInfo.width; x++)
            for (var y = 0; y < ConfigMapBlockInfo.height; y++)
            {
                if (x > 0)
                {
                    if (y > 0)
                    {
                        var splat00 = block.splates[x - 1, y - 1];
                        var splat01 = block.splates[x - 1, y];
                        var splat10 = block.splates[x, y - 1];
                        int splat11;
                        var cohesion01 = splates[splat01].cohesion;
                        var cohesion10 = splates[splat10].cohesion;
                        if (Random.value * 2 < cohesion01 + cohesion10)
                        {
                            if (Random.value * (cohesion01 + cohesion10) < cohesion01) splat11 = splat01;
                            else splat11 = splat10;
                        }
                        else splat11 = GetRandomSplate(splates, weight);
                        block.splates[x, y] = (byte)splat11;
                        if (splates[splat11].extend && splat00 == splat11 && splat01 == splat11 && splat10 == splat11)
                            block.extends[x - 1, y - 1] = (byte)Random.Range(0, 16);
                    }
                    else if (sy == 0)
                    {
                        var splat = block.splates[x - 1, y];
                        if (Random.value < splates[splat].cohesion) splat = (byte)GetRandomSplate(splates, weight);
                        block.splates[x, y] = splat;
                    }
                }
                else if (sx == 0)
                {
                    if (y > 0)
                    {
                        var splat = block.splates[x, y - 1];
                        if (Random.value < splates[splat].cohesion) splat = (byte)GetRandomSplate(splates, weight);
                        block.splates[x, y] = splat;
                    }
                    else if (sy == 0) block.splates[x, y] = (byte)GetRandomSplate(splates, weight);
                }
            }
    }
    public override void OnInspectorGUI()
    {
        EditorGUILayout.BeginHorizontal();
        width = EditorGUILayout.IntField("width", width);
        height = EditorGUILayout.IntField("height", height);
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("重新生成"))
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            serializedObject.ApplyModifiedProperties();
            var blocks = target as ConfigMapBlocks;
            blocks.Resize(width, height);

            var splates = Config.SplatInfos;
            var weight = 0;
            foreach (var s in splates) weight += s.weight;

            for (var x = 0; x < width; x++)
                for (var y = 0; y < height; y++)
                {
                    var info = ConfigMapBlockInfo.Create();

                    if (x > 0)
                    {
                        var prev = blocks[x - 1, y];
                        for (var i = 0; i < ConfigMapBlockInfo.height; i++)
                            info.splates[0, i] = prev.splates[ConfigMapBlockInfo.width - 1, i];
                        for (var i = 0; i < ConfigMapBlockInfo.height - 1; i++)
                            info.extends[0, i] = prev.extends[ConfigMapBlockInfo.width - 2, i];
                    }
                    if (y > 0)
                    {
                        var prev = blocks[x, y - 1];
                        for (var i = 0; i < ConfigMapBlockInfo.width; i++)
                            info.splates[i, 0] = prev.splates[i, ConfigMapBlockInfo.height - 1];
                        for (var i = 0; i < ConfigMapBlockInfo.width - 1; i++)
                            info.extends[i, 0] = prev.extends[i, ConfigMapBlockInfo.height - 2];
                    }
                    GenBlock(ref info, x, y, splates, weight);

                    blocks[x, y] = info;
                }
            serializedObject.Update();
            sw.Stop();
            Debug.LogFormat("<color=#00ff00>地图生成完成！</color> 耗时<color=#ffcc00>{0}</color>ms", sw.ElapsedMilliseconds);
        }
    }
    [MenuItem("配置文件/地图")]
    private static void CreateConfigMapBlocks()
    {
        ConfigEditor.Create<ConfigMapBlocks>(ConfigMapBlocks.path);
    }
}
