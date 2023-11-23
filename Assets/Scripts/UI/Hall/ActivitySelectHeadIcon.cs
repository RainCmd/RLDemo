using System.Collections.Generic;
using UnityEngine;

public class ActivitySelectHeadIcon : UIActivity
{
    public RectTransform content;
    public GameObject iconPrefab;
    private int current;
    private ActivitySetPlayerInfo info;
    private List<ActivitySelectHeadIconItem> icons = new List<ActivitySelectHeadIconItem>();
    public void Init(ActivitySetPlayerInfo spi, int headIcon)
    {
        info = spi;
        current = headIcon;
        var icons = Config.PlayerHeadIconList;
        for (int i = 0; i < icons.Count; i++)
        {
            var icon = Instantiate(iconPrefab, content).GetComponent<ActivitySelectHeadIconItem>();
            this.icons.Add(icon);
            icon.Init(i, OnSelect);
            icon.gameObject.SetActive(true);
        }
        OnSelect(current);
    }
    private void OnSelect(int headIcon)
    {
        current = headIcon;
        for (int i = 0; i < icons.Count; i++)
        {
            icons[i].Select = headIcon == i;
        }
    }
    public void OnOKClick()
    {
        info.SetHeadIcon(current);
        Close();
    }
}
