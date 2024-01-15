using UnityEngine;

public class ActivityGameMainNpcFloatInfo : MonoBehaviour
{
    private RectTransform rt;
    public StateBar lifebar, manabar;
    public RectTransform RectTransform { get { return rt; } }
    public GameMgr Manager { get; private set; }
    public GameUnit Unit { get; private set; }
    private void Awake()
    {
        rt = transform as RectTransform;
    }
    public virtual void Init(GameMgr manager, GameUnit entity)
    {
        Manager = manager;
        Unit = entity;
        lifebar.State = entity.Life;
        manabar.State = entity.Mana;
        entity.OnLifeStateChanged += OnLifeStateChanged;
        entity.OnManaStateChanged += OnManaStateChanged;
        switch (entity.UnitType)
        {
            case UnitType.Player: break;
            case UnitType.Npc:
                {
                    manabar.gameObject.SetActive(true);
                    var sd = rt.sizeDelta;
                    sd.y = 8;
                    rt.sizeDelta = sd;
                }
                break;
            case UnitType.NpcNoMana:
                {
                    manabar.gameObject.SetActive(false);
                    var sd = rt.sizeDelta;
                    sd.y = 4;
                    rt.sizeDelta = sd;
                }
                break;
        }
    }
    private void OnLifeStateChanged(GameUnitState state)
    {
        lifebar.State = state;
    }
    private void OnManaStateChanged(GameUnitState state)
    {
        manabar.State = state;
    }
    public virtual void Deinit()
    {
        Unit.OnManaStateChanged -= OnManaStateChanged;
        Unit.OnLifeStateChanged -= OnLifeStateChanged;
        Unit = null;
    }
}
