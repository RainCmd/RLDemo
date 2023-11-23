using UnityEngine;

[System.Serializable]
public struct ConfigSplatInfo
{
    public Texture splat;
    public bool extend;
    public int weight;
    public float cohesion;
}
public class ConfigSplat : ConfigInfos<ConfigSplatInfo>
{
    public const string path = "Configs/Splats";
}
