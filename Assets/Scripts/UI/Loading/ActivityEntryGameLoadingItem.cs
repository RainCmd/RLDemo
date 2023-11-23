using UnityEngine;
using UnityEngine.UI;

public class ActivityEntryGameLoadingItem : MonoBehaviour
{
    public Image progress;
    public CircleIcon icon;
    public Text playerName;
    public Text delay;
    public RoomInfo.MemberInfo member;
    private float targetProgress = 0;
    public void Init(RoomInfo.MemberInfo member)
    {
        icon.sprite = Config.PlayerHeadIconList[member.player.headIcon];
        playerName.text = member.player.name;
        UpdateInfo(member);
    }
    public void UpdateInfo(RoomInfo.MemberInfo info)
    {
        member = info;
        delay.text = string.Format("{0}ms", member.delay);
        delay.color = Tool.GetDelayColor(member.delay);
        icon.Grayscale = info.ready ? 0 : 1;
    }
    public void UpdateProgress(float progress)
    {
        targetProgress = Mathf.Max(targetProgress, progress);
    }
    public void OnLeave()
    {
        icon.Grayscale = 1;
        delay.gameObject.SetActive(false);
        targetProgress = 0;
    }
    private void Update()
    {
        progress.fillAmount = Mathf.Lerp(progress.fillAmount, targetProgress, .1f);
    }
}
