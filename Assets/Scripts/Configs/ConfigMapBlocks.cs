using System;
using UnityEngine;

[Serializable]
public struct ConfigMapBlockSplates
{
    [SerializeField]
    private int[] splates;
    public int this[int x, int y]
    {
        get
        {
            return splates[x * ConfigMapBlockInfo.height + y];
        }
        set
        {
            splates[x * ConfigMapBlockInfo.height + y] = value;
        }
    }
    public static ConfigMapBlockSplates Create()
    {
        return new ConfigMapBlockSplates() { splates = new int[ConfigMapBlockInfo.width * ConfigMapBlockInfo.height] };
    }
}
[Serializable]
public struct ConfigMapBlockExtebds
{
    [SerializeField]
    private byte[] extends;
    public byte this[int x, int y]
    {
        get
        {
            return extends[x * (ConfigMapBlockInfo.height - 1) + y];
        }
        set
        {
            extends[x * (ConfigMapBlockInfo.height - 1) + y] = value;
        }
    }
    public static ConfigMapBlockExtebds Create()
    {
        return new ConfigMapBlockExtebds() { extends = new byte[(ConfigMapBlockInfo.width - 1) * (ConfigMapBlockInfo.height - 1)] };
    }
}

[Serializable]
public struct ConfigMapBlockInfo
{
    [SerializeField]
    public ConfigMapBlockSplates splates;
    [SerializeField]
    public ConfigMapBlockExtebds extends;
    public static ConfigMapBlockInfo Create()
    {
        return new ConfigMapBlockInfo() { splates = ConfigMapBlockSplates.Create(), extends = ConfigMapBlockExtebds.Create() };
    }
    public const int width = 9, height = 9;
}
public class ConfigMapBlocks : ScriptableObject
{
    [SerializeField]
    private ConfigMapBlockInfo[] blocks;
    [SerializeField]
    public int width, height;
    public ConfigMapBlockInfo this[int x, int y]
    {
        get { return blocks[x * width + y]; }
        set { blocks[x * width + y] = value; }
    }
    public void Resize(int width, int height)
    {
        this.width = width;
        this.height = height;
        blocks = new ConfigMapBlockInfo[width * height];
    }
    public const string path = "Configs/MapBlocks";
}
