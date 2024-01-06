using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using RainLanguage;

//Guid      房间id
//byte      HallProto:
//  HallProto.Broadcast:
//      ushort      Port
//      int         icon
//      string      房间名
//      int         玩家数
//  HallProto.DelayTest:
//      long        时间戳
//  HallProto.JoinRes:
//      string      房间名
//      PlayerInfo  房主信息
//      int         成员数量
//          loop成员数量:
//              MemberInfo  成员信息
public enum HallProto
{
    Broadcast,
    DelayTest,
    JoinRes,
}

//Guid  房间id
//byte  RoomProto:
//  RoomProto.SHallDelayTest:
//      long    时间戳
//  RoomProto.CHeartbeat:
//      long    时间戳
//  RoomProto.SHeartbeatRes:
//      Guid    玩家id
//      long    时间戳
//  RoomProto.SRoomMsg:
//      guid    玩家id
//      string  msg
//  RoomProto.CRoomMsg:
//      guid    玩家id
//      string  msg
//  RoomProto.SJoinReq:
//  RoomProto.SJoin:
//      PlayerInfo  玩家信息
//  RoomProto.SLeave:
//      Guid    玩家id
//  RoomProto.CEntryPlayer:
//      PlayerInfo  玩家信息
//      int         控制id
//  RoomProto.CUpdatePlayer:
//      Guid    玩家id
//      bool    准备状态
//      int     延迟
//  RoomProto.CRemovePlayer:
//      Guid    玩家id
//  RoomProto.CDissolve:
//  RoomProto.CReject:
//  RoomProto.SReady:
//      Guid    玩家id
//  RoomProto.SCancelReady:
//      Guid    玩家id
//  RoomProto.CStartGame:
//      long    seed
//      int     玩家数
//          loop玩家数：
//              MemberInfo  成员信息
//  RoomProto.SStartGameRes:
//      Guid    玩家id
//  RoomProto.SLoading:
//      Guid    玩家id
//      float   加载进度
//  RoomProto.CLoading:
//      float   房主加载进度
//      int     玩家数
//          loop玩家数:
//              int     玩家编号
//              float   加载进度
//  RoomProto.CEntryGame:
//  RoomProto.SEntryGameRes:
//      Guid    玩家id
//  RoomProto.SOperator:
//      Guid    玩家id
//      int     缺少的帧数
//          loop缺少的帧数:
//              long    缺少的帧
//      int     操作数
//          loop操作数:
//              Operator    操作
//  RoomProto.COperator:
//      long    最新帧
//      int     帧数
//          loop帧数：
//              long    帧
//              int     操作数
//                  loop操作数：
//                      int         玩家编号
//                      Operator    操作
public enum RoomProto
{
    SHallDelayTest,
    CHeartbeat,
    SHeartbeatRes,
    SRoomMsg,
    CRoomMsg,
    SJoinReq,
    SJoin,
    SLeave,
    CEntryPlayer,
    CUpdatePlayer,
    CRemovePlayer,
    CDissolve,
    CReject,
    SReady,
    SCancelReady,
    CStartGame,
    SStartGameRes,
    SLoading,
    CLoading,
    CEntryGame,
    SEntryGameRes,
    SOperator,
    COperator,
}

