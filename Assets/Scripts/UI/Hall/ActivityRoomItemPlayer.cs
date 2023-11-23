using UnityEngine;
using UnityEngine.UI;

public class ActivityRoomItemPlayer : MonoBehaviour
{
    public RoomInfo.MemberInfo info;
    public CircleIcon icon;
    public Text playerName;
    public Text delay;
    public GameObject ready;
    public void UpdateInfo(PlayerInfo info)
    {
        icon.sprite = Config.PlayerHeadIconList[info.headIcon];
        playerName.text = info.name;
        delay.gameObject.SetActive(false);
        ready.SetActive(false);
    }
    public void UpdateInfo(RoomInfo.MemberInfo info)
    {
        this.info = info;
        icon.sprite = Config.PlayerHeadIconList[info.player.headIcon];
        playerName.text = info.player.name;
        delay.gameObject.SetActive(true);
        delay.text = string.Format("{0}ms", info.delay);
        delay.color = Tool.GetDelayColor(info.delay);
        ready.SetActive(info.ready);
    }
}
