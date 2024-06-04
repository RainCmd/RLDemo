using System;
using UnityEngine;
using UnityEngine.UI;

public class ActivityGameMain : UIActivity
{
    public ActivityGameMainFloatInfoPanel floatInfoPanel;
    public GameObject heroInfoPanel;
    public Text lifeText;
    public Text manaText;
    public Text atkCDText;
    public Text costText;
    public ActivityGameMainBuff buffPanel;
    public ActivityGameMainRocker moveRocker;
    public ActivityGameMainRocker atkRocker;
    public ActivityGameMainPickList pickList;
    public Image exitProgress;
    public Image[] wands;
    public Sprite[] wandsOff;
    public Sprite[] wandsOn;
    public GameMgr Manager { get; private set; }
    private GameUnit localHero;
    private void Start()
    {
        moveRocker.OnRock += MoveRocker_OnRock;
        atkRocker.OnRock += AtkRocker_OnRock;
    }

    private void AtkRocker_OnRock(float angle, float radius)
    {
        if (radius > 0) Manager.Room.UpdateOperator(Operator.Fire((RainLanguage.Real)angle));
        else Manager.Room.UpdateOperator(Operator.StopFire());
    }

    private void MoveRocker_OnRock(float angle, float radius)
    {
        Manager.Room.UpdateOperator(Operator.Rocker((RainLanguage.Real)angle, (RainLanguage.Real)radius));
    }

    public void Init(GameMgr manager)
    {
        Manager = manager;
        manager.Renderer.OnLateUpdate += UpdateFloatPanel;
        manager.Renderer.OnCreateGameEntity += floatInfoPanel.CreateInfo;
        manager.Renderer.OnDestroyGameEntity += floatInfoPanel.RemoveInfo;
        manager.Renderer.OnCreateFloatText += floatInfoPanel.ShowFloatText;

        manager.Renderer.OnWandUpdate += OnWandClick;
        OnWandClick(manager.Renderer.playerWand);
        manager.Renderer.OnWandCDChanged += OnWandCDChanged;
        for (int i = 0; i < 3; i++) OnWandCDChanged(i);
        pickList.Init(manager);

        manager.Renderer.OnLocalHeroChanged += Renderer_OnLocalHeroChanged;
        Renderer_OnLocalHeroChanged();
    }

    public override void OnDelete()
    {
        if (Manager.Renderer == null) return;
        Manager.Renderer.OnLocalHeroChanged -= Renderer_OnLocalHeroChanged;

        pickList.UnInit();
        Manager.Renderer.OnWandCDChanged -= OnWandCDChanged;
        Manager.Renderer.OnWandUpdate -= OnWandClick;

        Manager.Renderer.OnCreateFloatText -= floatInfoPanel.ShowFloatText;
        Manager.Renderer.OnDestroyGameEntity -= floatInfoPanel.RemoveInfo;
        Manager.Renderer.OnCreateGameEntity -= floatInfoPanel.CreateInfo;
        Manager.Renderer.OnLateUpdate -= UpdateFloatPanel;
    }
    private void Renderer_OnLocalHeroChanged()
    {
        if (localHero != null)
        {
            localHero.OnManaStateChanged -= LocalHero_OnManaStateChanged;
            localHero.OnLifeStateChanged -= LocalHero_OnLifeStateChanged;
        }
        localHero = null;
        if (Manager.Renderer.TryGetUnit(Manager.Renderer.LocalHero, out localHero))
        {
            heroInfoPanel.SetActive(true);
            localHero.OnLifeStateChanged += LocalHero_OnLifeStateChanged;
            LocalHero_OnLifeStateChanged(localHero.Life);
            localHero.OnManaStateChanged += LocalHero_OnManaStateChanged;
            LocalHero_OnManaStateChanged(localHero.Mana);
        }
        else
        {
            heroInfoPanel.SetActive(false);
        }
    }

    private void LocalHero_OnLifeStateChanged(GameUnitState state)
    {
        lifeText.text = state.ToString();
    }
    private void LocalHero_OnManaStateChanged(GameUnitState state)
    {
        manaText.text = state.ToString();
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
    private void OnWandCDChanged(int wand)
    {
        var cd = Manager.Renderer.wandCDs[wand];
    }
    public void OnBuildClick()
    {
        UIManager.Show<ActivityWorkbench>("Workbench").Init(Manager);
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
