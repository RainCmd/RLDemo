using System;
using UnityEngine;
using UnityEngine.UI;

public class ActivityHallItem : MonoBehaviour
{
    public Image icon;
    public Text roomName;
    public Text playerCount;
    public Text delay;
    public RoomSummaryInfo info;
    private Action<RoomSummaryInfo> onJoin;
    public void Init(Action<RoomSummaryInfo> onJoin)
    {
        this.onJoin = onJoin;
    }
    public void UpdateInfo(RoomSummaryInfo info)
    {
        this.info = info;
        icon.sprite = Config.PlayerHeadIconList[info.icon];
        roomName.text = info.name;
        playerCount.text = info.players.ToString();
        delay.text = string.Format("{0}ms", info.delay);
        delay.color = Tool.GetDelayColor(info.delay);
    }
    public void OnJoinClick()
    {
        onJoin?.Invoke(info);
    }
}
