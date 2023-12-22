using System;
using RainLanguage;

public enum UnitType
{
    Player,
    Npc,
    NpcNoMana,
}

[Serializable]
public struct ConfigUnit
{
    [ConfigId]
    public long id;
    public string name;
    public long entityId;
    public UnitType type;
    public Real hp;
    public Real mp;
    public Real speed;
    public const string Path = "/Configs/Unit.cfg";
}
