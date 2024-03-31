using RainLanguage;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ConfigBuff
{
    [ConfigId]
    public long id;
    public string name;
    public string description;
    public long icon;
    public bool isDebuff;
    /// <summary>
    /// 刷新持续时间
    /// </summary>
    public bool refresh;
    /// <summary>
    /// 堆积
    /// </summary>
    public bool accumulation;
    /// <summary>
    /// 持续时间
    /// </summary>
    public Real duration;
    public string logic;
    public const string Path = "Configs/Logic/Buff.bytes";
}
