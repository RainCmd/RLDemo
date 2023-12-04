using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using RainLanguage;
using RainLib = RainLanguage.RainLanguageAdapter.RainLibrary;
using Kernel = RainLanguage.RainLanguageAdapter.RainKernel;
using Function = RainLanguage.RainLanguageAdapter.RainFunction;

public struct CtrlInfo
{
    public long ctrlId;
    public string name;
    public CtrlInfo(long ctrlId, string name)
    {
        this.ctrlId = ctrlId;
        this.name = name;
    }
}
public struct LogicEntity
{
    public long id;
    public long owner;
    public string resource;
    public string anim;
    public Real3 forward;
    public Real3 position;
    public LogicEntity(long id, long owner, string resource, string anim, Real3 forward, Real3 position)
    {
        this.id = id;
        this.owner = owner;
        this.resource = resource;
        this.anim = anim;
        this.forward = forward;
        this.position = position;
    }
}
public struct LogicUnitEntity
{
    public long id;
    public UnitType type;
    public Real hp;
    public Real maxHP;
    public Real mp;
    public Real maxMP;
    public LogicUnitEntity(long id, UnitType type, Real hp, Real maxHP, Real mp, Real maxMP)
    {
        this.id = id;
        this.type = type;
        this.hp = hp;
        this.maxHP = maxHP;
        this.mp = mp;
        this.maxMP = maxMP;
    }
}
public struct LogicTimeSpan
{
    public Real start;
    public Real end;
    public LogicTimeSpan(Real start, Real end)
    {
        this.start = start;
        this.end = end;
    }
}
public struct LogicBuffEntity
{
    public long id;
    public long icon;
    public long numer;
    public LogicTimeSpan time;
    public LogicBuffEntity(long id, long icon, long numer, LogicTimeSpan time)
    {
        this.id = id;
        this.icon = icon;
        this.numer = numer;
        this.time = time;
    }
}
public struct LogicMagicNodeEntity
{
    public long id;
    public long nodeID;
    public long number;
    public LogicMagicNodeEntity(long id, long nodeID, long number)
    {
        this.id = id;
        this.nodeID = nodeID;
        this.number = number;
    }
}
public struct LogicWand
{
    public LogicTimeSpan cd;
    public readonly List<long> nodes;
    public LogicWand(List<long> nodes)
    {
        cd = new LogicTimeSpan(0, 0);
        this.nodes = nodes;
    }
}
public struct LogicPlayerEntity
{
    public long playerId;
    public long ctrlId;
    public string name;
    public readonly List<long> buffs;
    public readonly List<long> bag;
    public LogicWand[] wands;
    public LogicPlayerEntity(long playerId, long ctrlId, string name)
    {
        this.playerId = playerId;
        this.ctrlId = ctrlId;
        this.name = name;
        buffs = new List<long>();
        bag = new List<long>();
        wands = new LogicWand[3];
        for (int i = 0; i < wands.Length; i++) wands[i] = new LogicWand(new List<long>());
    }
}
public struct LogicFloatTextMsg
{
    public Real3 position;
    public Real3 color;
    public string text;
    public LogicFloatTextMsg(Real3 position, Real3 color, string text)
    {
        this.position = position;
        this.color = color;
        this.text = text;
    }
}
public struct LogicEffectMsg
{
    public Real3 position;
    public Real3 forward;
    public string resource;
    public LogicEffectMsg(Real3 position, Real3 forward, string resource)
    {
        this.position = position;
        this.forward = forward;
        this.resource = resource;
    }
}
public class LogicWorld : IDisposable
{
    private readonly Queue<string> messages = new Queue<string>();
    public readonly GameMgr mgr;
    private readonly Queue<IDisposable> disposables = new Queue<IDisposable>();
    private Kernel kernel;
    private Function[] operFuncs;
    public event Action<LogicEntity> OnEntityChanged;
    public event Action<long, bool> OnEntityRemoved;
    public event Action<LogicUnitEntity> OnUpdateUnitEntity;
    public event Action<long> OnRemoveUnitEntity;
    public event Action<LogicBuffEntity> OnUpdateBuffEntity;
    public event Action<long> OnRemoveBuffEntity;
    public event Action<LogicMagicNodeEntity> OnUpdateMagicNodeEntity;
    public event Action<long> OnRemoveMagicNodeEntity;
    public event Action<long, long, bool> OnPlayerBuffChanged;
    public event Action<long, long, bool> OnPlayerBagMagicNodeChanged;
    public event Action<long, long, long, long> OnPlayerWandMagicNodeChanged;
    public event Action<long, long, LogicTimeSpan> OnPlayerWandCDUpdate;

