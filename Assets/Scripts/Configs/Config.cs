using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConfigInfos<T> : ScriptableObject, IReadOnlyList<T>
{
    [SerializeField]
    private List<T> _list = new List<T>();
    public T this[int index] => _list[index];
    public int Count => _list.Count;
    public IEnumerator<T> GetEnumerator()
    {
        return _list.GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return _list.GetEnumerator();
    }
}
public static class Config
{
    public const string GameName = "RLDemo";
    private struct Icons
    {
        private readonly string path;
        private ConfigIcons icons;
        public Icons(string path)
        {
            this.path = path;
            icons = null;
        }
        public IReadOnlyList<Sprite> IconList
        {
            get
            {
                if (!icons) icons = LoadConfig<ConfigIcons>(path);
                return icons;
            }
        }
    }
    public const int HallPort = 14567;
    public const int LFPS = 30;//逻辑帧帧率
    public const int WandSlotSize = 16;//修改的时候需要同步修改 Scripts\Logic\RainScripts\ConstantValues.rain 中的 WAND_SLOT_SIZE
    private static Icons palyerHeadIcons = new Icons(ConfigIcons.HeadIconsPath);
    public static IReadOnlyList<Sprite> PlayerHeadIconList
    {
        get
        {
            return palyerHeadIcons.IconList;
        }
    }
    private static Icons nodeIcons = new Icons(ConfigIcons.NodeIconsPath);
    public static IReadOnlyList<Sprite> NodeIconList
    {
        get
        {
            return nodeIcons.IconList;
        }
    }
    private static Icons magicNodeTypeIcons = new Icons(ConfigIcons.MagicNodeTypeIconsPath);
    public static IReadOnlyList<Sprite> MagicNodeTypeIcons
    {
        get
        {
            return magicNodeTypeIcons.IconList;
        }
    }
    private static Icons buffIcons = new Icons(ConfigIcons.BuffIconsPath);
    public static IReadOnlyList<Sprite> BuffIcons
    {
        get
        {
            return buffIcons.IconList;
        }
    }

    private static ConfigSplat splatInfos;
    public static IReadOnlyList<ConfigSplatInfo> SplatInfos
    {
        get
        {
            if (!splatInfos) splatInfos = LoadConfig<ConfigSplat>(ConfigSplat.path);
            return splatInfos;
        }
    }

    private static ConfigMapBlocks mapBlocks;
    public static ConfigMapBlocks MapBlocks
    {
        get
        {
            if (!mapBlocks)
            {
                mapBlocks = LoadConfig<ConfigMapBlocks>(ConfigMapBlocks.path);
                var assert = LoadConfig<TextAsset>(ConfigMapBlocks.path + ConfigMapBlocks.path_data);
                mapBlocks.Load(assert.bytes);
            }
            return mapBlocks;
        }
    }

    public static int IndexOf<T>(this IReadOnlyList<T> list, T value)
    {
        if (value == null)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i] == null) return i;
        }
        else
            for (int i = 0; i < list.Count; i++)
                if (value.Equals(list[i])) return i;
        return -1;
    }
    private static T LoadConfig<T>(string path) where T : Object
    {
        return Resources.Load<T>(path);
    }
}
