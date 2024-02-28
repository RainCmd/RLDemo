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
    public readonly Dictionary<long, LogicBuffEntity> buffs = new Dictionary<long, LogicBuffEntity>();
    public readonly Dictionary<long, LogicMagicNodeEntity> magicNodes = new Dictionary<long, LogicMagicNodeEntity>();
    public long LocalPlayerId { get; private set; }
    #region logic=>renderer缓冲区
    private readonly Pipeline<LogicFloatTextMsg> floatTextMsgPipeline = new Pipeline<LogicFloatTextMsg>(4);
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

                foreach (var item in loadResult.entities)
                    EnL2REvent(L2RData.EntityChanged(item.Value));
                foreach (var item in loadResult.units)
                    EnL2REvent(L2RData.UpdateUnitEntity(item.Value));
                foreach (var unitBuffs in loadResult.buffs)
                    foreach (var item in unitBuffs.Value)
                    {
                        EnL2REvent(L2RData.UpdateBuffEntity(item.Value));
                        EnL2REvent(L2RData.UnitBuffChanged(unitBuffs.Key, item.Key, true));
                    }
                foreach (var item in loadResult.magicNodes) 
                    EnL2REvent(L2RData.UpdateMagicNodeEntity(item.Value));
                //todo 加载玩家当前数据：背包，拾取列表，法杖状态，自身状态信息等
                foreach (var player in loadResult.players)
                {
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
        floatTextMsgPipeline.Clear();
        //todo 清理
    }
    private void EnL2REvent(L2RData data)
    {
        lock (l2RDatas) l2RDatas.Enqueue(data);
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
