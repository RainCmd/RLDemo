using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ConfigMapBlocks))]
public class ConfigMapBlocksEditor : Editor
{
    private int width, height;
    private void OnEnable()
    {
        var blocks = target as ConfigMapBlocks;
        width = blocks.width;
        height = blocks.height;
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
        for (var x = sx; x < ConfigMapBlockInfo.width - 1; x++)
            for (var y = sy; y < ConfigMapBlockInfo.height - 1; y++)
            {
                if (x > 0)
                {
                    if (y > 0)
                    {
                        var splat00 = block.GetSplat(x - 1, y - 1);
                        var splat01 = block.GetSplat(x - 1, y);
                        var splat10 = block.GetSplat(x, y - 1);
                        int splat11;
                        var cohesion01 = splates[splat01].cohesion;
                        var cohesion10 = splates[splat10].cohesion;
                        if (Random.value * 2 < cohesion01 + cohesion10)
                        {
                            if (Random.value * (cohesion01 + cohesion10) < cohesion01) splat11 = splat01;
                            else splat11 = splat10;
                        }
                        else splat11 = GetRandomSplate(splates, weight);
                        block.SetSplat(x, y, splat11);
                        if (splates[splat11].extend && splat00 == splat11 && splat01 == splat11 && splat10 == splat11)
                            block.SetExtend(x - 1, y - 1, Random.Range(0, 16));
                    }
                    else
                    {
                        var splat = block.GetSplat(x - 1, y);
                        if (Random.value < splates[splat].cohesion) splat = GetRandomSplate(splates, weight);
                        block.SetSplat(x, y, splat);
                    }
                }
                else if (y > 0)
                {
                    var splat = block.GetSplat(x, y - 1);
                    if (Random.value < splates[splat].cohesion) splat = GetRandomSplate(splates, weight);
                    block.SetSplat(x, y, splat);
                }
                else block.SetSplat(x, y, GetRandomSplate(splates, weight));
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
            serializedObject.ApplyModifiedProperties();
            var blocks = target as ConfigMapBlocks;
            blocks.width = width;
            blocks.height = height;
            blocks.blocks = new List<ConfigMapBlockInfo>();

            var splates = Config.SplatInfos;
            var weight = 0;
            foreach (var s in splates) weight += s.weight;

            for (var x = 0; x < width; x++)
                for (var y = 0; y < height; y++)
                {
                    var info = ConfigMapBlockInfo.Create();

                    var sx = 0; var sy = 0;
                    if (x > 0)
                    {
                        var prev = blocks[x - 1, y];
                        for (var i = 0; i < ConfigMapBlockInfo.height; i++)
                            info[0, i] = prev[ConfigMapBlockInfo.width - 1, i];
                        sx++;
                    }
                    if (y > 0)
                    {
                        var prev = blocks[x, y - 1];
                        for (var i = 0; i < ConfigMapBlockInfo.width; i++)
                            info[i, 0] = prev[i, ConfigMapBlockInfo.height - 1];
                        sy++;
                    }
                    GenBlock(ref info, x, y, splates, weight);

                    blocks.blocks.Add(info);
                }
            serializedObject.Update();
        }
    }
    [MenuItem("配置文件/地图")]
    private static void CreateConfigMapBlocks()
    {
        ConfigEditor.Create<ConfigMapBlocks>(ConfigMapBlocks.path);
    }
}
