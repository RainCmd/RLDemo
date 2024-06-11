using System;
using System.Collections.Generic;
using UnityEngine;

public class RendererWorld : IDisposable
{
    public event Action OnLateUpdate;
    public event Action<GameUnit> OnCreateGameUnit;
    public event Action<long> OnDestroyGameUnit;
    public event Action<FloatText> OnCreateFloatText;
    private readonly MapBlockRenderer mapBlock;
    private readonly Dictionary<long, GameEntity> entities = new Dictionary<long, GameEntity>();
    private readonly Dictionary<long, GameUnit> units = new Dictionary<long, GameUnit>();
    public readonly Dictionary<long, LogicBuffEntity> buffs = new Dictionary<long, LogicBuffEntity>();
    public readonly Dictionary<long, LogicMagicNodeEntity> magicNodes = new Dictionary<long, LogicMagicNodeEntity>();

    public readonly RendererEntityManager rendererEntityManager = new RendererEntityManager();

    public readonly PlayerDataManager playerDataManager = new PlayerDataManager();

    public GameMgr Mgr { get; private set; }

    #region logic=>renderer缓冲区
    private readonly Pipeline<LogicFloatTextMsg> floatTextMsgPipeline = new Pipeline<LogicFloatTextMsg>(4);
    private readonly Pipeline<L2RData> l2RDatas = new Pipeline<L2RData>(10, true);
    #endregion

