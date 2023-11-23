using System;
using UnityEngine;

[Serializable]
public struct GameEntityState
{
    public float cur;
    public float max;
    public bool Full => cur >= max;
    public GameEntityState(float cur, float max)
    {
        this.cur = cur;
        this.max = max;
    }
    public override bool Equals(object obj)
    {
        return obj is GameEntityState state &&
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
public class GameEntity
{
    public event Action OnOwnerChanged;
    public long id;
    public long owner;
    private Vector3 forward;
    private Vector3 trgForward;
    private Vector3 position;
    private Vector3 trgPosition;
    private float trgTime;
    public Vector3 Position
    {
        get
        {
            return position;
        }
    }
    public void UpdateTransform(Vector3 forward, Vector3 position, bool immediately)
    {
        trgForward = forward;
        trgPosition = position;
        trgTime = Time.time + 1f / Config.LFPS;
        if (immediately) ImmediatelyTransform();
    }
    private void ImmediatelyTransform()
    {
        forward = trgForward;
        position = trgPosition;
        trgTime = Time.time;
    }
    public void UpdateMove(float deltaTime)
    {
        if (trgTime > Time.time)
        {
            var t = deltaTime / (deltaTime + trgTime - Time.time);
            forward = Vector3.Lerp(forward, trgForward, t);
            position = Vector3.Lerp(position, trgPosition, t);
        }
    }
    public virtual void PlayAnimation(string animation)
    {

    }
    public void OnRemove(bool immediately)
    {

    }
}
public class GameUnit
{
    public GameEntity entity;
    public event Action<GameEntityState> OnLifeStateChanged, OnManaStateChanged;
    private GameEntityState life, mana;
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
    public GameEntityState Life
    {
        get { return life; }
        set
        {
            var changed = !life.Equals(value);
            life = value;
            if (changed) OnLifeStateChanged?.Invoke(value);
        }
    }
    public GameEntityState Mana
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
        life = new GameEntityState((float)unit.hp, (float)unit.maxHP);
        mana = new GameEntityState((float)unit.mp, (float)unit.maxMP);
    }
    public void OnRemove()
    {

    }
}
