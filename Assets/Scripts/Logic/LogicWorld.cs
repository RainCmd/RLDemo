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
    public string resource;
    public string anim;
    public Real3 forward;
    public Real3 position;
    public LogicEntity(long id, string resource, string anim, Real3 forward, Real3 position)
    {
        this.id = id;
        this.resource = resource;
        this.anim = anim;
        this.forward = forward;
        this.position = position;
    }
}
public struct LogicUnitEntity
{
    public long id;
    public long player;
    public UnitType type;
    public Real hp;
    public Real maxHP;
    public Real mp;
    public Real maxMP;
    public LogicUnitEntity(long id, long player, UnitType type, Real hp, Real maxHP, Real mp, Real maxMP)
    {
        this.id = id;
        this.player = player;
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
    public long configId;
    public long number;
    public LogicMagicNodeEntity(long id, long configId, long number)
    {
        this.id = id;
        this.configId = configId;
        this.number = number;
    }
}
public struct LogicWand
{
    public readonly LogicTimeSpan cd;
    public readonly long[] nodes;
    public LogicWand(Real cdStart, Real cdEnd, long[] nodes)
    {
        cd = new LogicTimeSpan(cdStart, cdEnd);
        this.nodes = nodes;
    }
}
public struct LogicPlayerEntity
{
    public long playerId;
    public string name;
    public readonly long hero;
    public readonly long wand;
    public readonly List<long> bag;
    public readonly List<long> picks;
    public LogicWand[] wands;
    public LogicPlayerEntity(long playerId, string name, long hero, long wand, long[] bag, long[] picks)
    {
        this.playerId = playerId;
        this.name = name;
        this.hero = hero;
        this.wand = wand;
        this.bag = new List<long>(bag);
        this.picks = new List<long>(picks);
        wands = new LogicWand[3];
    }
}
public class LogicInitResult
{
    public LogicPlayerEntity[] players;
    public readonly Dictionary<long, LogicEntity> entities = new Dictionary<long, LogicEntity>();
    public readonly Dictionary<long, LogicUnitEntity> units = new Dictionary<long, LogicUnitEntity>();
    public readonly Dictionary<long, Dictionary<long, LogicBuffEntity>> buffs = new Dictionary<long, Dictionary<long, LogicBuffEntity>>();// unitId => buffId =>> buff
    public readonly Dictionary<long, LogicMagicNodeEntity> magicNodes = new Dictionary<long, LogicMagicNodeEntity>();
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
public class LogicWorld : IDisposable
{
    public readonly long[] ctrlIds;
    private readonly Queue<string> messages = new Queue<string>();
    private readonly Queue<IDisposable> disposables = new Queue<IDisposable>();
    private Kernel kernel;
    private Function[] operFuncs;
    public event Action<LogicEntity> OnEntityChanged;
    public event Action<long, bool> OnEntityRemoved;
    public event Action<LogicUnitEntity> OnUpdateUnitEntity;
    public event Action<long> OnRemoveUnitEntity;
    public event Action<long, long, bool> OnUnitBuffChanged;
    public event Action<LogicBuffEntity> OnUpdateBuff;
    public event Action<long> OnRemoveBuff;
    public event Action<LogicMagicNodeEntity> OnUpdateMagicNode;
    public event Action<long> OnRemvoeMagicNode;
    public event Action<long, long, bool> OnPlayerBagMagicNodeChanged;
    public event Action<long, long, long, long> OnPlayerWandMagicNodeChanged;
    public event Action<long, long, LogicTimeSpan> OnPlayerWandCDUpdate;
    public event Action<long, long, bool> OnPlayerMagicNodePickListChanged;
    public event Action<long, long> OnPlayerWandChanged;

