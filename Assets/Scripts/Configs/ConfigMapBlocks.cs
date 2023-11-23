using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ConfigMapBlockInfo
{
    public int[] splates;
    public int this[int x, int y]
    {
        get
        {
            return splates[x * width + y];
        }
        set
        {
            splates[x * width + y] = value;
        }
    }
    public int GetSplat(int x, int y)
    {
        return this[x, y] & 0xffff;
    }
    public int GetExtend(int x, int y)
    {
        return this[x + 1, y + 1] >> 16;
    }
    public void SetSplat(int x, int y, int splat)
    {
        var value = this[x, y];
        value &= ~0xffff;
        value |= splat;
        this[x, y] = value;
    }
    public void SetExtend(int x, int y, int extend)
    {
        var value = this[x + 1, y + 1];
        value &= 0xffff;
        value |= extend << 16;
        this[x + 1, y + 1] = value;
    }
    private ConfigMapBlockInfo(int[] splates)
    {
        this.splates = splates;
    }
    public static ConfigMapBlockInfo Create()
    {
        return new ConfigMapBlockInfo(new int[width * height]);
    }
    public const int width = 9, height = 9;
}
public class ConfigMapBlocks : ScriptableObject
{
    public List<ConfigMapBlockInfo> blocks;
    public int width, height;
    public ConfigMapBlockInfo this[int x, int y]
    {
        get { return blocks[x * width + y]; }
        set { blocks[x * width + y] = value; }
    }
    public const string path = "Configs/MapBlocks";
}