    public event Action<LogicFloatTextMsg> OnFloatTextMsg;
    public event Action<LogicEffectMsg> OnEffectMsg;
    public LogicWorld(GameMgr mgr, LoadingProgress loading)
    {
        this.mgr = mgr;
        var info = mgr.Room.Info;

        var asset = Resources.Load<TextAsset>("RainLibraries/RLDemo.lib");
        var lib = RainLib.Create(asset.bytes);
        var libs = new RainLib[] { lib };
        var parameter = new StartupParameter(libs, info.seed, 0xff, 0xff,
            OnReferenceEntity, OnReleaseEntity,
            LoadLibrary, LoadCaller, OnExceptionExit, LoadProgramDatabase);
        kernel = RainLanguageAdapter.CreateKernel(parameter);
        using (var init = kernel.FindFunction("GameMain"))
        using (var invoker = init.CreateInvoker())
            invoker.Start(true, true);
        InitOperFuncs();
        loading.Progress = 1;
    }

    public OnCaller LoadCaller(Kernel kernel, string fullName, RainType[] parameters)
    {
        if (callerMap.TryGetValue(fullName, out CallerHelper helper)) return helper.OnCaller;
        else
        {
            GameLog.Show(Color.red, string.Format("函数：{0} 没有绑定!", fullName));
            return (k, c) => EnMsg("调用了未绑定函数：" + fullName);
        }
    }
    #region NativeFunctions
    private void Debug(string msg)
    {
        GameLog.Show(Color.white, msg);
        EnMsg(msg);
    }
    private long GetCtrlCount()
    {
        return mgr.Room.Info.members.Count + 1;
    }
    private CtrlInfo GetCtrl(long idx)
    {
        if (idx > 0) return new CtrlInfo(idx - 1, mgr.Room.Info.members[(int)idx - 1].player.name);
        else return new CtrlInfo(-1, mgr.Room.Info.owner.name);
    }
    private long Config_GetMagicNodeCount()
    {
        return LogicConfig.magicNodes.Length;
    }
    private ConfigMagicNode Config_GetMagicNode(long index)
    {
        return LogicConfig.magicNodes[index];
    }
    private long Config_GetEntityConfigCount()
    {
        return LogicConfig.entities.Length;
    }
    private ConfigEntity Config_GetEntityConfig(long index)
    {
        return LogicConfig.entities[index];
    }
    private long Config_GetUnitCount()
    {
        return LogicConfig.units.Length;
    }
    private ConfigUnit Config_GetUnit(long index)
    {
        return LogicConfig.units[index];
    }