    public event Action<LogicFloatTextMsg> OnFloatTextMsg;
    public LogicWorld(long[] ctrlIds, long seed, LoadingProgress loading)
    {
        this.ctrlIds = ctrlIds;
        var asset = Resources.Load<TextAsset>("RainLibraries/RLDemo.lib");
        var lib = RainLib.Create(asset.bytes);
        var libs = new RainLib[] { lib };
        var parameter = new StartupParameter(libs, seed, 0xff, 0xff,
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
    private long[] GetCtrls()
    {
        return ctrlIds;
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

    private void NativeOnUpdateEntity(long id, string resource, string anim, Real3 forward, Real3 position)
    {
        OnEntityChanged?.Invoke(new LogicEntity(id, resource, anim, forward, position));
    }
    private void NativeOnRemoveEntity(long id, bool immediately)
    {
        OnEntityRemoved?.Invoke(id, immediately);
    }
    private void NativeOnUpdateUnitEntity(long id, long player, long type, Real hp, Real maxHP, Real mp, Real maxMP)
    {
        OnUpdateUnitEntity?.Invoke(new LogicUnitEntity(id, player, (UnitType)type, hp, maxHP, mp, maxMP));
    }
    private void NativeOnRemoveUnitEntity(long id)
    {
        OnRemoveUnitEntity?.Invoke(id);
    }
    private void NativeOnUnitBuffChanged(long unitId, long buffId, bool addition)
    {
        OnUnitBuffChanged?.Invoke(unitId, buffId, addition);
    }
    private void NativeOnUpdateBuff(long id, long icon, long number, Real start, Real end)
    {
        OnUpdateBuff?.Invoke(new LogicBuffEntity(id, icon, number, new LogicTimeSpan(start, end)));
    }
    private void NativeOnRemoveBuff(long id)
    {
        OnRemoveBuff?.Invoke(id);
    }
    private void NativeOnUpdateMagicNode(long id, long configId, long number)
    {
        OnUpdateMagicNode?.Invoke(new LogicMagicNodeEntity(id, configId, number));
    }
    private void NativeOnRemvoeMagicNode(long id)
    {
        OnRemvoeMagicNode?.Invoke(id);
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
    private void NativeOnPlayerMagicNodePickListChanged(long player, long nodeID, bool addition)
    {
        OnPlayerMagicNodePickListChanged?.Invoke(player, nodeID, addition);
    }
    private void NativeOnPlayerWandChanged(long player, long wand)
    {
        OnPlayerWandChanged?.Invoke(player, wand);
    }

    private void ShowFloatText(Real3 position, Real3 color, string value)
    {
        OnFloatTextMsg?.Invoke(new LogicFloatTextMsg(position, color, value));
    }

    private void NativeOnLoadGameEntity(long id, string resource, string anim, Real3 forward, Real3 position)
    {
        initResult?.entities?.Add(id, new LogicEntity(id, resource, anim, forward, position));
    }
    private void NativeOnLoadGameUnit(long id, long player, UnitType unitType, Real hp, Real maxHP, Real mp, Real maxMP)
    {
        initResult?.units?.Add(id, new LogicUnitEntity(id, player, unitType, hp, maxHP, mp, maxMP));
    }
    private void NativeOnLoadBuff(long unitId, long id, long icon, long number, Real startTime, Real endTime)
    {
        if (initResult != null)
        {
            if (!initResult.buffs.TryGetValue(unitId, out var buffs)) initResult.buffs.Add(unitId, buffs = new Dictionary<long, LogicBuffEntity>());
            buffs.Add(id, new LogicBuffEntity(id, icon, number, new LogicTimeSpan(startTime, endTime)));
        }
    }
    private void NativeOnLoadMagicNode(long id, long configId, long number)
    {
        if (initResult != null) initResult.magicNodes[id] = new LogicMagicNodeEntity(id, configId, number);//多个玩家间pick list中可能会有重复
    }
    #endregion
    #region InitFunctions
    private LogicInitResult initResult;
    private long Init_GetPlayerCount()
    {
        using (var function = kernel.FindFunction("Init_GetPlayerCount"))
        using (var invoker = function.CreateInvoker())
        {
            invoker.Start(true, true);
            return invoker.GetIntegerReturnValue(0);
        }
    }
    public LogicPlayerEntity[] GetLogicPlayers()
    {
        var players = new LogicPlayerEntity[Init_GetPlayerCount()];
        using (var function = kernel.FindFunction("Init_GetPlayer"))
            for (var i = 0; i < players.Length; i++)
                using (var invoker = function.CreateInvoker())
                {
                    invoker.SetIntegerParameter(0, i);
                    invoker.Start(true, true);
                    var playerName = invoker.GetStringReturnValue(0);
                    var hero = invoker.GetIntegerReturnValue(1);
                    var wand = invoker.GetIntegerReturnValue(2);
                    var bag = invoker.GetIntegersReturnValue(3);
                    var picks = invoker.GetIntegersReturnValue(4);
                    players[i] = new LogicPlayerEntity(i, playerName,hero, wand, bag,  picks);
                }
        using (var function = kernel.FindFunction("Init_GetPlayerWand"))
            for (var pid = 0; pid < players.Length; pid++)
                for (var wand = 0; wand < 3; wand++)
                    using (var invoker = function.CreateInvoker())
                    {
                        invoker.SetIntegerParameter(0, pid);
                        invoker.SetIntegerParameter(1, wand);
                        invoker.Start(true, true);
                        var start = invoker.GetIntegerReturnValue(0);
                        var end = invoker.GetIntegerReturnValue(1);
                        var nodes = invoker.GetIntegersReturnValue(2);
                        players[pid].wands[wand] = new LogicWand(start, end, nodes);
                    }
        return players;
    }
    public LogicInitResult LoadGameData(CtrlInfo[] infos)
    {
        using (var function = kernel.FindFunction("Init_SetPlayerName"))
            foreach (var info in infos)
                using (var invoker = function.CreateInvoker())
                {
                    invoker.SetIntegerParameter(0, info.ctrlId);
                    invoker.SetStringParameter(1, info.name);
                    invoker.Start(true, true);
                }
        var initResult = this.initResult = new LogicInitResult();
        using (var function = kernel.FindFunction("LoadGameData"))
        using (var invoker = function.CreateInvoker())
            invoker.Start(true, true);
        initResult.players = GetLogicPlayers();
        this.initResult = null;
        return initResult;
    }
    #endregion
    public long GetPlayerId(long ctrlId)
    {
        for (var i = 0; i < ctrlIds.Length; i++)
            if (ctrlIds[i] == ctrlId)
                return i;
        return -1;
    }
    public long GetCtrlId(long playerId)
    {
        return ctrlIds[playerId];
    }
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
    public void EntryGame(IRoom room)
    {
        using (var onEntryGame = kernel.FindFunction("GameEntry"))
        using (var onEntryGameInvoker = onEntryGame.CreateInvoker())
            onEntryGameInvoker.Start(true, false);

        new Thread(() => LogicLoop(room)).Start();
    }
    private void LogicLoop(IRoom room)
    {
        var step = TimeSpan.FromMilliseconds(1000 / Config.LFPS).TotalMilliseconds;
        var start = DateTime.Now;
        while (room.State != RoomState.Invalid)
        {
            var kernel = this.kernel;
            if (kernel == null) break;
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
    private static void RegistFunction(string rainFunctionName, string csFunctionName)
    {
        callerMap.Add(Config.GameName + "." + rainFunctionName, CallerHelper.Create<LogicWorld>(csFunctionName));
    }
    static LogicWorld()
    {
        RegistFunction("Debug", "Debug");

        RegistFunction("InitGame.GetControls", "GetCtrls");
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
        RegistFunction("OnUnitBuffChanged", "NativeOnUnitBuffChanged");
        RegistFunction("OnUpdateBuff", "NativeOnUpdateBuff");
        RegistFunction("OnRemoveBuff", "NativeOnRemoveBuff");
        RegistFunction("OnUpdateMagicNode", "NativeOnUpdateMagicNode");
        RegistFunction("OnRemvoeMagicNode", "NativeOnRemvoeMagicNode");
        RegistFunction("OnPlayerBagMagicNodeChanged", "NativeOnPlayerBagMagicNodeChanged");
        RegistFunction("OnPlayerWandMagicNodeChanged", "NativeOnPlayerWandMagicNodeChanged");
        RegistFunction("OnPlayerWandCDUpdate", "NativeOnPlayerWandCDUpdate");
        RegistFunction("OnPlayerMagicNodePickListChanged", "NativeOnPlayerMagicNodePickListChanged");
        RegistFunction("OnPlayerWandChanged", "NativeOnPlayerWandChanged");

        RegistFunction("ShowFloatText", "ShowFloatText");
    }
}
