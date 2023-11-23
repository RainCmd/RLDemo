using UnityEngine.UI;

public class ActivitySetPlayerInfo : UIActivity
{
    public InputField input;
    public Image headIcon;
    private int GetHeadIcon()
    {
        return Config.PlayerHeadIconList.IndexOf(headIcon.sprite);
    }
    public void SetHeadIcon(int headIcon)
    {
        this.headIcon.sprite = Config.PlayerHeadIconList[headIcon];
    }
    public override void OnCreate()
    {
        input.text = PlayerInfo.Local.name;
        SetHeadIcon(PlayerInfo.Local.headIcon);
    }
    public void OnHeadIconClick()
    {
        var select = Show<ActivitySelectHeadIcon>("SelectHeadIcon");
        if (select) select.Init(this, GetHeadIcon());
    }
    public void OnOKClick()
    {
        PlayerInfo.Local = new PlayerInfo(PlayerInfo.Local.id, GetHeadIcon(), input.text);
        Close();
    }
}
