using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class RoomClient : IRoom
{
    private bool _disposed;
    private RoomInfo info;
    private readonly Socket socket;
    private DateTime lastHeartbeat;
    private readonly Queue<Operator> operators = new Queue<Operator>();
    private readonly List<FrameOperator> frameOperators = new List<FrameOperator>();
    private long serverFrame;
    private readonly Stack<List<PlayerOperator>> pool = new Stack<List<PlayerOperator>>();

    public event Action<PlayerInfo, string> OnRecvMsg;
    public event Action<Guid> OnRemovePlayerInfo;
    public event Action<RoomInfo.MemberInfo> OnUpdateMember;
    public event Action OnUnexpectedExit;
    public event Action OnGameStart;
    public event Action<Guid, float> OnUpdatePlayerLoading;
    public event Action OnEntryGame;

    public RoomState State { get; private set; }
    public RoomInfo Info => info;
    public long Frame { get; private set; }
    public long OverstockFrame { get; private set; }

    public RoomClient(RoomInfo info)
    {
        _disposed = false;
        State = RoomState.Ready;
        this.info = info;
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        var port = 1994;
    rebind:
        try
        {
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
        }
        catch (Exception e)
        {
            port++;
            GameLog.Show(Color.white, e.Message);
            goto rebind;
        }
        new Thread(Recv).Start();
        new Thread(ActiveCheck).Start();
        Frame = 0;
    }
    private void Recv()
    {
        var readerBuffer = new byte[2048];
        var writerBuffer = new byte[2048];
        while (!_disposed)
        {
            EndPoint remote = info.ip;
            var size = socket.ReceiveFrom(readerBuffer, ref remote);
            var reader = new ProtoReader(readerBuffer, size);
            if (reader.valid)
            {
                var guid = reader.ReadGuid();
                if (guid == info.id)
                {
                    switch (reader.ReadRoomProto())
                    {
                        case RoomProto.SHallDelayTest: break;
                        case RoomProto.CHeartbeat:
                            {
                                lastHeartbeat = DateTime.Now;
                                var writer = new ProtoWriter(writerBuffer);
                                writer.Write(guid);
                                writer.Write(RoomProto.SHeartbeatRes);
                                writer.Write(PlayerInfo.Local.id);
                                writer.Write(reader.ReadLong());
                                writer.Send(socket, info.ip);
                            }
                            break;
                        case RoomProto.SHeartbeatRes: break;
                        case RoomProto.SRoomMsg: break;
                        case RoomProto.CRoomMsg:
                            {
                                var id = reader.ReadGuid();
                                PlayerInfo player;
                                lock (info.members)
                                {
                                    var idx = info.members.FindIndex(v => v.player.id == id);
                                    if (idx < 0) break;
                                    player = info.members[idx].player;
                                }
                                OnRecvMsg?.Invoke(player, reader.ReadString());
                            }
                            break;
                        case RoomProto.SJoinReq: break;
                        case RoomProto.SJoin: break;
                        case RoomProto.SLeave: break;
                        case RoomProto.CEntryPlayer:
                            {
                                if (State != RoomState.Ready) break;
                                var player = reader.ReadPlayerInfo();
                                var ctrlId = reader.ReadInt();
                                RoomInfo.MemberInfo member;
                                lock (info.members)
                                {
                                    var idx = info.members.FindIndex(v => v.player.id == player.id);
                                    if (idx < 0)
                                    {
                                        member = new RoomInfo.MemberInfo(player, ctrlId, false, 0);
                                        info.members.Add(member);
                                    }
                                    else
                                    {
                                        member = info.members[idx];
                                        member.player = player;
                                        info.members[idx] = member;
                                    }
                                }
                                OnUpdateMember?.Invoke(member);
                            }
                            break;
                        case RoomProto.CUpdatePlayer:
                            {
                                var id = reader.ReadGuid();
                                RoomInfo.MemberInfo member;
                                lock (info.members)
                                {
                                    var idx = info.members.FindIndex(v => v.player.id == id);
                                    if (idx < 0) break;
                                    member = info.members[idx];
                                    member.ready = reader.ReadBool();
                                    member.delay = reader.ReadInt();
                                    info.members[idx] = member;
                                }
                                OnUpdateMember?.Invoke(member);
                            }
                            break;
                        case RoomProto.CRemovePlayer:
                            {
                                if (State != RoomState.Ready) break;
                                var id = reader.ReadGuid();
                                lock (info.members)
                                {
                                    var idx = info.members.FindIndex(v => v.player.id == id);
                                    if (idx < 0) break;
                                    info.members.RemoveAt(idx);
                                }
                                OnRemovePlayerInfo?.Invoke(id);
                            }
                            break;
                        case RoomProto.CDissolve:
                            GameLog.Show(Color.yellow, "房间已解散！");
                            OnUnexpectedExit?.Invoke();
                            break;
                        case RoomProto.CReject:
                            GameLog.Show(Color.yellow, "请求被积极拒绝！");
                            OnUnexpectedExit?.Invoke();
                            break;
                        case RoomProto.SReady: break;
                        case RoomProto.SCancelReady: break;
                        case RoomProto.CStartGame:
                            {
                                if (State != RoomState.Ready) break;
                                State = RoomState.Loading;
                                info.seed = reader.ReadLong();
                                var count = reader.ReadInt();
                                lock (info.members)
                                {
                                    info.members.Clear();
                                    while (count-- > 0) info.members.Add(reader.ReadRoomMemberInfo());
                                }
                                var writer = new ProtoWriter(writerBuffer);
                                writer.Write(guid);
                                writer.Write(RoomProto.SStartGameRes);
                                writer.Write(PlayerInfo.Local.id);
                                writer.Send(socket, info.ip);
                                OnGameStart?.Invoke();
                            }
                            break;
                        case RoomProto.SStartGameRes: break;
                        case RoomProto.SLoading: break;
                        case RoomProto.CLoading:
                            {
                                if (State != RoomState.Loading) break;
                                var ownerProgress = reader.ReadFloat();
                                OnUpdatePlayerLoading?.Invoke(info.owner.id, ownerProgress);
                                var count = reader.ReadInt();
                                while (count-- > 0)
                                {
                                    Guid id;
                                    lock (info.members) id = info.members[reader.ReadInt()].player.id;
                                    var progress = reader.ReadFloat();
                                    OnUpdatePlayerLoading?.Invoke(id, progress);
                                }
                            }
                            break;
                        case RoomProto.CEntryGame:
                            {
                                if (State != RoomState.Loading) break;
                                State = RoomState.Game;
                                serverFrame = 0;
                                Frame = 0;
                                lock (operators) operators.Clear();
                                lock (frameOperators) frameOperators.Clear();
                                var writer = new ProtoWriter(writerBuffer);
                                writer.Write(guid);
                                writer.Write(RoomProto.SEntryGameRes);
                                writer.Write(PlayerInfo.Local.id);
                                writer.Send(socket, info.ip);
                                OnEntryGame?.Invoke();
                            }
                            break;
                        case RoomProto.SEntryGameRes: break;
                        case RoomProto.SOperator: break;
                        case RoomProto.COperator:
                            {
                                if (State != RoomState.Game) break;
                                serverFrame = reader.ReadLong();
                                var frameCount = reader.ReadInt();
                                while (frameCount-- > 0)
                                {
                                    var frame = reader.ReadLong();
                                    lock (frameOperators)
                                    {
                                        if (frameOperators.FindIndex(v => v.frame == frame) < 0)
                                        {
                                            lock (pool) frameOperators.Add(new FrameOperator(frame, pool.Count > 0 ? pool.Pop() : new List<PlayerOperator>()));
                                            var foper = frameOperators[frameOperators.Count - 1];
                                            var operatorCount = reader.ReadInt();
                                            while (operatorCount-- > 0)
                                            {
                                                var ctrlId = reader.ReadInt();
                                                var oper = reader.ReadOperator();
                                                foper.operators.Add(new PlayerOperator(ctrlId, oper));
                                            }
                                        }
                                        else
                                        {
                                            var operatorCount = reader.ReadInt();
                                            while (operatorCount-- > 0)
                                            {
                                                reader.ReadInt();
                                                reader.ReadOperator();
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                    }
                }
            }
        }
    }
    private void ActiveCheck()
    {
        lastHeartbeat = DateTime.Now;
        while (!_disposed)
        {
            Thread.Sleep(1000);
            if (DateTime.Now - lastHeartbeat > TimeSpan.FromSeconds(3))
            {
                GameLog.Show(Color.red, "已与主机失联！");
                OnUnexpectedExit?.Invoke();
            }
        }
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
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
        writer.Send(socket, info.ip);
    }
    public void Ready(bool ready)
    {
        var writer = new ProtoWriter(operatorBuffer);
        writer.Write(info.id);
        writer.Write(ready ? RoomProto.SReady : RoomProto.SCancelReady);
        writer.Write(PlayerInfo.Local.id);
        writer.Send(socket, info.ip);
    }
    public void Exit()
    {
        if (_disposed) return;
        var writer = new ProtoWriter(operatorBuffer);
        writer.Write(info.id);
        writer.Write(RoomProto.SLeave);
        writer.Write(PlayerInfo.Local.id);
        writer.Send(socket, info.ip);
    }

    public void UpdateLoading(float progress)
    {
        if (State != RoomState.Loading) return;
        var writer = new ProtoWriter(operatorBuffer);
        writer.Write(info.id);
        writer.Write(RoomProto.SLoading);
        writer.Write(progress);
        writer.Send(socket, info.ip);
    }
    public void UpdateOperator(Operator oper)
    {
        if (State != RoomState.Game) return;
        lock (operators) operators.Enqueue(oper);
    }

    private readonly byte[] logicBuffer = new byte[2048];
    private readonly List<long> lackFrames = new List<long>();
    public List<PlayerOperator> EntryNextFrame()
    {
        if (State != RoomState.Game) return null;
        lackFrames.Clear();
        lock (frameOperators)
        {
            if (frameOperators.Count == 0) return null;
            frameOperators.RemoveAll(v => v.frame < Frame);
            frameOperators.Sort((a, b) => a.frame.CompareTo(b.frame));
            var idx = 0;
            for (var frame = Frame; frame < serverFrame; frame++)
            {
                if (idx < frameOperators.Count && frameOperators[idx].frame == frame) idx++;
                else lackFrames.Add(frame);
            }
        }
        if (lackFrames.Count > 0 || operators.Count > 0)
        {
            var writer = new ProtoWriter(logicBuffer);
            writer.Write(info.id);
            writer.Write(RoomProto.SOperator);
            writer.Write(PlayerInfo.Local.id);
            writer.Write(lackFrames.Count);
            foreach (var lack in lackFrames) writer.Write(lack);
            lock (operators)
            {
                writer.Write(operators.Count);
                while (operators.Count > 0) writer.Write(operators.Dequeue());
            }
            writer.Send(socket, info.ip);
        }
        lock (frameOperators)
        {
            OverstockFrame = 0;
            for (var i = 0; i < frameOperators.Count; i++)
                if (frameOperators[i].frame == Frame + i)
                    OverstockFrame++;
            if (OverstockFrame > 0)
            {
                var result = frameOperators[0].operators;
                frameOperators.RemoveAt(0);
                Frame++;
                return result;
            }
        }
        return null;
    }

    public void Recyle(List<PlayerOperator> operators)
    {
        operators.Clear();
        lock (pool) pool.Push(operators);
    }
}