    private void NativeOnUpdateEntity(long id, long owner, string resource, string anim, Real3 forward, Real3 position)
    {
        OnEntityChanged?.Invoke(new LogicEntity(id, owner, resource, anim, forward, position));
    }
    private void NativeOnRemoveEntity(long id, bool immediately)
    {
        OnEntityRemoved?.Invoke(id, immediately);
    }
    private void NativeOnUpdateUnitEntity(long id, long type, Real hp, Real maxHP, Real mp, Real maxMP)
    {
        OnUpdateUnitEntity?.Invoke(new LogicUnitEntity(id, (UnitType)type, hp, maxHP, mp, maxMP));
    }
    private void NativeOnRemoveUnitEntity(long id)
    {
        OnRemoveUnitEntity?.Invoke(id);
    }
    private void NativeOnUpdateBuffEntity(long id, long icon, long number, Real start, Real end)
    {
        OnUpdateBuffEntity?.Invoke(new LogicBuffEntity(id, icon, number, new LogicTimeSpan(start, end)));
    }
    private void NativeOnRemoveBuffEntity(long id)
    {
        OnRemoveBuffEntity?.Invoke(id);
    }
    private void NativeOnUpdateMagicNodeEntity(long id, long nodeID, long number)
    {
        OnUpdateMagicNodeEntity?.Invoke(new LogicMagicNodeEntity(id, nodeID, number));
    }
    private void NativeOnRemvoeMagicNodeEntity(long id)
    {
        OnRemoveMagicNodeEntity?.Invoke(id);
    }
    private void NativeOnPlayerBuffChanged(long player, long buffID, bool addition)
    {
        OnPlayerBuffChanged?.Invoke(player, buffID, addition);
    }
    private void NativeOnPlayerBagMagicNodeChanged(long player, long nodeID, bool addition)
    {
        OnPlayerBagMagicNodeChanged?.Invoke(player, nodeID, addition);
    }
    private void NativeOnPlayerWandMagicNodeChanged(long player, long wand, long nodeID, long slot)
    {
        OnPlayerWandMagicNodeChanged?.Invoke(player, wand, nodeID, slot);
    }
    private void NativeOnPlayerWandCDUpdate(long player, long wand, Real start, Real end)
    {
        OnPlayerWandCDUpdate?.Invoke(player, wand, new LogicTimeSpan(start, end));
    }

