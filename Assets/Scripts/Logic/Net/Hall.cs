using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class Hall : IDisposable
{
    private bool _disposed;
    private Socket socket;
    private List<RoomSummaryInfo> infos = new List<RoomSummaryInfo>();
    public event Action<RoomSummaryInfo> Update;
    public event Action<Guid> Remove;
    public event Action<RoomInfo> Join;
    public Hall()
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        socket.Bind(new IPEndPoint(IPAddress.Any, Config.HallPort));
        _disposed = false;
        new Thread(Recv).Start();
        new Thread(Heartbeat).Start();
    }
    private void Recv()
    {
        var buffer = new byte[2048];
        var ip = new IPEndPoint(IPAddress.Any, Config.HallPort);
        while (!_disposed)
        {
            EndPoint remote = ip;
            try
            {
                var size = socket.ReceiveFrom(buffer, ref remote);
                var rip = (IPEndPoint)remote;
                var reader = new ProtoReader(buffer, size);
                if (reader.valid)
                {
                    var guid = reader.ReadGuid();
                    lock (infos)
                    {
                        var index = infos.FindIndex(v => v.id == guid);
                        switch (reader.ReadHallProto())
                        {
                            case HallProto.Broadcast:
                                {
                                    var info = new RoomSummaryInfo(rip.Address, guid, reader);
                                    if (index < 0) infos.Add(info);
                                    else
                                    {
                                        info.delay = infos[index].delay;
                                        infos[index] = info;
                                    }
                                    Update?.Invoke(info);
                                }
                                break;
                            case HallProto.DelayTest:
                                if (index >= 0)
                                {
                                    var info = infos[index];
                                    info.delay = Tool.GetDelay(reader.ReadLong());
                                    infos[index] = info;
                                    Update?.Invoke(info);
                                }
                                break;
                            case HallProto.JoinRes:
                                {
                                    var name = reader.ReadString();
                                    var owner = reader.ReadPlayerInfo();
                                    var members = new List<RoomInfo.MemberInfo>();
                                    var count = reader.ReadInt();
                                    while (count-- > 0) members.Add(reader.ReadRoomMemberInfo());
                                    Join?.Invoke(new RoomInfo(rip, guid, name, owner, members));
                                }
                                break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                GameLog.Show(UnityEngine.Color.red, e.Message);
            }
        }
    }
    private void Heartbeat()
    {
        var buffer = new byte[512];
        while (!_disposed)
        {
            Thread.Sleep(250);
            lock (infos)
            {
                infos.RemoveAll(info =>
                {
                    if (info.Active) return false;
                    Remove?.Invoke(info.id);
                    return true;
                });
                var timestamp = BitConverter.GetBytes(DateTime.Now.Ticks);
                foreach (var info in infos)
                {
                    var writer = new ProtoWriter(buffer);
                    writer.Write(info.id);
                    writer.Write(RoomProto.SHallDelayTest);
                    writer.WriteBytes(timestamp);
                    writer.Send(socket, info.ip);
                }
            }
        }
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        socket.Close();
    }
    ~Hall() { Dispose(); }
    private byte[] operatorBuffer = new byte[512];
    public void JoinRoom(RoomSummaryInfo info)
    {
        var writer = new ProtoWriter(operatorBuffer);
        writer.Write(info.id);
        writer.Write(RoomProto.SJoinReq);
        writer.Send(socket, info.ip);
    }
}