using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;

public static class LogicConfig
{
    private static bool loaded = false;
    public static ConfigMagicNode[] magicNodes;
    public static ConfigEntity[] entities;
    public static ConfigUnit[] units;
    public static void LoadConfigs()
    {
        if (loaded) return;
        loaded = true;
        magicNodes = Load<ConfigMagicNode>(ConfigMagicNode.Path).ToArray();
        entities = Load<ConfigEntity>(ConfigEntity.Path).ToArray();
        units = Load<ConfigUnit>(ConfigUnit.Path).ToArray();
    }
    public static IList Load(string path)
    {
        path = Application.streamingAssetsPath + path;
        if (File.Exists(path))
        {
            using (var stream = File.OpenRead(path))
            {
                var bf = new BinaryFormatter();
                return bf.Deserialize(stream) as IList;
            }
        }
        return null;
    }
    public static List<T> Load<T>(string path)
    {
        return Load(path) as List<T>;
    }
#if UNITY_EDITOR
    public static void Save(string path, IList cfgs)
    {
        path = Application.streamingAssetsPath + path;
        using (var stream = new FileStream(path, FileMode.Create))
        {
            var bf = new BinaryFormatter();
            bf.Serialize(stream, cfgs);
        }
    }
#endif
}
