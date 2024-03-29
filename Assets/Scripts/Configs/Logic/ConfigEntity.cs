using System;

[Serializable]
public struct ConfigEntity
{
    [ConfigId]
    public long id;
    public string resource;
    public const string Path = "Configs/Logic/Entity.bytes";
}
