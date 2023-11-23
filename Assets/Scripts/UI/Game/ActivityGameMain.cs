using UnityEngine;
using UnityEngine.UI;

public class ActivityGameMain : UIActivity
{
    public ActivityGameMainFloatInfoPanel floatInfoPanel;
    public Text lifeText;
    public Text manaText;
    public Text atkCDText;
    public Text costText;
    public ActivityGameMainBuff buffPanel;
    public ActivityGameMainRocker moveRocker;
    public ActivityGameMainRocker atkRocker;
    public Image exitProgress;
    public Image[] wands;
    public Sprite[] wandsOff;
    public Sprite[] wandsOn;
    public GameMgr Manager { get; private set; }
    public void Init(GameMgr manager)
    {
        Manager = manager;
        manager.Renderer.OnLateUpdate += UpdateFloatPanel;
        manager.Renderer.OnCreateGameEntity += floatInfoPanel.CreateInfo;
        manager.Renderer.OnDestroyGameEntity += floatInfoPanel.RemoveInfo;
        manager.Renderer.OnCreateFloatText += floatInfoPanel.ShowFloatText;
    }
    public override void OnDelete()
    {
        Manager.Renderer.OnCreateFloatText -= floatInfoPanel.ShowFloatText;
        Manager.Renderer.OnDestroyGameEntity -= floatInfoPanel.RemoveInfo;
        Manager.Renderer.OnCreateGameEntity -= floatInfoPanel.CreateInfo;
        Manager.Renderer.OnLateUpdate -= UpdateFloatPanel;
    }
    private void UpdateFloatPanel()
    {
        if (gameObject.activeSelf) floatInfoPanel.UpdatePanel();
    }
    public void OnWandClick(int index)
    {
        for (int i = 0; i < wands.Length; i++)
            wands[i].sprite = i == index ? wandsOn[i] : wandsOff[i];
    }
    public void OnBuildClick()
    {

    }
    public void OnExitClick()
    {
        if (exitProgress.fillAmount > 0)
        {
            DestroyImmediate(Manager);
            Manager = null;
        }
        else exitProgress.fillAmount = 1;
    }
    private void Update()
    {
        if (exitProgress.fillAmount > 0) exitProgress.fillAmount -= Time.deltaTime;
    }
}
