using UnityEngine;
using UnityEngine.UI;

public class ActivityGameMain : UIActivity
{
    public ActivityGameMainFloatInfoPanel floatInfoPanel;
    public GameObject heroInfoPanel;
    public Text lifeText;
    public Text manaText;
    public ActivityGameMainBuff buffPanel;
    public ActivityGameMainRocker moveRocker;
    public ActivityGameMainRocker atkRocker;
    public ActivityGameMainPickList pickList;
    public Image exitProgress;
    public Image[] wands;
    public Sprite[] wandsOff;
    public Sprite[] wandsOn;
    public Image[] wandCDs;
    private LogicTimeSpan[] wandTimes = new LogicTimeSpan[3];
    public GameMgr Manager { get; private set; }
    private PlayerData localPlayerData;
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
        manager.Renderer.OnLateUpdate += OnLateUpdate;

        floatInfoPanel.Init(manager);

        if (manager.Renderer.playerDataManager.TryGet(manager.Renderer.playerDataManager.localPlayer, out localPlayerData))
        {
            localPlayerData.WandChanged += OnWandChanged;
            OnWandChanged();
            OnWandClick((int)localPlayerData.wand);
            localPlayerData.WandCDChanged += OnWandCDChanged;
            for (var i = 0; i < localPlayerData.wandCDs.Length; i++) OnWandCDChanged(i);
            localPlayerData.HeroChanged += OnLocalHeroChanged;
            OnLocalHeroChanged();
        }

        pickList.Init(manager);

    }

    private void OnWandChanged()
    {
        if (localPlayerData != null)
        {
            for (int i = 0; i < wands.Length; i++)
                wands[i].sprite = i == localPlayerData.wand ? wandsOn[i] : wandsOff[i];
        }
    }

    public override void OnDelete()
    {
        if (localPlayerData != null)
        {
            localPlayerData.HeroChanged -= OnLocalHeroChanged;
            localPlayerData.WandCDChanged -= OnWandCDChanged;
            localPlayerData.WandChanged -= OnWandChanged;
            localPlayerData = null;
        }

        pickList.UnInit();

        floatInfoPanel.Deinit();
        Manager.Renderer.OnLateUpdate -= OnLateUpdate;
    }
    private void OnLocalHeroChanged()
    {
        if (localHero != null)
        {
            localHero.OnManaStateChanged -= OnManaStateChanged;
            localHero.OnLifeStateChanged -= OnLifeStateChanged;
        }
        localHero = null;
        if (localPlayerData != null && Manager.Renderer.TryGetUnit(localPlayerData.hero, out localHero))
        {
            heroInfoPanel.SetActive(true);
            localHero.OnLifeStateChanged += OnLifeStateChanged;
            OnLifeStateChanged(localHero.Life);
            localHero.OnManaStateChanged += OnManaStateChanged;
            OnManaStateChanged(localHero.Mana);
        }
        else
        {
            heroInfoPanel.SetActive(false);
        }
    }

    private void OnLifeStateChanged(GameUnitState state)
    {
        lifeText.text = state.ToString();
    }
    private void OnManaStateChanged(GameUnitState state)
    {
        manaText.text = state.ToString();
    }

    private void OnLateUpdate()
    {
        if (localHero != null)
        {
            var mgr = Manager.CameraMgr;
            mgr.Target = localHero.entity.Position;
        }
        if (gameObject.activeSelf) floatInfoPanel.UpdatePanel();
    }
    public void OnWandClick(int index)
    {
        Manager.Room?.UpdateOperator(Operator.SwitchWeapon(index));
    }
    private void OnWandCDChanged(long wand)
    {
        if (localPlayerData != null)
            wandTimes[wand] = localPlayerData.wandCDs[wand];
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
            Close();
        }
        else exitProgress.fillAmount = 1;
    }
    private void Update()
    {
        if (exitProgress.fillAmount > 0) exitProgress.fillAmount -= Time.deltaTime;
        var curr = Manager.Logic?.LogicTime;
        for (int i = 0; i < 3; i++)
        {
            var img = wandCDs[i];
            var cd = wandTimes[i];
            if (cd.end > curr)
            {
                img.gameObject.SetActive(true);
                if (cd.start > curr) img.fillAmount = 1;
                else img.fillAmount = 1 - (float)((curr - cd.start) / (cd.end - cd.start));
            }
            else
            {
                img.gameObject.SetActive(false);
            }
        }
    }
}
