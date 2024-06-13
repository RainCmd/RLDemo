using System;
using UnityEngine;

public struct ConfigMapBlockInfo
{
    public readonly byte[,] splates;
    public readonly byte[,] extends;
    private ConfigMapBlockInfo(byte[,] splates, byte[,] extends)
    {
        this.splates = splates;
        this.extends = extends;
    }
    public static ConfigMapBlockInfo Create()
    {
        return new ConfigMapBlockInfo(new byte[width, height], new byte[width - 1, height - 1]);
    }
    public const int width = 9, height = 9;
}
public class ConfigMapBlocks : ScriptableObject
{
    private struct Reader
    {
        private readonly byte[] bytes;
        private int index;
        public Reader(byte[] bytes)
        {
            this.bytes = bytes;
            index = 0;
        }
        public byte ReadByte()
        {
            return bytes[index++];
        }
        public int ReadInt()
        {
            var result = BitConverter.ToInt32(bytes, index);
            index += 4;
            return result;
        }
    }
    public ConfigMapBlockInfo[,] blocks;
    public int Width
    {
        get
        {
            return blocks.GetLength(0);
        }
    }
    public int Height
    {
        get
        {
            return blocks.GetLength(1);
        }
    }
    public ConfigMapBlockInfo this[int x, int y]
    {
        get { return blocks[x, y]; }
        set { blocks[x, y] = value; }
    }
    public void Resize(int width, int height)
    {
        blocks = new ConfigMapBlockInfo[width, height];
        for (var w = 0; w < width; w++)
            for (var h = 0; h < height; h++)
                blocks[w, h] = ConfigMapBlockInfo.Create();
    }
    public void Load(byte[] data)
    {
        var reader = new Reader(data);
        var width = reader.ReadInt();
        var height = reader.ReadInt();
        Resize(width, height);
        for (var w = 0; w < width; w++)
            for (var h = 0; h < height; h++)
            {
                for (var x = 0; x < ConfigMapBlockInfo.width; x++)
                    for (var y = 0; y < ConfigMapBlockInfo.height; y++)
                        blocks[w, h].splates[x, y] = reader.ReadByte();
                for (var x = 0; x < ConfigMapBlockInfo.width - 1; x++)
                    for (var y = 0; y < ConfigMapBlockInfo.height - 1; y++)
                        blocks[w, h].extends[x, y] = reader.ReadByte();
            }
    }
    public const string path = "Configs/MapBlocks";
    public const string path_data = "_Data";
}
