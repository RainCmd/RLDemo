using UnityEngine;
using UnityEngine.UI;

public class ActivityHallLocalPlayerInfo : MonoBehaviour
{
    public Image icon;
    public Text playerName;
    private void Start()
    {
        PlayerInfo.OnLocalInfoChange += Refresh;
        Refresh(PlayerInfo.Local);
    }
    private void Refresh(PlayerInfo info)
    {
        icon.sprite = Config.PlayerHeadIconList[info.headIcon];
        playerName.text = info.name;
    }
    private void OnDestroy()
    {
        PlayerInfo.OnLocalInfoChange -= Refresh;
    }
}
