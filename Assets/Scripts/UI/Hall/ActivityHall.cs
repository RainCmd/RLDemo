using System;
using System.Collections.Generic;
using UnityEngine;

public class ActivityHall : UIActivity
{
    public RectTransform roomContent;
    public GameObject roomItem;
    private List<ActivityHallItem> items;
    private Stack<ActivityHallItem> pool;
    private Hall hall;
    private Dictionary<Guid, RoomSummaryInfo> updates;
    private HashSet<Guid> removes;
    private void Awake()
    {
        items = new List<ActivityHallItem>();
        pool = new Stack<ActivityHallItem>();
        updates = new Dictionary<Guid, RoomSummaryInfo>();
        removes = new HashSet<Guid>();
    }
    private void Update()
    {
        lock (removes)
        {
            items.RemoveAll(info =>
            {
                if (!removes.Contains(info.info.id)) return false;
                info.gameObject.SetActive(false);
                pool.Push(info);
                return true;
            });
            removes.Clear();
        }
        lock (updates)
        {
            foreach (var info in updates.Values)
            {
                var room = items.Find(v => v.info == info);
                if (!room)
                {
                    room = CreateItem();
                    items.Add(room);
                }
                room.UpdateInfo(info);
            }
            updates.Clear();
        }
    }
    public override void OnCreate()
    {
        hall = new Hall();
        hall.Update += UpdateRoomInfo;
        hall.Remove += RemoveRoomInfo;
        hall.Join += JoinRoom;
    }
    public override void OnDelete()
    {
        hall.Join -= JoinRoom;
        hall.Remove -= RemoveRoomInfo;
        hall.Update -= UpdateRoomInfo;
        hall.Dispose();
        hall = null;
    }
    private ActivityHallItem CreateItem()
    {
        if (pool.Count > 0)
        {
            var result = pool.Pop();
            result.gameObject.SetActive(true);
            result.transform.SetAsFirstSibling();
            return result;
        }
        else
        {
            var result = Instantiate(roomItem, roomContent).GetComponent<ActivityHallItem>();
            result.gameObject.SetActive(true);
            result.transform.SetAsFirstSibling();
            result.Init(OnClickJoinRoom);
            return result;
        }
    }
    private void JoinRoom(RoomInfo info)
    {
        try
        {
            var client = new RoomClient(info);
            Do(() =>
            {
                UIManager.CloseAll();
                Show<ActivityRoom>("Room").Init(client);
            });
        }
        catch (Exception e)
        {
            GameLog.Show(Color.red, e.Message);
        }
    }
    private void OnClickJoinRoom(RoomSummaryInfo info)
    {
        hall.JoinRoom(info);
    }
    private void UpdateRoomInfo(RoomSummaryInfo info)
    {
        lock (updates) updates[info.id] = info;
    }
    private void RemoveRoomInfo(Guid id)
    {
        lock (updates)
            lock (removes)
            {
                updates.Remove(id);
                removes.Add(id);
            }
    }
    public void OnClickSelfInfo()
    {
        Show("PlayerInfo");
    }
    public void OnClickCreateRoom()
    {
        Show("CreateRoom");
    }
}
