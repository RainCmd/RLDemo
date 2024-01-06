using System;
using System.Collections.Generic;
using RainLanguage;

public enum OperatorType
{
    Rocker,
    Fire,
    StopFire,
    SwitchWeapon,
    Pick,
    Drop,
    Equip,
}
public struct Operator
{
    public OperatorType type;
    public Real direction;
    public Real distance;
    public long target;
    public int weapon;
    public int slot;
    public Operator(OperatorType type) : this() { this.type = type; }
    public static Operator Rocker(Real direction, Real distance) { return new Operator(OperatorType.Rocker) { direction = direction, distance = distance }; }
    public static Operator Fire(Real direction) { return new Operator(OperatorType.Fire) { direction = direction }; }
    public static Operator StopFire() { return new Operator(OperatorType.StopFire); }
    public static Operator SwitchWeapon(int weapon) { return new Operator(OperatorType.SwitchWeapon) { weapon = weapon }; }
    public static Operator Pick(long target) { return new Operator(OperatorType.Pick) { target = target }; }
    public static Operator Drop(long target) { return new Operator(OperatorType.Drop) { target = target }; }
    public static Operator Equip(long target, int weapon, int slot) { return new Operator(OperatorType.Equip) { target = target, weapon = weapon, slot = slot }; }
}
public struct PlayerOperator
{
    public int ctrlId;
    public Operator oper;
    public PlayerOperator(int ctrlId, Operator oper)
    {
        this.ctrlId = ctrlId;
        this.oper = oper;
    }
}
public struct FrameOperator
{
    public long frame;
    public List<PlayerOperator> operators;
    public FrameOperator(long frame, List<PlayerOperator> operators)
    {
        this.frame = frame;
        this.operators = operators;
    }
}
public enum RoomState
{
    Ready,
    Loading,
    Game,
    Invalid,
}
public interface IRoom : IDisposable
{
    event Action<PlayerInfo, string> OnRecvMsg;
    event Action<RoomInfo.MemberInfo> OnUpdateMember;
    event Action<Guid> OnRemovePlayerInfo;
    event Action OnUnexpectedExit;
    event Action OnGameStart;
    event Action<Guid, float> OnUpdatePlayerLoading;
    event Action OnEntryGame;
    RoomState State { get; }
    RoomInfo Info { get; }
    long Frame { get; }
    long OverstockFrame { get; }
    void Send(string msg);
    void UpdateLoading(float progress);
    void UpdateOperator(Operator oper);
    List<PlayerOperator> EntryNextFrame();
    void Recyle(List<PlayerOperator> operators);
}