    private void ShowFloatText(Real3 position, Real3 color, string value)
    {
        OnFloatTextMsg?.Invoke(new LogicFloatTextMsg(position, color, value));
    }
    private void ShowEffect(Real3 position, Real3 forward, string resource)
    {
        OnEffectMsg?.Invoke(new LogicEffectMsg(position, forward, resource));
    }
    #endregion
    #region InitFunctions
    public void GetLogicEntities(Action<LogicEntity> action)
    {
        using (var getCount = kernel.FindFunction("Init_GetEntityCount"))
        using (var getCountInvoker = getCount.CreateInvoker())
        {
            getCountInvoker.Start(true, true);
            var count = getCountInvoker.GetIntegerReturnValue(0);
            using (var getEntity = kernel.FindFunction("Init_GetEntity"))
                while (count-- > 0)
                    using (var invoker = getEntity.CreateInvoker())
                    {
                        invoker.SetIntegerParameter(0, count);
                        invoker.Start(true, true);
                        var id = invoker.GetIntegerReturnValue(0);
                        var owner = invoker.GetIntegerReturnValue(1);
                        var resource = invoker.GetStringReturnValue(2);
                        var anim = invoker.GetStringReturnValue(3);
                        var forward = invoker.GetReal3ReturnValue(4);
                        var position = invoker.GetReal3ReturnValue(5);
                        action(new LogicEntity(id, owner, resource, anim, forward, position));
                    }
        }
    }
    public void GetLogicUnits(Action<LogicUnitEntity> action)
    {
        using (var getCount = kernel.FindFunction("Init_GetUnitCount"))
        using (var getCountInvoker = getCount.CreateInvoker())
        {
            getCountInvoker.Start(true, true);
            var count = getCountInvoker.GetIntegerReturnValue(0);
            using (var getEntity = kernel.FindFunction("Init_GetUnit"))
                while (count-- > 0)
                    using (var invoker = getEntity.CreateInvoker())
                    {
                        invoker.SetIntegerParameter(0, count);
                        invoker.Start(true, true);
                        var id = invoker.GetIntegerReturnValue(0);
                        var type = (UnitType)invoker.GetEnumReturnValue(1);
                        var hp = invoker.GetRealReturnValue(2);
                        var maxHP = invoker.GetRealReturnValue(3);
                        var mp = invoker.GetRealReturnValue(4);
                        var maxMP = invoker.GetRealReturnValue(5);
                        action(new LogicUnitEntity(id, type, hp, maxHP, mp, maxMP));
                    }
        }
    }
    public void GetLogicBuffs(Action<LogicBuffEntity> action)
    {
        using (var getCount = kernel.FindFunction("Init_GetBuffCount"))
        using (var getCountInvoker = getCount.CreateInvoker())
        {
            getCountInvoker.Start(true, true);
            var count = getCountInvoker.GetIntegerReturnValue(0);
            using (var getEntity = kernel.FindFunction("Init_GetBuff"))
                while (count-- > 0)
                    using (var invoker = getEntity.CreateInvoker())
                    {
                        invoker.SetIntegerParameter(0, count);
                        invoker.Start(true, true);
                        var id = invoker.GetIntegerReturnValue(0);
                        var icon = invoker.GetIntegerReturnValue(1);
                        var number = invoker.GetIntegerReturnValue(2);
                        var startTime = invoker.GetRealReturnValue(3);
                        var endTime = invoker.GetRealReturnValue(4);
                        action(new LogicBuffEntity(id, icon, number, new LogicTimeSpan(startTime, endTime)));
                    }
        }
    }
    public void GetMagicNodes(Action<LogicMagicNodeEntity> action)
    {
        using (var getCount = kernel.FindFunction("Init_GetMagicNodeCount"))
        using (var getCountInvoker = getCount.CreateInvoker())
        {
            getCountInvoker.Start(true, true);
            var count = getCountInvoker.GetIntegerReturnValue(0);
            using (var getEntity = kernel.FindFunction("Init_GetMagicNode"))
                while (count-- > 0)
                    using (var invoker = getEntity.CreateInvoker())
                    {
                        invoker.SetIntegerParameter(0, count);
                        invoker.Start(true, true);
                        var id = invoker.GetIntegerReturnValue(0);
                        var nodeId = invoker.GetIntegerReturnValue(1);
                        var number = invoker.GetIntegerReturnValue(2);
                        action(new LogicMagicNodeEntity(id, nodeId, number));
                    }
        }
    }
    public LogicPlayerEntity[] GetLogicPlayers()
    {
        using (var function = kernel.FindFunction("Init_GetPlayerCount"))
        using (var playerCountInvoker = function.CreateInvoker())
        {
            playerCountInvoker.Start(true, true);
            var players = new LogicPlayerEntity[playerCountInvoker.GetIntegerReturnValue(0)];
            using (var getPlayer = kernel.FindFunction("Init_GetPlayer"))
            using (var getBuffCount = kernel.FindFunction("Init_GetPlayerBuffCount"))
            using (var getBuff = kernel.FindFunction("Init_GetPlayerBuff"))
            using (var getBagMagicNodeCount = kernel.FindFunction("Init_GetPlayerBagMagicNodeCount"))
            using (var getBagMagicNode = kernel.FindFunction("Init_GetPlayerBagMagicNode"))
            using (var getWandMagicNodeCount = kernel.FindFunction("Init_GetPlayerWandMagicNodeCount"))
            using (var getWandMagicNode = kernel.FindFunction("Init_GetPlayerWandMagicNode"))
                for (var i = 0; i < players.Length; i++)
                {
                    using (var invoker = getPlayer.CreateInvoker())
                    {
                        invoker.SetIntegerParameter(0, i);
                        invoker.Start(true, true);
                        var ctrlId = invoker.GetIntegerReturnValue(0);
                        var name = invoker.GetStringReturnValue(1);
                        players[i] = new LogicPlayerEntity(i, ctrlId, name);
                    }
                    using (var countInvoker = getBuffCount.CreateInvoker())
                    {
                        countInvoker.SetIntegerParameter(0, i);
                        countInvoker.Start(true, true);
                        var count = countInvoker.GetIntegerReturnValue(0);
                        for (int index = 0; index < count; index++)
                            using (var invoker = getBuff.CreateInvoker())
                            {
                                invoker.SetIntegerParameter(0, i);
                                invoker.SetIntegerParameter(1, index);
                                invoker.Start(true, true);
                                players[i].buffs.Add(invoker.GetIntegerReturnValue(0));
                            }
                    }
                    using (var countInvoker = getBagMagicNodeCount.CreateInvoker())
                    {
                        countInvoker.SetIntegerParameter(0, i);
                        countInvoker.Start(true, true);
                        var count = countInvoker.GetIntegerReturnValue(0);
                        for (int index = 0; index < count; index++)
                            using (var invoker = getBagMagicNode.CreateInvoker())
                            {
                                invoker.SetIntegerParameter(0, i);
                                invoker.SetIntegerParameter(1, index);
                                invoker.Start(true, true);
                                players[i].bag.Add(invoker.GetIntegerReturnValue(0));
                            }
                    }
                    for (var wand = 0; wand < players[i].wands.Length; wand++)
                        using (var countInvoker = getWandMagicNodeCount.CreateInvoker())
                        {
                            countInvoker.SetIntegerParameter(0, i);
                            countInvoker.SetIntegerParameter(1, wand);
                            countInvoker.Start(true, true);
                            var count = countInvoker.GetIntegerReturnValue(0);
                            for (int index = 0; index < count; index++)
                                using (var invoker = getWandMagicNode.CreateInvoker())
                                {
                                    invoker.SetIntegerParameter(0, i);
                                    invoker.SetIntegerParameter(1, wand);
                                    invoker.SetIntegerParameter(2, index);
                                    invoker.Start(true, true);
                                    players[i].wands[wand].nodes.Add(invoker.GetIntegerReturnValue(0));
                                }
                        }
                }

            return players;
        }
    }
    #endregion

