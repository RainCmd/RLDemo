using System;
using System.IO;
using UnityEngine;

public struct PlayerInfo
{
    public Guid id;
    public int headIcon;
    public string name;
    public PlayerInfo(Guid id, int headIcon, string name)
    {
        this.id = id;
        this.headIcon = headIcon;
        this.name = name;
    }
    public override bool Equals(object obj)
    {
        return obj is PlayerInfo info && info.id == id;
    }
    public override int GetHashCode()
    {
        return 1877310944 + id.GetHashCode();
    }
    public static bool operator ==(PlayerInfo left, PlayerInfo right)
    {
        return left.id == right.id;
    }
    public static bool operator !=(PlayerInfo left, PlayerInfo right)
    {
        return left.id != right.id;
    }
    private static readonly string path = Application.persistentDataPath + "/PlayerInfo.cfg";
    private static PlayerInfo local;
    public static PlayerInfo Local
    {
        get
        {
            return local;
        }
        set
        {
            local = value;
            using (var fs = File.Create(path))
            using (var sw = new StreamWriter(fs))
            {
                sw.WriteLine(value.id.ToString());
                sw.WriteLine(value.headIcon.ToString());
                sw.WriteLine(value.name);
            }
            OnLocalInfoChange?.Invoke(local);
        }
    }
    [RuntimeInitializeOnLoadMethod]
    public static void Init()
    {
        Debug.Log(path);
        if (File.Exists(path))
        {
            using (var fs = File.OpenRead(path))
            using (var sr = new StreamReader(fs))
            {
                Guid.TryParse(sr.ReadLine(), out local.id);
                int.TryParse(sr.ReadLine(), out local.headIcon);
                local.name = sr.ReadLine();
            }
        }
        else
        {
            Local = new PlayerInfo(Guid.NewGuid(), 0, "点击这里修改头像和名称");
        }
    }

    public static event Action<PlayerInfo> OnLocalInfoChange;
}
