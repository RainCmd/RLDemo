using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActivityRoom : UIActivity
{
    public RectTransform playerContent;
    public GameObject playerItem;
    public RectTransform msgContent;
    public GameObject msgItem;
    public InputField input;
    public Button btnStart;
    public Button btnReady;
    public Button btnCancel;
    public Button btnExit;
    public Button btnDissolve;
    private RoomServer server;
    private RoomClient client;
    private IRoom room;
    private ActivityRoomItemPlayer owner;
    private List<ActivityRoomItemPlayer> members;
    private Stack<ActivityRoomItemPlayer> pool;
    private Dictionary<Guid, RoomInfo.MemberInfo> updates;
    private HashSet<Guid> removes;
    private void Awake()
    {
        members = new List<ActivityRoomItemPlayer>();
        pool = new Stack<ActivityRoomItemPlayer>();
        updates = new Dictionary<Guid, RoomInfo.MemberInfo>();
        removes = new HashSet<Guid>();
        owner = CreateMemberInfo();
    }
    private void Update()
    {
        bool dirty = false;
        lock (removes)
        {
            dirty |= removes.Count > 0;
            members.RemoveAll(member =>
            {
                if (!removes.Contains(member.info.player.id)) return false;
                member.gameObject.SetActive(false);
                pool.Push(member);
                return true;
            });
            removes.Clear();
        }
        lock (updates)
        {
            dirty |= updates.Count > 0;
            foreach (var info in updates.Values)
            {
                var member = members.Find(v => v.info.player.id == info.player.id);
                if (!member)
                {
                    member = CreateMemberInfo();
                    members.Add(member);
                }
                member.UpdateInfo(info);
            }
            updates.Clear();
        }
        if (dirty) RefreshBtnsState();
    }
    public void Init(IRoom room)
    {
        if (room is RoomServer server)
        {
            this.server = server;
            client = null;
            server.OnUnexpectedExit += OnDissolveClick;
        }
        else if (room is RoomClient client)
        {
            this.client = client;
            server = null;
            client.OnUnexpectedExit += OnExitClick;
        }
        this.room = room;
        owner.UpdateInfo(room.Info.owner);
        room.OnRecvMsg += OnRecvMsg;
        room.OnUpdateMember += OnUpdatePlayerInfo;
        room.OnRemovePlayerInfo += OnRemovePlayerInfo;
        room.OnGameStart += OnGameStart;
        RefreshBtnsState();
        UpdateMemberInfo();
    }
    private void Deinit()
    {
        room.OnRemovePlayerInfo -= OnRemovePlayerInfo;
        room.OnUpdateMember -= OnUpdatePlayerInfo;
        room.OnRecvMsg -= OnRecvMsg;
        room.OnGameStart -= OnGameStart;
        if (client != null) client.OnUnexpectedExit -= OnExitClick;
        if (server != null) server.OnUnexpectedExit -= OnDissolveClick;
    }
    private void OnGameStart()
    {
        var room = this.room;
        Deinit();
        this.room = null;
        Do(() =>
        {
            UIManager.CloseAll();
            new GameObject("GameMgr").AddComponent<GameMgr>().Init(room);
        });
    }

    public override void OnDelete()
    {
        if (room != null)
        {
            Deinit();
            room.Dispose();
            room = null;
        }
    }
    private void OnRemovePlayerInfo(Guid id)
    {
        lock (updates)
            lock (removes)
            {
                updates.Remove(id);
                removes.Add(id);
            }
    }
    private void OnUpdatePlayerInfo(RoomInfo.MemberInfo info)
    {
        lock (updates) updates[info.player.id] = info;
    }
    public void RefreshBtnsState()
    {
        if (server != null)
        {
            btnStart.gameObject.SetActive(true);
            lock (room.Info.members)
                btnStart.interactable = room.Info.members.FindIndex(v => !v.ready) < 0;
            btnReady.gameObject.SetActive(false);
            btnCancel.gameObject.SetActive(false);
            btnExit.gameObject.SetActive(false);
            btnDissolve.gameObject.SetActive(true);
        }
        else if (client != null)
        {
            lock (room.Info.members)
            {
                var selfIdx = room.Info.members.FindIndex(v => PlayerInfo.Local == v);
                if (selfIdx < 0) OnExitClick();
                else
                {
                    var self = room.Info.members[selfIdx];
                    btnStart.gameObject.SetActive(false);
                    btnReady.gameObject.SetActive(!self.ready);
                    btnCancel.gameObject.SetActive(self.ready);
                    btnExit.gameObject.SetActive(true);
                    btnExit.interactable = !self.ready;
                    btnReady.gameObject.SetActive(false);
                }
            }
        }
    }
    private void OnRecvMsg(PlayerInfo player, string msg)
    {
        Do(() =>
        {
            var prt = (RectTransform)msgContent.parent;
            var prect = prt.rect;
            var roll = msgContent.rect.height > prect.height && msgContent.anchoredPosition.y > msgContent.rect.height - prect.height - 48;
            Instantiate(msgItem, msgContent).GetComponent<ActivityRoomItemMsg>().SetMsg(player.headIcon, player.name, msg);
            if (roll)
            {
                var ap = msgContent.anchoredPosition;
                ap.y = msgContent.rect.height - prect.height;
                msgContent.anchoredPosition = ap;
            }
        });
    }
    private ActivityRoomItemPlayer CreateMemberInfo()
    {
        var result = pool.Count > 0 ? pool.Pop() : Instantiate(playerItem, playerContent).GetComponent<ActivityRoomItemPlayer>();
        result.gameObject.SetActive(true);
        result.transform.SetAsLastSibling();
        return result;
    }
    private void UpdateMemberInfo()
    {
        foreach (var member in members)
        {
            member.gameObject.SetActive(false);
            pool.Push(member);
        }
        members.Clear();
        lock (room.Info.members)
        {
            for (int i = 0; i < room.Info.members.Count; i++)
            {
                var member = CreateMemberInfo();
                members.Add(member);
                member.UpdateInfo(room.Info.members[i]);
            }
        }
    }
    public void OnSendMsgClick()
    {
        var msg = input.text;
        if (!string.IsNullOrEmpty(msg))
        {
            room.Send(msg);
            input.text = "";
        }
    }
    public void OnStartClick()
    {
        if (server != null) server.StartGame();
    }
    public void OnReadyClick()
    {
        client?.Ready(true);
    }
    public void OnCancelClick()
    {
        client?.Ready(false);
    }
    public void OnExitClick()
    {
        try
        {
            client?.Exit();
        }
        finally
        {
            Do(BackHall);
        }
    }
    public void OnDissolveClick()
    {
        try
        {
            server?.Dissolve();
        }
        finally
        {
            Do(BackHall);
        }
    }
    private void BackHall()
    {
        UIManager.CloseAll();
        Show("Hall");
    }
}