    private void InitOperFuncs()
    {
        operFuncs = new Function[typeof(OperatorType).GetEnumValues().Length];
        operFuncs[(int)OperatorType.Rocker] = GetFunction("OperRocker");
        operFuncs[(int)OperatorType.Fire] = GetFunction("OperFire");
        operFuncs[(int)OperatorType.StopFire] = GetFunction("OperStopFire");
        operFuncs[(int)OperatorType.SwitchWeapon] = GetFunction("OperSwitchWeapon");
        operFuncs[(int)OperatorType.Pick] = GetFunction("OperPick");
        operFuncs[(int)OperatorType.Drop] = GetFunction("OperDrop");
        operFuncs[(int)OperatorType.Equip] = GetFunction("OperEquip");
    }
    private Function GetFunction(string name)
    {
        var result = kernel.FindFunction(name);
        disposables.Enqueue(result);
        return result;
    }
    private void OnReferenceEntity(Kernel kernel, ulong entity) { }
    private void OnReleaseEntity(Kernel kernel, ulong entity) { }
    private byte[] LoadLibrary(string name)
    {
        var asset = Resources.Load<TextAsset>(string.Format("RainLibraries/{0}.lib", name));
        return asset.bytes;
    }
    private void OnExceptionExit(Kernel kernel, RainStackFrame[] frames, string msg)
    {
        GameLog.Show(Color.red, msg);
        msg = string.Format("<color=#ff0000>{0}</color>", msg);
        foreach (var frame in frames)
            msg += string.Format("\n{0} <color=#00ff00>{1}</color> <color=#ffcc00>0X{2}</color>", frame.funName, frame.libName, frame.address.ToString("X"));
        EnMsg(msg);
    }
    private byte[] LoadProgramDatabase(string name)
    {
        return null;//todo 加载pdb文件，在Asset/RainProgramDatabase下
    }
    private void EnMsg(string msg)
    {
        lock (messages) messages.Enqueue(msg);
    }
    public bool TryDeMsg(out string msg)
    {
        if (messages.Count > 0)
        {
            lock (messages) msg = messages.Dequeue();
            return true;
        }
        msg = null;
        return false;
    }
    public void EntryGame()
    {
        using (var onEntryGame = kernel.FindFunction("GameEntry"))
        using (var onEntryGameInvoker = onEntryGame.CreateInvoker())
            onEntryGameInvoker.Start(true, false);

        new Thread(LogicLoop).Start();
    }
    private void LogicLoop()
    {
        var step = TimeSpan.FromMilliseconds(1000 / Config.LFPS).TotalMilliseconds;
        var start = DateTime.Now;
        while (true)
        {
            var kernel = this.kernel;
            var room = mgr.Room;
            if (kernel == null || room == null) break;
            var operators = room.EntryNextFrame();
            if (operators != null)
            {
                foreach (var op in operators)
                {
                    using (var invoker = operFuncs[(int)op.oper.type].CreateInvoker())
                    {
                        invoker.SetIntegerParameter(0, op.ctrlId);
                        switch (op.oper.type)
                        {
                            case OperatorType.Rocker:
                                invoker.SetRealParameter(1, op.oper.direction);
                                invoker.SetRealParameter(2, op.oper.distance);
                                break;
                            case OperatorType.Fire:
                                invoker.SetRealParameter(1, op.oper.direction);
                                break;
                            case OperatorType.StopFire: break;
                            case OperatorType.SwitchWeapon:
                                invoker.SetIntegerParameter(1, op.oper.weapon);
                                break;
                            case OperatorType.Pick:
                                invoker.SetIntegerParameter(1, op.oper.target);
                                break;
                            case OperatorType.Drop:
                                invoker.SetIntegerParameter(1, op.oper.target);
                                break;
                            case OperatorType.Equip:
                                invoker.SetIntegerParameter(1, op.oper.target);
                                invoker.SetIntegerParameter(2, op.oper.weapon);
                                invoker.SetIntegerParameter(3, op.oper.slot);
                                break;
                        }
                        invoker.Start(true, false);
                    }
                }
                room.Recyle(operators);
                kernel.Update();
                var scale = 1.0;
                if (room.OverstockFrame > 1) scale = 30 / (29 + room.OverstockFrame);
                start = start.AddMilliseconds(step * scale);
                var dt = DateTime.Now - start;
                if (dt.TotalMilliseconds < step) Thread.Sleep((int)(step - dt.TotalMilliseconds));
            }
            else
            {
                start = DateTime.Now;
                Thread.Sleep((int)step);
            }
        }
    }
    public void Dispose()
    {
        while (disposables.Count > 0) disposables.Dequeue().Dispose();
        kernel.Dispose();
        kernel = null;
    }

