using System;
using System.Collections.Generic;
using UnityEngine;

public class RendererWorld : IDisposable
{
    public event Action OnLateUpdate;
    public event Action<GameUnit> OnCreateGameEntity;
    public event Action<long> OnDestroyGameEntity;
    public event Action<FloatText> OnCreateFloatText;
    private readonly MapBlockRenderer mapBlock;
    private readonly Dictionary<long, GameEntity> entities = new Dictionary<long, GameEntity>();
    private readonly Dictionary<long, GameUnit> units = new Dictionary<long, GameUnit>();
    public long LocalPlayerId { get; private set; }

    public LogicPlayerEntity[] players;
    public LogicWorld World { get; private set; }
    public RendererWorld(GameMgr mgr, LoadingProgress loading)
    {
        mapBlock = new MapBlockRenderer(mgr, loading);
        loading.Progress = 1;
    }
    public void Load(GameMgr mgr, LogicWorld world, LoadingProgress loading)
    {
        Unload();
        if (world == null) loading.Progress = 1;
        else
        {
            loading.Progress = .2f;
            World = world;
            lock (world)
            {
                var info = mgr.Room.Info;
                var infos = new CtrlInfo[info.members.Count + 1];
                infos[0] = new CtrlInfo(info.ctrlId, info.owner.name);
                for (int i = 0; i < info.members.Count; i++)
                    infos[i + 1] = new CtrlInfo(info.members[i].ctrlId, info.members[i].player.name);
                var loadResult = world.LoadGameData(infos);
                LocalPlayerId = mgr.GetPlayerId(PlayerInfo.Local.id);

                foreach (var item in loadResult.entities) OnEntityChanged(item.Value);
                foreach (var item in loadResult.units) OnUpdateUnitEntity(item.Value);
                foreach (var unitBuffs in loadResult.buffs)
                    foreach (var item in unitBuffs.Value)
                    {
                        OnUpdateBuffEntity(item.Value);
                        OnUnitBuffChanged(unitBuffs.Key, item.Key, true);
                    }
                foreach (var item in loadResult.magicNodes) OnUpdateMagicNodeEntity(item.Value);
                //todo 加载玩家当前数据：背包，拾取列表，法杖状态，自身状态信息等

                world.OnEntityChanged += OnEntityChanged;
                world.OnEntityRemoved += OnEntityRemoved;
                world.OnUpdateUnitEntity += OnUpdateUnitEntity;
                world.OnRemoveUnitEntity += OnRemoveUnitEntity;
                world.OnUnitBuffChanged += OnUnitBuffChanged;
                world.OnUpdateBuff += OnUpdateBuffEntity;
                world.OnRemoveBuff += OnRemoveBuffEntity;
                world.OnUpdateMagicNode += OnUpdateMagicNodeEntity;
                world.OnRemvoeMagicNode += OnRemoveMagicNodeEntity;
                world.OnPlayerBagMagicNodeChanged += OnPlayerBagMagicNodeChanged;
                world.OnPlayerWandMagicNodeChanged += OnPlayerWandMagicNodeChanged;
                world.OnPlayerWandCDUpdate += OnPlayerWandCDUpdate;
                world.OnPlayerMagicNodePickListChanged += OnPlayerMagicNodePickListChanged;
                world.OnPlayerWandChanged += OnPlayerWandChanged;

                world.OnFloatTextMsg += OnFloatTextMsg;
                world.OnEffectMsg += OnEffectMsg;
            }
            loading.Progress = 1;
        }
    }

    public void Unload()
    {
        if (World == null) return;
        World.OnEffectMsg -= OnEffectMsg;
        World.OnFloatTextMsg -= OnFloatTextMsg;

        World.OnPlayerWandChanged -= OnPlayerWandChanged;
        World.OnPlayerMagicNodePickListChanged -= OnPlayerMagicNodePickListChanged;
        World.OnPlayerWandCDUpdate -= OnPlayerWandCDUpdate;
        World.OnPlayerWandMagicNodeChanged -= OnPlayerWandMagicNodeChanged;
        World.OnPlayerBagMagicNodeChanged -= OnPlayerBagMagicNodeChanged;
        World.OnRemvoeMagicNode -= OnRemoveMagicNodeEntity;
        World.OnUpdateMagicNode -= OnUpdateMagicNodeEntity;
        World.OnUnitBuffChanged -= OnUnitBuffChanged;
        World.OnRemoveBuff -= OnRemoveBuffEntity;
        World.OnUpdateBuff -= OnUpdateBuffEntity;
        World.OnRemoveUnitEntity -= OnRemoveUnitEntity;
        World.OnUpdateUnitEntity -= OnUpdateUnitEntity;
        World.OnEntityRemoved -= OnEntityRemoved;
        World.OnEntityChanged -= OnEntityChanged;
        World = null;
        foreach (var item in units) item.Value.OnRemove();
        units.Clear();
        foreach (var item in entities) item.Value.OnRemove(true);
        entities.Clear();
        //todo 清理
    }
    #region EntityEvents
    private void OnEffectMsg(LogicEffectMsg msg)
    {

    }
    private void OnFloatTextMsg(LogicFloatTextMsg msg)
    {

    }

    private void OnPlayerWandChanged(long player, long wand)
    {

    }
    private void OnPlayerMagicNodePickListChanged(long player, long nodeId, bool addition)
    {

    }
    private void OnPlayerWandCDUpdate(long player, long wand, LogicTimeSpan cd)
    {

    }
    private void OnEntityChanged(LogicEntity entity)
    {

    }
    private void OnEntityRemoved(long id, bool immediately)
    {

    }
    private void OnUpdateUnitEntity(LogicUnitEntity unit)
    {

    }
    private void OnRemoveUnitEntity(long id)
    {

    }
    private void OnUnitBuffChanged(long unit, long buff, bool addition)
    {

    }

    private void OnUpdateBuffEntity(LogicBuffEntity buff)
    {

    }
    private void OnRemoveBuffEntity(long id)
    {

    }
    private void OnUpdateMagicNodeEntity(LogicMagicNodeEntity node)
    {

    }
    private void OnRemoveMagicNodeEntity(long id)
    {

    }
    private void OnPlayerBagMagicNodeChanged(long player, long node, bool addition)
    {

    }
    private void OnPlayerWandMagicNodeChanged(long player, long wand, long node, long slot)
    {

    }
    #endregion
    public void Update(float deltaTime)
    {
        mapBlock.OnRendererUpdate();
        foreach (var entity in entities) entity.Value.UpdateMove(deltaTime);
    }

    public void LateUpdate() { OnLateUpdate?.Invoke(); }

    public void Dispose()
    {
        mapBlock.Dispose();
    }
}
