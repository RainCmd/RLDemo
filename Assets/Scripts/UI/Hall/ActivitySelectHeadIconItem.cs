using System;
using UnityEngine;
using UnityEngine.UI;

public class ActivitySelectHeadIconItem : MonoBehaviour
{
    public Image icon;
    public Image select;
    private Action<int> onClick;
    private int headIcon;
    public bool Select
    {
        get { return select.enabled; }
        set { select.enabled = value; }
    }
    public void Init(int headIcon, Action<int> onClick)
    {
        this.headIcon = headIcon;
        this.onClick = onClick;
        icon.sprite = Config.PlayerHeadIconList[headIcon];
    }
    public void OnClick()
    {
        onClick?.Invoke(headIcon);
    }
}
