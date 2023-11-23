using UnityEngine;
using UnityEngine.UI;

public class ActivityRoomItemMsg : MonoBehaviour
{
    public Image icon;
    public Text msg;
    public void SetMsg(int icon, string name, string msg)
    {
        gameObject.SetActive(true);
        this.icon.sprite = Config.PlayerHeadIconList[icon];
        this.msg.text = string.Format("    <color=#ffcc00>{0}:</color>{1}", name, msg);
        UpdateSize();
    }
    private RectTransform rt;
    [ContextMenu("UpdateSize")]
    private void UpdateSize()
    {
        if (!rt) rt = transform as RectTransform;
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, msg.preferredHeight);
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }
}