public struct ProtoWriter
{
    public int position;
    public readonly byte[] buffer;
    public ProtoWriter(byte[] buffer)
    {
        position = 0;
        this.buffer = buffer;
        buffer[position++] = (byte)'R';
        buffer[position++] = (byte)'A';
        buffer[position++] = (byte)'I';
        buffer[position++] = (byte)'N';
    }
    public void Write(HallProto proto)
    {
        buffer[position++] = (byte)proto;
    }
    public void Write(RoomProto proto)
    {
        buffer[position++] = (byte)proto;
    }
    public void Write(bool value)
    {
        buffer[position++] = (byte)(value ? 1 : 0);
    }
    public void Write(PlayerInfo info)
    {
        Write(info.id);
        Write(info.headIcon);
        Write(info.name);
    }
    public void Write(RoomInfo.MemberInfo info)
    {
        Write(info.player);
        Write(info.ctrlId);
        Write(info.ready);
        Write(info.delay);
    }
    public void Write(Real real)
    {
        Write(real.value);
    }
    public void Write(Operator oper)
    {
        Write((int)oper.type);
        switch (oper.type)
        {
            case OperatorType.Rocker:
                Write(oper.direction);
                Write(oper.distance);
                break;
            case OperatorType.Fire:
                Write(oper.direction);
                break;
            case OperatorType.StopFire:
                break;
            case OperatorType.SwitchWeapon:
                Write(oper.weapon);
                break;
            case OperatorType.Pick:
                Write(oper.target);
                break;
            case OperatorType.Drop:
                Write(oper.target);
                break;
            case OperatorType.Equip:
                Write(oper.target);
                Write(oper.weapon);
                Write(oper.slot);
                break;
        }
    }
    public void WriteBytes(byte[] bytes)
    {
        Array.Copy(bytes, 0, buffer, position, bytes.Length);
        position += bytes.Length;
    }
    public void Write(Guid guid)
    {
        Write(guid.ToString());
    }
    public void Write(ushort value)
    {
        WriteBytes(BitConverter.GetBytes(value));
    }
    public void Write(int value)
    {
        WriteBytes(BitConverter.GetBytes(value));
    }
    public void Write(float value)
    {
        WriteBytes(BitConverter.GetBytes(value));
    }
    public void Write(double value)
    {
        WriteBytes(BitConverter.GetBytes(value));
    }
    public void Write(long value)
    {
        WriteBytes(BitConverter.GetBytes((ulong)value));
    }
    public void Write(string value)
    {
        var buffer = Encoding.UTF8.GetBytes(value);
        Write(buffer.Length);
        WriteBytes(buffer);
    }
    public unsafe void Send(Socket socket, EndPoint remote)
    {
        fixed (byte* pbuffer = buffer)
        {
            uint* sp = (uint*)pbuffer;
            uint sum = 0;
            for (int i = 4; i < buffer.Length; i++) sum += buffer[i];
            *sp ^= sum;
            socket.SendTo(buffer, position, SocketFlags.None, remote);
            *sp ^= sum;
        }
    }
    public void Broadcast(Socket socket)
    {
        Send(socket, broadcastIP);
    }
    private static readonly IPEndPoint broadcastIP = new IPEndPoint(IPAddress.Broadcast, Config.HallPort);
}
public struct ProtoReader
{
    public int position;
    public readonly byte[] buffer;
    public readonly bool valid;
    public unsafe ProtoReader(byte[] buffer, int size)
    {
        position = 4;
        this.buffer = buffer;
        fixed (byte* pbuffer = buffer)
        {
            uint* sp = (uint*)pbuffer;
            uint sum = 0;
            for (int i = 4; i < buffer.Length; i++) sum += buffer[i];
            *sp ^= sum;
            valid = size >= 4 && buffer[0] == 'R' && buffer[1] == 'A' && buffer[2] == 'I' && buffer[3] == 'N';
            *sp ^= sum;
        }
    }
    public HallProto ReadHallProto()
    {
        return (HallProto)buffer[position++];
    }
    public RoomProto ReadRoomProto()
    {
        return (RoomProto)buffer[position++];
    }
    public bool ReadBool()
    {
        return buffer[position++] != 0;
    }
    public PlayerInfo ReadPlayerInfo()
    {
        var id = ReadGuid();
        var icon = ReadInt();
        var name = ReadString();
        return new PlayerInfo(id, icon, name);
    }
    public RoomInfo.MemberInfo ReadRoomMemberInfo()
    {
        var player = ReadPlayerInfo();
        var ctrlId = ReadInt();
        var ready = ReadBool();
        var delay = ReadInt();
        return new RoomInfo.MemberInfo(player, ctrlId, ready, delay);
    }
    public Operator ReadOperator()
    {
        var oper = (OperatorType)ReadInt();
        switch (oper)
        {
            case OperatorType.Rocker:
                {
                    var direction = ReadReal();
                    var distance = ReadReal();
                    return Operator.Rocker(direction, distance);
                }
            case OperatorType.Fire:
                {
                    var direction = ReadReal();
                    return Operator.Fire(direction);
                }
            case OperatorType.StopFire: return Operator.StopFire();
            case OperatorType.SwitchWeapon:
                {
                    var weapon = ReadInt();
                    return Operator.SwitchWeapon(weapon);
                }
            case OperatorType.Pick:
                {
                    var target = ReadLong();
                    return Operator.Pick(target);
                }
            case OperatorType.Drop:
                {
                    var target = ReadLong();
                    return Operator.Drop(target);
                }
            case OperatorType.Equip:
                {
                    var target = ReadLong();
                    var weapon = ReadInt();
                    var slot = ReadInt();
                    return Operator.Equip(target, weapon, slot);
                }
        }
        return new Operator(oper);
    }
    public ushort ReadUShort()
    {
        var result = BitConverter.ToUInt16(buffer, position);
        position += 2;
        return result;
    }
    public int ReadInt()
    {
        var result = BitConverter.ToInt32(buffer, position);
        position += 4;
        return result;
    }
    public float ReadFloat()
    {
        var result = BitConverter.ToSingle(buffer, position);
        position += 4;
        return result;
    }
    public double ReadDouble()
    {
        var result = BitConverter.ToDouble(buffer, position);
        position += 8;
        return result;
    }
    public Real ReadReal()
    {
#if FIXED_REAL
        return new Real(ReadLong());
#else
        return new Real(ReadDouble());
#endif
    }
    public long ReadLong()
    {
        var result = BitConverter.ToInt64(buffer, position);
        position += 8;
        return result;
    }
    public string ReadString()
    {
        var size = ReadInt();
        var result = Encoding.UTF8.GetString(buffer, position, size);
        position += size;
        return result;
    }
    public Guid ReadGuid()
    {
        return Guid.Parse(ReadString());
    }
}