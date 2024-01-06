using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class RoomServer : IRoom
{
    private bool _disposed;
    public RoomInfo info;
    private readonly HashSet<Guid> dirtyPlayers = new HashSet<Guid>();
    private readonly HashSet<Guid> started = new HashSet<Guid>();
    private readonly HashSet<Guid> entered = new HashSet<Guid>();
    private readonly Socket socket;
    private readonly Dictionary<int, float> loadingProgress = new Dictionary<int, float>();
    private List<PlayerOperator> currentOperators = new List<PlayerOperator>();
    private readonly List<FrameOperator> frameOperators = new List<FrameOperator>();
    private readonly Stack<List<PlayerOperator>> pool = new Stack<List<PlayerOperator>>();

    public event Action<PlayerInfo, string> OnRecvMsg;
    public event Action<RoomInfo.MemberInfo> OnUpdateMember;
    public event Action<Guid> OnRemovePlayerInfo;
    public event Action OnUnexpectedExit;
    public event Action OnGameStart;
    public event Action<Guid, float> OnUpdatePlayerLoading;
    public event Action OnEntryGame;

    private readonly Stack<int> ctrlIdPool = new Stack<int>();
    private int ctrlIdx = 1;

    public RoomState State { get; private set; }
    public RoomInfo Info => info;
    public long Frame { get; private set; }
    public long OverstockFrame { get { return 0; } }
    public RoomServer(string name)
    {
        _disposed = false;
        State = RoomState.Ready;
        info = new RoomInfo(null, Guid.NewGuid(), name, PlayerInfo.Local, 0, new List<RoomInfo.MemberInfo>());
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        var port = 2023;
    rebind:
        try
        {
            socket.Bind(info.ip = new IPEndPoint(IPAddress.Any, port));
        }
        catch (Exception e)
        {
            port++;
            GameLog.Show(Color.white, e.Message);
            goto rebind;
        }
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
        new Thread(Accept).Start();
        new Thread(Heartbeat).Start();
    }
    private void Accept()
    {
        var lacks = new List<FrameOperator>();
        var readerBuffer = new byte[2048];
        var writerBuffer = new byte[2048];
        var ip = new IPEndPoint(IPAddress.Any, 0);
        while (!_disposed)
        {
            EndPoint remote = ip;
            var size = socket.ReceiveFrom(readerBuffer, ref remote);
            var rip = (IPEndPoint)remote;
            var reader = new ProtoReader(readerBuffer, size);
            if (reader.valid)
            {
                var guid = reader.ReadGuid();
                if (guid == info.id)
                {
                    switch (reader.ReadRoomProto())
                    {
                        case RoomProto.SHallDelayTest:
                            {
                                if (State != RoomState.Ready) break;
                                var writer = new ProtoWriter(writerBuffer);
                                writer.Write(guid);
                                writer.Write(HallProto.DelayTest);
                                writer.Write(reader.ReadLong());
                                writer.Send(socket, rip);
                            }
                            break;
                        case RoomProto.CHeartbeat: break;
                        case RoomProto.SHeartbeatRes:
                            {
                                var id = reader.ReadGuid();
                                lock (info.members)
                                {
                                    var idx = info.members.FindIndex(v => v.player.id == id);
                                    if (idx >= 0)
                                    {
                                        var member = info.members[idx];
                                        member.delay = Tool.GetDelay(reader.ReadLong());
                                        member.Update();
                                        info.members[idx] = member;
                                        lock (dirtyPlayers) dirtyPlayers.Add(id);
                                    }
                                    else Reject(rip, writerBuffer);
                                }
                            }
                            break;
                        case RoomProto.SRoomMsg:
                            {
                                var id = reader.ReadGuid();
                                var msg = reader.ReadString();
                                PlayerInfo player;
                                lock (info.members)
                                {
                                    var idx = info.members.FindIndex(v => v.player.id == id);
                                    if (idx < 0)
                                    {
                                        Reject(rip, writerBuffer);
                                        break;
                                    }
                                    var writer = new ProtoWriter(writerBuffer);
                                    writer.Write(guid);
                                    writer.Write(RoomProto.CRoomMsg);
                                    writer.Write(id);
                                    writer.Write(msg);
                                    Broadcast(writer);
                                    player = info.members[idx].player;
                                }
                                OnRecvMsg?.Invoke(player, msg);
                            }
                            break;
                        case RoomProto.CRoomMsg: break;
                        case RoomProto.SJoinReq:
                            {
                                if (State != RoomState.Ready) break;
                                var writer = new ProtoWriter(writerBuffer);
                                writer.Write(guid);
                                writer.Write(HallProto.JoinRes);
                                writer.Write(info.name);
                                writer.Write(info.owner);
                                writer.Write(info.ctrlId);
                                lock (info.members)
                                {
                                    writer.Write(info.members.Count);
                                    foreach (var member in info.members) writer.Write(member);
                                }
                                writer.Send(socket, rip);
                            }
                            break;
                        case RoomProto.SJoin:
                            {
                                if (State != RoomState.Ready)
                                {
                                    Reject(rip, writerBuffer);
                                    break;
                                }
                                var player = reader.ReadPlayerInfo();
                                RoomInfo.MemberInfo member;
                                lock (info.members)
                                {
                                    var idx = info.members.FindIndex(v => v.player.id == player.id);
                                    if (idx < 0)
                                    {
                                        member = new RoomInfo.MemberInfo(player, rip,
                                            ctrlIdPool.Count > 0 ? ctrlIdPool.Pop() : ctrlIdx++,    //理论上应该由玩家自己选择控制id，这里简化成自动分配了
                                            false, 0);
                                        info.members.Add(member);
                                        var writer = new ProtoWriter(writerBuffer);
                                        writer.Write(guid);
                                        writer.Write(RoomProto.CEntryPlayer);
                                        writer.Write(player);
                                        Broadcast(writer);
                                    }
                                    else
                                    {
                                        member = info.members[idx];
                                        member.player = player;
                                        info.members[idx] = member;
                                        lock (dirtyPlayers) dirtyPlayers.Add(player.id);
                                    }
                                }
                                OnUpdateMember?.Invoke(member);
                            }
                            break;
                        case RoomProto.SLeave:
                            {
                                var id = reader.ReadGuid();
                                lock (info.members)
                                {
                                    var idx = info.members.FindIndex(v => v.player.id == id);
                                    if (idx < 0)
                                    {
                                        Reject(rip, writerBuffer);
                                        break;
                                    }
                                    if (State == RoomState.Ready)
                                    {
                                        ctrlIdPool.Push(info.members[idx].ctrlId);
                                        info.members.RemoveAt(idx);
                                        var writer = new ProtoWriter(writerBuffer);
                                        writer.Write(guid);
                                        writer.Write(RoomProto.CRemovePlayer);
                                        writer.Write(id);
                                        Broadcast(writer);
                                    }
                                    else
                                    {
                                        var member = info.members[idx];
                                        member.ready = false;
                                        info.members[idx] = member;
                                        lock (dirtyPlayers) dirtyPlayers.Add(id);
                                    }
                                }
                                OnRemovePlayerInfo?.Invoke(id);
                            }
                            break;
                        case RoomProto.CEntryPlayer: break;
                        case RoomProto.CUpdatePlayer: break;
                        case RoomProto.CRemovePlayer: break;
                        case RoomProto.CDissolve: break;
                        case RoomProto.CReject: break;
                        case RoomProto.SReady:
                            {
                                if (State != RoomState.Ready) break;
                                var id = reader.ReadGuid();
                                lock (info.members)
                                {
                                    var idx = info.members.FindIndex(v => v.player.id == id);
                                    if (idx < 0) Reject(rip, writerBuffer);
                                    else if (!info.members[idx].ready)
                                    {
                                        var member = info.members[idx];
                                        member.ready = true;
                                        info.members[idx] = member;
                                        lock (dirtyPlayers) dirtyPlayers.Add(id);
                                    }
                                }
                            }
                            break;
                        case RoomProto.SCancelReady:
                            {
                                if (State != RoomState.Ready) break;
                                var id = reader.ReadGuid();
                                lock (info.members)
                                {
                                    var idx = info.members.FindIndex(v => v.player.id == id);
                                    if (idx < 0) Reject(rip, writerBuffer);
                                    else if (info.members[idx].ready)
                                    {
                                        var member = info.members[idx];
                                        member.ready = false;
                                        info.members[idx] = member;
                                        lock (dirtyPlayers) dirtyPlayers.Add(id);
                                    }
                                }
                            }
                            break;
                        case RoomProto.CStartGame: break;
                        case RoomProto.SStartGameRes:
                            {
                                var id = reader.ReadGuid();
                                lock (started) started.Remove(id);
                            }
                            break;
                        case RoomProto.SLoading:
                            {
                                if (State != RoomState.Loading) break;
                                var id = reader.ReadGuid();
                                var progress = reader.ReadFloat();
                                lock (started) started.Remove(id);
                                lock (loadingProgress)
                                    lock (info.members)
                                    {
                                        var idx = info.members.FindIndex(v => v.player.id == id);
                                        if (idx >= 0) loadingProgress[idx] = progress;
                                    }
                                OnUpdatePlayerLoading?.Invoke(id, progress);
                            }
                            break;
                        case RoomProto.CLoading: break;
                        case RoomProto.CEntryGame: break;
                        case RoomProto.SEntryGameRes:
                            {
                                var id = reader.ReadGuid();
                                lock (entered) entered.Remove(id);
                            }
                            break;
                        case RoomProto.SOperator:
                            {
                                if (State != RoomState.Game) break;
                                var id = reader.ReadGuid();
                                lock (entered) entered.Remove(id);

                                var lackCount = reader.ReadInt();
                                if (lackCount > 0)
                                {
                                    var writer = new ProtoWriter(writerBuffer);
                                    writer.Write(info.id);
                                    writer.Write(RoomProto.COperator);
                                    writer.Write(Frame);
                                    lock (frameOperators)
                                    {
                                        lacks.Clear();
                                        while (lackCount-- > 0)
                                            if (TryFindFrameOperators(reader.ReadLong(), out FrameOperator frameOperator))
                                                lacks.Add(frameOperator);
                                    }
                                    writer.Write(lacks.Count);
                                    foreach (var lack in lacks)
                                    {
                                        writer.Write(lack.frame);
                                        writer.Write(lack.operators.Count);
                                        foreach (var item in lack.operators)
                                        {
                                            writer.Write(item.ctrlId);
                                            writer.Write(item.oper);
                                        }
                                    }
                                    writer.Send(socket, rip);
                                }

                                var operatorCount = reader.ReadInt();
                                while (operatorCount-- > 0) PlayerOperation(id, reader.ReadOperator());
                            }
                            break;
                        case RoomProto.COperator: break;
                    }
                }
            }
        }
    }
    private bool TryFindFrameOperators(long frame, out FrameOperator frameOperator)
    {
        if (frameOperators.Count > 0)
        {
            var min = 0; var max = frameOperators.Count;
            while (min < max)
            {
                var mid = (min + max) / 2;
                frameOperator = frameOperators[mid];
                if (frameOperator.frame == frame) return true;
                else if (frameOperator.frame < frame) max = mid;
                else if (min == mid) break;
                else min = mid;
            }
        }
        frameOperator = default;
        return false;
    }
    private void PlayerOperation(Guid id, Operator oper)
    {
        if (State != RoomState.Game) return;
        int ctrlId;
        if (id == info.owner.id) ctrlId = info.ctrlId;
        else lock (info.members)
            {
                var idx = info.members.FindIndex(v => v.player.id == id);
                if (idx < 0) return;
                ctrlId = info.members[idx].ctrlId;
            }

        lock (currentOperators) currentOperators.Add(new PlayerOperator(ctrlId, oper));
    }
    private void Reject(IPEndPoint ip, byte[] buffer)
    {
        var writer = new ProtoWriter(buffer);
        writer.Write(info.id);
        writer.Write(RoomProto.CReject);
        writer.Send(socket, ip);
    }
    private void Broadcast(ProtoWriter writer)
    {
        foreach (var member in info.members)
            if (State == RoomState.Ready || (member.ready && member.Active))
                writer.Send(socket, member.ip);
    }
    private void Heartbeat()
    {
        var buffer = new byte[2048];
        var removes = new List<Guid>();
        while (!_disposed)
        {
            Thread.Sleep(250);
            if (State == RoomState.Ready)
            {
                var writer = new ProtoWriter(buffer);
                writer.Write(info.id);
                writer.Write(HallProto.Broadcast);
                writer.Write((ushort)info.ip.Port);
                writer.Write(info.owner.headIcon);
                writer.Write(info.name);
                writer.Write(info.members.Count + 1);
                writer.Broadcast(socket);
            }

            lock (info.members)
            {
                if (State == RoomState.Ready)
                {
                    info.members.RemoveAll(member =>
                    {
                        if (member.Active) return false;
                        removes.Add(member.player.id);
                        dirtyPlayers.Remove(member.player.id);
                        return true;
                    });
                }
                else
                {
                    for (var i = 0; i < info.members.Count; i++)
                    {
                        var member = info.members[i];
                        if (member.ready && !member.Active)
                        {
                            member.ready = false;
                            info.members[i] = member;
                            dirtyPlayers.Add(member.player.id);
                        }
                    }
                }

                var writer = new ProtoWriter(buffer);
                writer.Write(info.id);
                writer.Write(RoomProto.CHeartbeat);
                writer.Write(DateTime.Now.Ticks);
                Broadcast(writer);
            }

            if (State == RoomState.Ready)
            {
                foreach (var id in removes)
                {
                    var writer = new ProtoWriter(buffer);
                    writer.Write(info.id);
                    writer.Write(RoomProto.CRemovePlayer);
                    writer.Write(id);
                    lock (info.members) Broadcast(writer);
                    OnRemovePlayerInfo?.Invoke(id);
                }
                removes.Clear();
            }

            lock (dirtyPlayers)
                lock (info.members)
                {
                    foreach (var member in info.members)
                        if (dirtyPlayers.Contains(member.player.id))
                        {
                            var writer = new ProtoWriter(buffer);
                            writer.Write(info.id);
                            writer.Write(RoomProto.CUpdatePlayer);
                            writer.Write(member.player.id);
                            writer.Write(member.ready);
                            writer.Write(member.delay);
                            Broadcast(writer);
                        }
                    dirtyPlayers.Clear();
                }
        }
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Dissolve();
        socket.Close();
        State = RoomState.Invalid;
    }

    private readonly byte[] operatorBuffer = new byte[2048];
    public void Send(string msg)
    {
        var writer = new ProtoWriter(operatorBuffer);
        writer.Write(info.id);
        writer.Write(RoomProto.SRoomMsg);
        writer.Write(PlayerInfo.Local.id);
        writer.Write(msg);
        lock (info.members) Broadcast(writer);
        OnRecvMsg?.Invoke(PlayerInfo.Local, msg);
    }
    public void StartGame()
    {
        lock (info.members)
        {
            foreach (var member in info.members)
                if (!member.ready)
                {
                    GameLog.Show(Color.white, "有玩家未准备");
                    return;
                }
            lock (started)
                lock (entered)
                {
                    started.Clear();
                    entered.Clear();
                    foreach (var member in info.members)
                    {
                        started.Add(member.player.id);
                        entered.Add(member.player.id);
                    }
                }
        }
        lock (loadingProgress) loadingProgress.Clear();
        info.seed = DateTime.Now.Ticks;
        State = RoomState.Loading;
        new Thread(UrgeGameStart).Start();
        OnGameStart?.Invoke();
    }
    private void UrgeGameStart()
    {
        var buffer = new byte[256];
        var writer = new ProtoWriter(buffer);
        writer.Write(info.id);
        writer.Write(RoomProto.CStartGame);
        writer.Write(info.seed);
        lock (info.members)
        {
            writer.Write(info.members.Count);
            foreach (var member in info.members)
                writer.Write(member);
        }
        while (!_disposed)
        {
            lock (started)
            {
                lock (info.members)
                {
                    started.RemoveWhere(id =>
                    {
                        var idx = info.members.FindIndex(v => v.player.id == id);
                        if (idx <= 0) return true;
                        var member = info.members[idx];
                        if (!member.Active || !member.ready) return true;
                        writer.Send(socket, member.ip);
                        return false;
                    });
                }
                if (started.Count == 0) break;
            }
            Thread.Sleep(500);
        }
    }
    public void Dissolve()
    {
        var writer = new ProtoWriter(operatorBuffer);
        writer.Write(info.id);
        writer.Write(RoomProto.CDissolve);
        lock (info.members) Broadcast(writer);
    }

    public void UpdateLoading(float progress)
    {
        if (State != RoomState.Loading) return;
        OnUpdatePlayerLoading?.Invoke(PlayerInfo.Local.id, progress);
        var writer = new ProtoWriter(operatorBuffer);
        writer.Write(info.id);
        writer.Write(RoomProto.CLoading);
        lock (loadingProgress)
        {
            lock (info.members)
                for (int i = 0; i < info.members.Count; i++)
                    if (!info.members[i].ready)
                        loadingProgress[i] = 1;
            writer.Write(progress);
            writer.Write(loadingProgress.Count);
            foreach (var item in loadingProgress)
            {
                writer.Write(item.Key);
                writer.Write(item.Value);
            }
        }
        //todo 记录下各自加载进度，如果没有发生变化就不广播
        lock (info.members) Broadcast(writer);

        if (progress == 1)
        {
            lock (loadingProgress)
                if (loadingProgress.Count == info.members.Count)
                    foreach (var item in loadingProgress.Values)
                        if (item < 1) return;
            EntryGame();
        }
    }
    private void EntryGame()
    {
        if (State != RoomState.Loading) return;
        Frame = 0;
        lock (currentOperators) currentOperators = GetPlayerOperators();
        lock (frameOperators) frameOperators.Clear();
        State = RoomState.Game;
        new Thread(UrgeEntryGame).Start();
        OnEntryGame?.Invoke();
    }
    private void UrgeEntryGame()
    {
        var buffer = new byte[256];
        var writer = new ProtoWriter(buffer);
        writer.Write(info.id);
        writer.Write(RoomProto.CEntryGame);
        while (!_disposed)
        {
            lock (entered)
            {
                lock (info.members)
                {
                    entered.RemoveWhere(id =>
                    {
                        var idx = info.members.FindIndex(v => v.player.id == id);
                        if (idx <= 0) return true;
                        var member = info.members[idx];
                        if (!member.ready || !member.Active) return true;
                        writer.Send(socket, member.ip);
                        return false;
                    });
                }
                if (entered.Count == 0) break;
            }
            Thread.Sleep(500);
        }
    }
    public void UpdateOperator(Operator oper)
    {
        PlayerOperation(info.owner.id, oper);
    }

    private readonly byte[] logicBuffer = new byte[2048];
    public List<PlayerOperator> EntryNextFrame()
    {
        if (State != RoomState.Game) return null;
        List<PlayerOperator> result;
        lock (currentOperators)
        {
            result = currentOperators;
            currentOperators = GetPlayerOperators();
        }
        var writer = new ProtoWriter(logicBuffer);
        writer.Write(info.id);
        writer.Write(RoomProto.COperator);
        writer.Write(Frame);
        if (result.Count > 0)
        {
            writer.Write(1);
            writer.Write(Frame);
            foreach (var item in result)
            {
                writer.Write(item.ctrlId);
                writer.Write(item.oper);
            }
        }
        else writer.Write(0);
        lock (info.members) Broadcast(writer);
        if (result.Count > 0)
        {
            var temp = GetPlayerOperators();
            temp.AddRange(result);
            lock (frameOperators) frameOperators.Add(new FrameOperator(Frame, temp));
        }
        Frame++;
        return result;
    }

    public void Recyle(List<PlayerOperator> operators)
    {
        operators.Clear();
        lock (pool) pool.Push(operators);
    }
    private List<PlayerOperator> GetPlayerOperators()
    {
        lock (pool) return pool.Count > 0 ? pool.Pop() : new List<PlayerOperator>();
    }
}
