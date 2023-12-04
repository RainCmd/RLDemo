using System;
using RainLanguage;

public enum MagicNodeType
{
    Alter,
    Missile,
}

[Serializable]
public struct ConfigMagicNode
{
    public string name;
    public long icon;
    public string resouce;
    public Real cd;
    public Real cost;
    public MagicNodeType type;
    public long multiple;//重复触发次数
    public long sequence;//顺序触发次数
    public long number;//可以使用的次数
    public string desc;
    public string logic;
    public const string Path = "/Configs/MagicNode.cfg";
}