    private static readonly Dictionary<string, CallerHelper> callerMap = new Dictionary<string, CallerHelper>();
    private static void RegistFunction(string name, string function)
    {
        callerMap.Add(Config.GameName + "." + name, CallerHelper.Create<LogicWorld>(function));
    }
    static LogicWorld()
    {
        RegistFunction("Debug", "Debug");

        RegistFunction("InitGame.GetControlCount", "GetCtrlCount");
        RegistFunction("InitGame.GetControl", "GetCtrl");
        RegistFunction("GameConfig.ConfigMagicNode_GetConfigCount", "Config_GetMagicNodeCount");
        RegistFunction("GameConfig.ConfigMagicNode_GetConfig", "Config_GetMagicNode");
        RegistFunction("GameConfig.ConfigEntity_GetConfigCount", "Config_GetEntityConfigCount");
        RegistFunction("GameConfig.ConfigEntity_GetConfig", "Config_GetEntityConfig");
        RegistFunction("GameConfig.ConfigUnit_GetConfigCount", "Config_GetUnitCount");
        RegistFunction("GameConfig.ConfigUnit_GetConfig", "Config_GetUnit");

        RegistFunction("OnUpdateEntity", "NativeOnUpdateEntity");
        RegistFunction("OnRemoveEntity", "NativeOnRemoveEntity");
        RegistFunction("OnUpdateUnitEntity", "NativeOnUpdateUnitEntity");
        RegistFunction("OnRemoveUnitEntity", "NativeOnRemoveUnitEntity");
        RegistFunction("OnUpdateBuffEntity", "NativeOnUpdateBuffEntity");
        RegistFunction("OnRemoveBuffEntity", "NativeOnRemoveBuffEntity");
        RegistFunction("OnUpdateMagicNodeEntity", "NativeOnUpdateMagicNodeEntity");
        RegistFunction("OnRemvoeMagicNodeEntity", "NativeOnRemvoeMagicNodeEntity");
        RegistFunction("OnPlayerBuffChanged", "NativeOnPlayerBuffChanged");
        RegistFunction("OnPlayerBagMagicNodeChanged", "NativeOnPlayerBagMagicNodeChanged");
        RegistFunction("OnPlayerWandMagicNodeChanged", "NativeOnPlayerWandMagicNodeChanged");
        RegistFunction("OnPlayerWandCDUpdate", "NativeOnPlayerWandCDUpdate");

        RegistFunction("ShowFloatText", "ShowFloatText");
        RegistFunction("ShowEffect", "ShowEffect");
    }
}
