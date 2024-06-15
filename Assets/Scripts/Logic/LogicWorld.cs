using System;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;
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
    public LogicEntity(long id, Real3 forward, Real3 position)
    {
        this.id = id;
        resource = anim = null;
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
    public long configId;
    public long number;
    public LogicTimeSpan time;
    public LogicBuffEntity(long id, long configId, long numer, LogicTimeSpan time)
    {
        this.id = id;
        this.configId = configId;
        this.number = numer;
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
[AttributeUsage(AttributeTargets.Method)]
public class RainMethodAttribute : Attribute
{
    public readonly string function;
    public RainMethodAttribute(string function) { this.function = function; }
}
public class LogicWorld : IDisposable
{
    public readonly long[] ctrlIds;
    private readonly Queue<IDisposable> disposables = new Queue<IDisposable>();
    private Kernel kernel;
    private Function[] operFuncs;
    public event Action<L2RData> OnRendererMsg;

    public event Action<LogicFloatTextMsg> OnFloatTextMsg;
    public LogicWorld(long[] ctrlIds, long seed, LoadingProgress loading)
    {
        RegistFunctions();
        this.ctrlIds = ctrlIds;
        var lib = RainLib.Create(LoadLibrary("RLDemo")) ?? throw new NullReferenceException("逻辑世界lib加载失败");
        var libs = new RainLib[] { lib };
        var parameter = new StartupParameter(libs, seed, 0xff, 0xff,
            OnReferenceEntity, OnReleaseEntity,
            LoadLibrary, LoadCaller, OnExceptionExit);
        kernel = RainLanguageAdapter.CreateKernel(parameter, LoadProgramDatabase);
        using (var init = kernel.FindFunction("GameMain"))
        using (var invoker = init.CreateInvoker())
            invoker.Start(true, false);
        InitOperFuncs();
        loading.Progress = 1;
    }
    private OnCaller LoadCaller(Kernel kernel, string fullName, RainType[] parameters)
    {
        if (callerMap.TryGetValue(fullName, out CallerHelper helper)) return helper.OnCaller;
        else
        {
            var log = "函数：{0} 没有绑定!".Format(fullName);
            GameLog.Show(Color.red, log);
            UnityEngine.Debug.LogError(log);
            return (k, c) => UnityEngine.Debug.LogError("调用了未绑定函数:" + fullName);
        }
    }
    #region NativeFunctions
    private struct Line
    {
        private Vector3 start, end;
        private Color color;

        public Line(Vector3 start, Vector3 end, Color color)
        {
            this.start = start;
            this.end = end;
            this.color = color;
        }
        public void Draw()
        {
            UnityEngine.Debug.DrawLine(start, end, color);
        }
    }
    private List<Line> lines = new List<Line>();
    [RainMethod("ClearLines")]
    private void ClearLines()
    {
        lock (lines)
            lines.Clear();
    }
    [RainMethod("DrwaLine")]
    private void DrawLine(Real3 start, Real3 end, Real4 color)
    {
        lock (lines)
            lines.Add(new Line(start.ToVector(), end.ToVector(), new Color((float)color.x, (float)color.y, (float)color.z, (float)color.z)));
    }
    public void ShowDebugLine()
    {
        lock (lines)
            foreach (var line in lines)
                line.Draw();
    }

    [RainMethod("Debug")]
    private void Debug(string msg)
    {
        GameLog.Show(Color.white, msg);
        UnityEngine.Debug.Log("<color=#00ffcc>雨言Debug</color>:{0}".Format(msg));
    }

    [RainMethod("InitGame.GetControls")]
    private long[] GetCtrls()
    {
        return ctrlIds;
    }
    [RainMethod("GameConfig.ConfigMagicNode_GetConfigCount")]
    private long Config_GetMagicNodeCount()
    {
        return LogicConfig.magicNodes.Length;
    }
    [RainMethod("GameConfig.ConfigMagicNode_GetConfig")]
    private ConfigMagicNode Config_GetMagicNode(long index)
    {
        return LogicConfig.magicNodes[index];
    }
    [RainMethod("GameConfig.ConfigEntity_GetConfigCount")]
    private long Config_GetEntityConfigCount()
    {
        return LogicConfig.entities.Length;
    }
    [RainMethod("GameConfig.ConfigEntity_GetConfig")]
    private ConfigEntity Config_GetEntityConfig(long index)
    {
        return LogicConfig.entities[index];
    }
    [RainMethod("GameConfig.ConfigUnit_GetConfigCount")]
    private long Config_GetUnitCount()
    {
        return LogicConfig.units.Length;
    }
    [RainMethod("GameConfig.ConfigUnit_GetConfig")]
    private ConfigUnit Config_GetUnit(long index)
    {
        return LogicConfig.units[index];
    }
    [RainMethod("GameConfig.ConfigBuff_GetConfigCount")]
    private long Config_GetBuffCount()
    {
        return LogicConfig.buffs.Length;
    }
    [RainMethod("GameConfig.ConfigBuff_GetConfig")]
    private ConfigBuff Config_GetBuff(long index)
    {
        return LogicConfig.buffs[index];
    }

    [RainMethod("OnUpdateEntity")]
    private void NativeOnUpdateEntity(long id, string resource, string anim, Real3 forward, Real3 position)
    {
        OnRendererMsg?.Invoke(L2RData.EntityChanged(new LogicEntity(id, resource, anim, forward, position)));
    }
    [RainMethod("OnUpdateEntityTransform")]
    private void NativeOnUpdateEntityTransform(long id, Real3 forward, Real3 position, bool immediately)
    {
        OnRendererMsg?.Invoke(L2RData.EntityTransformChanged(id, forward, position, immediately));
    }
    [RainMethod("OnRemoveEntity")]
    private void NativeOnRemoveEntity(long id, bool immediately)
    {
        OnRendererMsg?.Invoke(L2RData.EntityRemoved(id, immediately));
    }
    [RainMethod("OnUpdateUnitEntity")]
    private void NativeOnUpdateUnitEntity(long id, long player, UnitType type, Real hp, Real maxHP, Real mp, Real maxMP)
    {
        OnRendererMsg?.Invoke(L2RData.UpdateUnitEntity(new LogicUnitEntity(id, player, type, hp, maxHP, mp, maxMP)));
    }
    [RainMethod("OnRemoveUnitEntity")]
    private void NativeOnRemoveUnitEntity(long id)
    {
        OnRendererMsg?.Invoke(L2RData.RemoveUnitEntity(id));
    }
    [RainMethod("OnUnitBuffChanged")]
    private void NativeOnUnitBuffChanged(long unitId, long buffId, bool addition)
    {
        OnRendererMsg?.Invoke(L2RData.UnitBuffChanged(unitId, buffId, addition));
    }
    [RainMethod("OnUpdateBuff")]
    private void NativeOnUpdateBuff(long id, long configId, long number, Real start, Real end)
    {
        OnRendererMsg?.Invoke(L2RData.UpdateBuffEntity(new LogicBuffEntity(id, configId, number, new LogicTimeSpan(start, end))));
    }
    [RainMethod("OnRemoveBuff")]
    private void NativeOnRemoveBuff(long id)
    {
        OnRendererMsg?.Invoke(L2RData.RemoveBuffEntity(id));
    }
    [RainMethod("OnUpdateMagicNode")]
    private void NativeOnUpdateMagicNode(long id, long configId, long number)
    {
        OnRendererMsg?.Invoke(L2RData.UpdateMagicNodeEntity(new LogicMagicNodeEntity(id, configId, number)));
    }
    [RainMethod("OnRemvoeMagicNode")]
    private void NativeOnRemvoeMagicNode(long id)
    {
        OnRendererMsg?.Invoke(L2RData.RemoveMagicNodeEntity(id));
    }
    [RainMethod("OnPlayerBagMagicNodeChanged")]
    private void NativeOnPlayerBagMagicNodeChanged(long player, long nodeID, bool addition)
    {
        OnRendererMsg?.Invoke(L2RData.PlayerBagMagicNodeChanged(player, nodeID, addition));
    }
    [RainMethod("OnPlayerWandMagicNodeChanged")]
    private void NativeOnPlayerWandMagicNodeChanged(long player, long wand, long nodeID, long slot)
    {
        OnRendererMsg?.Invoke(L2RData.PlayerWandMagicNodeChanged(player, wand, nodeID, slot));
    }
    [RainMethod("OnPlayerWandCDUpdate")]
    private void NativeOnPlayerWandCDUpdate(long player, long wand, Real start, Real end)
    {
        OnRendererMsg?.Invoke(L2RData.PlayerWandCDUpdate(player, wand, new LogicTimeSpan(start, end)));
    }
    [RainMethod("OnPlayerMagicNodePickListChanged")]
    private void NativeOnPlayerMagicNodePickListChanged(long player, long nodeID, bool addition)
    {
        OnRendererMsg?.Invoke(L2RData.PlayerMagicNodePickListChanged(player, nodeID, addition));
    }
    [RainMethod("OnPlayerWandChanged")]
    private void NativeOnPlayerWandChanged(long player, long wand)
    {
        OnRendererMsg?.Invoke(L2RData.PlayerWandChanged(player, wand));
    }

    [RainMethod("ShowFloatText")]
    private void ShowFloatText(Real3 position, Real3 color, string value)
    {
        OnFloatTextMsg?.Invoke(new LogicFloatTextMsg(position, color, value));
    }

    [RainMethod("InitGame.OnLoadGameEntity")]
    private void NativeOnLoadGameEntity(long id, string resource, string anim, Real3 forward, Real3 position)
    {
        initResult?.entities?.Add(id, new LogicEntity(id, resource, anim, forward, position));
    }
    [RainMethod("InitGame.OnLoadGameUnit")]
    private void NativeOnLoadGameUnit(long id, long player, UnitType unitType, Real hp, Real maxHP, Real mp, Real maxMP)
    {
        initResult?.units?.Add(id, new LogicUnitEntity(id, player, unitType, hp, maxHP, mp, maxMP));
    }
    [RainMethod("InitGame.OnLoadBuff")]
    private void NativeOnLoadBuff(long unitId, long id, long configId, long number, Real startTime, Real endTime)
    {
        if (initResult != null)
        {
            if (!initResult.buffs.TryGetValue(unitId, out var buffs)) initResult.buffs.Add(unitId, buffs = new Dictionary<long, LogicBuffEntity>());
            buffs.Add(id, new LogicBuffEntity(id, configId, number, new LogicTimeSpan(startTime, endTime)));
        }
    }
    [RainMethod("InitGame.OnLoadMagicNode")]
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
                    players[i] = new LogicPlayerEntity(i, playerName, hero, wand, bag, picks);
                }
        using (var function = kernel.FindFunction("Init_GetPlayerWand"))
            for (var pid = 0; pid < players.Length; pid++)
                for (var wand = 0; wand < 3; wand++)
                    using (var invoker = function.CreateInvoker())
                    {
                        invoker.SetIntegerParameter(0, pid);
                        invoker.SetIntegerParameter(1, wand);
                        invoker.Start(true, true);
                        var start = invoker.GetRealReturnValue(0);
                        var end = invoker.GetRealReturnValue(1);
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
        return UIManager.LoadResource(string.Format("RainLibraries/{0}.lib", name));
    }
    private readonly Dictionary<string, RainLanguageAdapter.RainProgramDatabase> databaseMap = new Dictionary<string, RainLanguageAdapter.RainProgramDatabase>();
    private void OnExceptionExit(Kernel kernel, RainStackFrame[] frames, string msg)
    {
        if (msg == "虚拟机被关闭") return;
        GameLog.Show(Color.red, msg);
        msg = string.Format("<color=#ff0000>{0}</color>", msg);
        foreach (var frame in frames)
        {
            if (!databaseMap.TryGetValue(frame.libName, out var database))
            {
                database = RainLanguageAdapter.RainProgramDatabase.Create(LoadProgramDatabase(frame.libName));
                databaseMap.Add(frame.libName, database);
            }
            if (database == null)
                msg += string.Format("\n{0} [<color=#ffcc00>0X{1}</color>]",
                    frame.funName, frame.address.ToString("X"));
            else
            {
                database.GetPosition(frame.address, out var file, out var line);
                msg += string.Format("\n{0} [{1}:<color=#ffcc00>{2}</color>]",
                    frame.funName, file, line);
            }
        }
        UnityEngine.Debug.LogError(msg);
    }
    private byte[] LoadProgramDatabase(string name)
    {
        return UIManager.LoadResource(string.Format("RainProgramDatabase/{0}.pdb", name));
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

    private readonly Dictionary<string, CallerHelper> callerMap = new Dictionary<string, CallerHelper>();
    private void RegistFunctions()
    {
        foreach (var method in typeof(LogicWorld).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            var attr = method.GetCustomAttribute<RainMethodAttribute>();
            if (attr != null)
                callerMap.Add(Config.GameName + "." + attr.function, new CallerHelper(this, method));
        }
    }
}
