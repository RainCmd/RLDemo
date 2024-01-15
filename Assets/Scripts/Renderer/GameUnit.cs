using System;
using UnityEngine;

[Serializable]
public struct GameUnitState
{
    public float cur;
    public float max;
    public bool Full => cur >= max;
    public GameUnitState(float cur, float max)
    {
        this.cur = cur;
        this.max = max;
    }
    public override bool Equals(object obj)
    {
        return obj is GameUnitState state &&
               cur == state.cur &&
               max == state.max;
    }
    public override int GetHashCode()
    {
        int hashCode = 1600082838;
        hashCode = hashCode * -1521134295 + cur.GetHashCode();
        hashCode = hashCode * -1521134295 + max.GetHashCode();
        return hashCode;
    }
}
public class GameUnit
{
    public GameEntity entity;
    public event Action<GameUnitState> OnLifeStateChanged, OnManaStateChanged;
    private GameUnitState life, mana;
    public UnitType UnitType { get; private set; }
    public virtual bool VisableFloatInfo
    {
        get
        {
            switch (UnitType)
            {
                case UnitType.Player: return true;
                case UnitType.Npc: return !life.Full || !Mana.Full;
                case UnitType.NpcNoMana: return !life.Full;
            }
            return false;
        }
    }
    public virtual Vector3 FloatInfoPosition
    {
        get
        {
            return entity.Position + Vector3.up;
        }
    }
    public GameUnitState Life
    {
        get { return life; }
        set
        {
            var changed = !life.Equals(value);
            life = value;
            if (changed) OnLifeStateChanged?.Invoke(value);
        }
    }
    public GameUnitState Mana
    {
        get { return mana; }
        set
        {
            var changed = !mana.Equals(value);
            mana = value;
            if (changed) OnManaStateChanged?.Invoke(value);
        }
    }
    public void Init(GameEntity entity, LogicUnitEntity unit)
    {
        this.entity = entity;
        UnitType = unit.type;
        life = new GameUnitState((float)unit.hp, (float)unit.maxHP);
        mana = new GameUnitState((float)unit.mp, (float)unit.maxMP);
    }
    public void OnRemove()
    {

    }
}
