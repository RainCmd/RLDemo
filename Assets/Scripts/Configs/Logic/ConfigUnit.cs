using System;

public enum UnitType
{
    Player,
    Npc,
    NpcNoMana,
}

[Serializable]
public struct ConfigUnit
{
    public string name;
    public string entity;
    public UnitType type;
    public Real hp;
    public Real mp;
    public Real speed;
    public const string Path = "/Configs/Unit.cfg";
}
