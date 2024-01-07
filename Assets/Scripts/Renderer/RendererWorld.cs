using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.UI.CanvasScaler;

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
    #region logic=>renderer缓冲区
    private readonly Pipeline<LogicEffectMsg> effectMsgPipleline = new Pipeline<LogicEffectMsg>(16);
    private readonly Pipeline<LogicFloatTextMsg> floatTextMsgPipeline = new Pipeline<LogicFloatTextMsg>(4);
    private enum L2RType
    {
        PlayerWandChanged,
        PlayerMagicNodePickListChanged,
        PlayerWandCDUpdate,
        EntityChanged,
        EntityRemoved,
        UpdateUnitEntity,
        RemoveUnitEntity,
        UnitBuffChanged,
        UpdateBuffEntity,
        RemoveBuffEntity,
        UpdateMagicNodeEntity,
        RemoveMagicNodeEntity,
        PlayerBagMagicNodeChanged,
        PlayerWandMagicNodeChanged,
    }
    private struct L2RData
    {
        public L2RType type;
        public long player;
        public long wand;
        public long slot;
        public LogicTimeSpan cd;
        public LogicMagicNodeEntity node;
        public LogicEntity entity;
        public bool immediately;
        public LogicUnitEntity unit;
        public LogicBuffEntity buff;
        public bool addition;
    }
    private readonly Queue<L2RData> l2RDatas = new Queue<L2RData>();
    #endregion
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
                foreach (var player in loadResult.players)
                {
                    foreach (var item in player.bag)
                        OnPlayerBagMagicNodeChanged(player.playerId, item, true);
                    for (int i = 0; i < player.wands.Length; i++)
                    {
                        var wand = player.wands[i];
                        foreach (var item in wand.nodes)
                            if (item != 0)
                                OnPlayerWandMagicNodeChanged(player.playerId, i, item, i);
                        OnPlayerWandCDUpdate(player.playerId, i, wand.cd);
                    }
                    foreach (var item in player.picks)
                        OnPlayerMagicNodePickListChanged(player.playerId, item, true);
                    OnPlayerWandChanged(player.playerId, player.wand);
                }

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
        floatTextMsgPipeline.Clear();
        effectMsgPipleline.Clear();
        //todo 清理
    }
    private void EnL2REvent(L2RData data)
    {
        lock (l2RDatas) l2RDatas.Enqueue(data);
    }
    #region EntityEvents
    private void OnEffectMsg(LogicEffectMsg msg)
    {
        effectMsgPipleline.En(msg);
    }
    private void OnFloatTextMsg(LogicFloatTextMsg msg)
    {
        floatTextMsgPipeline.En(msg);
    }

    private void OnPlayerWandChanged(long player, long wand)
    {
        EnL2REvent(new L2RData
        {
            type = L2RType.PlayerWandChanged,
            player = player,
            wand = wand
        });
    }
    private void OnPlayerMagicNodePickListChanged(long player, long nodeId, bool addition)
    {
        EnL2REvent(new L2RData
        {
            type = L2RType.PlayerMagicNodePickListChanged,
            player = player,
            node = new LogicMagicNodeEntity() { id = nodeId },
            addition = addition
        });
    }
    private void OnPlayerWandCDUpdate(long player, long wand, LogicTimeSpan cd)
    {
        EnL2REvent(new L2RData
        {
            type = L2RType.PlayerWandCDUpdate,
            player = player,
            wand = wand,
            cd = cd
        });
    }
    private void OnEntityChanged(LogicEntity entity)
    {
        EnL2REvent(new L2RData
        {
            type = L2RType.EntityChanged,
            entity = entity,
            addition = true
        });
    }
    private void OnEntityRemoved(long id, bool immediately)
    {
        EnL2REvent(new L2RData
        {
            type = L2RType.EntityRemoved,
            entity = new LogicEntity() { id = id },
            immediately = immediately,
            addition = false,
        });
    }
    private void OnUpdateUnitEntity(LogicUnitEntity unit)
    {
        EnL2REvent(new L2RData
        {
            type = L2RType.UpdateUnitEntity,
            unit = unit,
            addition = true
        });
    }
    private void OnRemoveUnitEntity(long id)
    {
        EnL2REvent(new L2RData
        {
            type = L2RType.RemoveUnitEntity,
            unit = new LogicUnitEntity() { id = id },
            addition = false
        });
    }
    private void OnUnitBuffChanged(long unit, long buff, bool addition)
    {
        EnL2REvent(new L2RData
        {
            type = L2RType.UnitBuffChanged,
            unit = new LogicUnitEntity() { id = unit },
            buff = new LogicBuffEntity() { id = buff },
            addition = addition
        });
    }

    private void OnUpdateBuffEntity(LogicBuffEntity buff)
    {
        EnL2REvent(new L2RData
        {
            type = L2RType.UpdateBuffEntity,
            buff = buff,
            addition = true
        });
    }
    private void OnRemoveBuffEntity(long id)
    {
        EnL2REvent(new L2RData
        {
            type = L2RType.RemoveBuffEntity,
            buff = new LogicBuffEntity() { id = id },
            addition = false
        });
    }
    private void OnUpdateMagicNodeEntity(LogicMagicNodeEntity node)
    {
        EnL2REvent(new L2RData
        {
            type = L2RType.UpdateMagicNodeEntity,
            node = node,
            addition = true
        });
    }
    private void OnRemoveMagicNodeEntity(long id)
    {
        EnL2REvent(new L2RData
        {
            type = L2RType.RemoveMagicNodeEntity,
            node = new LogicMagicNodeEntity() { id = id },
            addition = false
        });
    }
    private void OnPlayerBagMagicNodeChanged(long player, long node, bool addition)
    {
        EnL2REvent(new L2RData
        {
            type = L2RType.PlayerBagMagicNodeChanged,
            player = player,
            node = new LogicMagicNodeEntity() { id = node },
            addition = addition
        });
    }
    private void OnPlayerWandMagicNodeChanged(long player, long wand, long node, long slot)
    {
        EnL2REvent(new L2RData
        {
            type = L2RType.PlayerWandMagicNodeChanged,
            player = player,
            wand = wand,
            slot = slot,
            node = new LogicMagicNodeEntity() { id = node },
        });
    }
    #endregion
    public void Update(float deltaTime)
    {
        mapBlock.OnRendererUpdate();
        while (effectMsgPipleline.De(out var effectMsg))
        {
            //创建特效
        }
        while (floatTextMsgPipeline.De(out var floatTextMsg))
        {
            var position = new UnityEngine.Vector3((float)floatTextMsg.position.x, (float)floatTextMsg.position.y, (float)floatTextMsg.position.z);
            var color = new Color((float)floatTextMsg.color.x, (float)floatTextMsg.color.y, (float)floatTextMsg.color.z);
            OnCreateFloatText?.Invoke(new FloatText(position, position + UnityEngine.Vector3.up, color, floatTextMsg.text));
        }
        lock (l2RDatas)
            while (l2RDatas.Count > 0)
            {
                var data = l2RDatas.Dequeue();
                switch (data.type)
                {
                    case L2RType.PlayerWandChanged:
                        break;
                    case L2RType.PlayerMagicNodePickListChanged:
                        break;
                    case L2RType.PlayerWandCDUpdate:
                        break;
                    case L2RType.EntityChanged:
                        break;
                    case L2RType.EntityRemoved:
                        break;
                    case L2RType.UpdateUnitEntity:
                        break;
                    case L2RType.RemoveUnitEntity:
                        break;
                    case L2RType.UnitBuffChanged:
                        break;
                    case L2RType.UpdateBuffEntity:
                        break;
                    case L2RType.RemoveBuffEntity:
                        break;
                    case L2RType.UpdateMagicNodeEntity:
                        break;
                    case L2RType.RemoveMagicNodeEntity:
                        break;
                    case L2RType.PlayerBagMagicNodeChanged:
                        break;
                    case L2RType.PlayerWandMagicNodeChanged:
                        break;
                }
            }
        foreach (var entity in entities) entity.Value.OnUpdate(deltaTime);
    }

    public void LateUpdate() { OnLateUpdate?.Invoke(); }

    public void Dispose()
    {
        mapBlock.Dispose();
    }
}
