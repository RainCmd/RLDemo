using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

public struct RoomSummaryInfo
{
    public IPEndPoint ip;
    public Guid id;
    public int icon;
    public string name;
    public int players;
    public int delay;
    public DateTime time;
    public bool Active
    {
        get
        {
            return DateTime.Now - time < TimeSpan.FromSeconds(2);
        }
    }
    public RoomSummaryInfo(IPEndPoint ip, Guid id, int icon, string name, int players, int delay, DateTime time)
    {
        this.ip = ip;
        this.id = id;
        this.icon = icon;
        this.name = name;
        this.players = players;
        this.delay = delay;
        this.time = time;
    }
    public RoomSummaryInfo(IPAddress address, Guid id, ProtoReader reader)
    {
        ip = new IPEndPoint(address, reader.ReadUShort());
        this.id = id;
        icon = reader.ReadInt();
        name = reader.ReadString();
        players = reader.ReadInt();
        delay = 0;
        time = DateTime.Now;
    }
    public override bool Equals(object obj)
    {
        return obj is RoomSummaryInfo info && info == this;
    }
    public override int GetHashCode()
    {
        return 1877310944 + id.GetHashCode();
    }

    public static bool operator ==(RoomSummaryInfo left, RoomSummaryInfo right)
    {
        return left.id == right.id;
    }
    public static bool operator !=(RoomSummaryInfo left, RoomSummaryInfo right)
    {
        return left.id != right.id;
    }
}
public struct RoomInfo
{
    public struct MemberInfo
    {
        public PlayerInfo player;
        public IPEndPoint ip;
        public bool ready;
        public int delay;
        public DateTime time;
        public bool Active
        {
            get
            {
                return DateTime.Now - time < TimeSpan.FromSeconds(2);
            }
        }
        public MemberInfo(PlayerInfo player, bool ready, int delay) : this(player, null, ready, delay) { }
        public MemberInfo(PlayerInfo player, IPEndPoint ip, bool ready, int delay)
        {
            this.player = player;
            this.ip = ip;
            this.ready = ready;
            this.delay = delay;
            time = DateTime.Now;
        }
        public void Update() { time = DateTime.Now; }
        public override bool Equals(object obj)
        {
            return obj is MemberInfo info &&
                   EqualityComparer<PlayerInfo>.Default.Equals(player, info.player);
        }
        public override int GetHashCode()
        {
            return -245417216 + player.GetHashCode();
        }

        public static bool operator ==(PlayerInfo left, MemberInfo right)
        {
            return left == right.player;
        }
        public static bool operator !=(PlayerInfo left, MemberInfo right)
        {
            return left != right.player;
        }
    }
    public IPEndPoint ip;
    public Guid id;
    public long seed;
    public string name;
    public PlayerInfo owner;
    public List<MemberInfo> members;

    public RoomInfo(IPEndPoint ip, Guid id, string name, PlayerInfo owner, List<MemberInfo> members)
    {
        this.ip = ip;
        this.id = id;
        seed = 0;
        this.name = name;
        this.owner = owner;
        this.members = members;
    }
}