    public LogicWorld World { get; private set; }
    public RendererWorld(GameMgr mgr, LoadingProgress loading)
    {
        Mgr = mgr;
        mapBlock = new MapBlockRenderer(mgr.CameraMgr, loading);
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
                playerDataManager.localPlayer = mgr.GetPlayerId(PlayerInfo.Local.id);

                foreach (var item in loadResult.entities.Values)
                    EnL2REvent(L2RData.EntityChanged(item));
                foreach (var item in loadResult.units.Values)
                    EnL2REvent(L2RData.UpdateUnitEntity(item));
                foreach (var unitBuffs in loadResult.buffs)
                    foreach (var item in unitBuffs.Value)
                    {
                        EnL2REvent(L2RData.UpdateBuffEntity(item.Value));
                        EnL2REvent(L2RData.UnitBuffChanged(unitBuffs.Key, item.Key, true));
                    }
                foreach (var item in loadResult.magicNodes)
                    EnL2REvent(L2RData.UpdateMagicNodeEntity(item.Value));

                foreach (var player in loadResult.players)
                {
                    EnL2REvent(L2RData.PlayerHeroChanged(player.playerId, player.hero));
                    foreach (var item in player.bag)
                        EnL2REvent(L2RData.PlayerBagMagicNodeChanged(player.playerId, item, true));
                    for (int i = 0; i < player.wands.Length; i++)
                    {
                        var wand = player.wands[i];
                        foreach (var item in wand.nodes)
                            if (item != 0)
                                EnL2REvent(L2RData.PlayerWandMagicNodeChanged(player.playerId, i, item, i));
                        EnL2REvent(L2RData.PlayerWandCDUpdate(player.playerId, i, wand.cd));
                    }
                    foreach (var item in player.picks)
                        EnL2REvent(L2RData.PlayerMagicNodePickListChanged(player.playerId, item, true));

                    EnL2REvent(L2RData.PlayerWandChanged(player.playerId, player.wand));
                }

                world.OnRendererMsg += EnL2REvent;
                world.OnFloatTextMsg += OnFloatTextMsg;
            }
            Update(0);
            loading.Progress = 1;
        }
    }

    public void Unload()
    {
        if (World == null) return;
        World.OnFloatTextMsg -= OnFloatTextMsg;
        World.OnRendererMsg -= EnL2REvent;

        World = null;
        foreach (var item in units) item.Value.OnRemove();
        units.Clear();
        foreach (var item in entities) item.Value.OnRemove(true);
        entities.Clear();
        rendererEntityManager.Dispose();

        floatTextMsgPipeline.Clear();
    }
    private void EnL2REvent(L2RData data)
    {
        l2RDatas.En(data);
    }
    private void OnFloatTextMsg(LogicFloatTextMsg msg)
    {
        floatTextMsgPipeline.En(msg);
    }

    public void Update(float deltaTime)
    {
        mapBlock.OnRendererUpdate();
        while (floatTextMsgPipeline.De(out var floatTextMsg))
        {
            var position = new Vector3((float)floatTextMsg.position.x, (float)floatTextMsg.position.y, (float)floatTextMsg.position.z);
            var color = new Color((float)floatTextMsg.color.x, (float)floatTextMsg.color.y, (float)floatTextMsg.color.z);
            OnCreateFloatText?.Invoke(new FloatText(position, position + Vector3.up, color, floatTextMsg.text));
        }
        while (l2RDatas.De(out var data))
        {
            switch (data.type)
            {
                case L2RType.PlayerHeroChanged:
                    playerDataManager.OnPlayerHeroChanged(data.player, data.hero);
                    break;
                case L2RType.PlayerWandChanged:
                    playerDataManager.OnPlayerWandChanged(data.player, data.wand);
                    break;
                case L2RType.PlayerMagicNodePickListChanged:
                    if (!magicNodes.ContainsKey(data.node.id))
                    {
                        L2R_Err("PlayerMagicNodePickListChanged: node id {0} 未找到".Format(data.node.id));
                        break;
                    }
                    playerDataManager.OnPickListChanged(data.player, data.node.id, data.addition);
                    break;
                case L2RType.PlayerWandCDUpdate:
                    playerDataManager.OnWandCDChanged(data.player, data.wand, data.cd);
                    break;
                case L2RType.EntityChanged:
                    {
                        if (entities.TryGetValue(data.entity.id, out var entity)) entity.Update(data.entity);
                        else entities.Add(data.entity.id, new GameEntity(this, data.entity));
                    }
                    break;
                case L2RType.EntityTransformChanged:
                    {
                        if (entities.TryGetValue(data.entity.id, out var entity))
                            entity.UpdateTransform(data.entity.forward.ToVector(), data.entity.position.ToVector(), data.immediately);
                        else L2R_Err("EntityTransformChanged: entity id {0} 未找到".Format(data.entity.id));
                    }
                    break;
                case L2RType.EntityRemoved:
                    {
                        if (entities.TryGetValue(data.entity.id, out var entity))
                        {
                            entities.Remove(data.entity.id);
                            entity.OnRemove(data.immediately);
                        }
                        else L2R_Err("EntityRemoved: entity id {0} 未找到".Format(data.entity.id));
                    }
                    break;
                case L2RType.UpdateUnitEntity:
                    {
                        if (units.TryGetValue(data.unit.id, out var unit)) unit.Update(data.unit);
                        else if (entities.TryGetValue(data.unit.id, out var entity))
                        {
                            unit = new GameUnit(entity, data.unit);
                            units.Add(data.unit.id, unit);
                            OnCreateGameUnit?.Invoke(unit);
                        }
                        else L2R_Err("UpdateUnitEntity: unit id {0} 未找到对应的entity".Format(data.unit.id));
                    }
                    break;
                case L2RType.RemoveUnitEntity:
                    {
                        if (units.TryGetValue(data.unit.id, out var unit))
                        {
                            unit.OnRemove();
                            units.Remove(data.unit.id);
                            OnDestroyGameUnit?.Invoke(data.unit.id);
                        }
                        else L2R_Err("RemoveUnitEntity: unit id {0} 未找到".Format(data.unit.id));
                    }
                    break;
                case L2RType.UnitBuffChanged:
                    {
                        if (!units.TryGetValue(data.unit.id, out var unit))
                        {
                            L2R_Err("UnitBuffChanged: unit id {0} 未找到".Format(data.unit.id));
                            break;
                        }
                        if (!buffs.TryGetValue(data.buff.id, out var buff))
                        {
                            L2R_Err("UnitBuffChanged: buff id {0} 未找到".Format(data.buff.id));
                            break;
                        }
                        if (data.addition) unit.buffs.Add(data.buff.id);
                        else unit.buffs.Remove(data.buff.id);
                    }
                    break;
                case L2RType.UpdateBuffEntity:
                    buffs[data.buff.id] = data.buff;
                    break;
                case L2RType.RemoveBuffEntity:
                    if (!buffs.Remove(data.buff.id)) L2R_Err("RemoveBuffEntity: buff id {0} 未找到".Format(data.buff.id));
                    break;
                case L2RType.UpdateMagicNodeEntity:
                    magicNodes[data.node.id] = data.node;
                    break;
                case L2RType.RemoveMagicNodeEntity:
                    if (!magicNodes.Remove(data.node.id)) L2R_Err("RemoveMagicNodeEntity: node id {0} 未找到".Format(data.node.id));
                    break;
                case L2RType.PlayerBagMagicNodeChanged:
                    if (!magicNodes.ContainsKey(data.node.id))
                    {
                        L2R_Err("PlayerBagMagicNodeChanged: node id {0} 未找到".Format(data.node.id));
                        break;
                    }
                    playerDataManager.OnBagListChanged(data.player, data.node.id, data.addition);
                    break;
                case L2RType.PlayerWandMagicNodeChanged:
                    if (!magicNodes.ContainsKey(data.node.id))
                    {
                        L2R_Err("PlayerBagMagicNodeChanged: node id {0} 未找到".Format(data.node.id));
                        break;
                    }
                    playerDataManager.OnWandNodeChanged(data.player, data.wand, data.slot, data.node.id);
                    break;
            }
        }
        foreach (var entity in entities) entity.Value.OnUpdate(deltaTime);
    }
    private void L2R_Err(string msg)
    {
        Debug.LogError(msg);
    }
    public void LateUpdate() { OnLateUpdate?.Invoke(); }

    public bool TryGetEntity(long id, out GameEntity entity)
    {
        return entities.TryGetValue(id, out entity);
    }
    public bool TryGetUnit(long id, out GameUnit unit)
    {
        return units.TryGetValue(id, out unit);
    }

    public IEnumerable<GameUnit> GetGameUnits()
    {
        return units.Values;
    }

    public void Dispose()
    {
        mapBlock.Dispose();
        Unload();
    }
}
