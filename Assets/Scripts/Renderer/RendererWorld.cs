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

    public LogicPlayerEntity[] players;
    public LogicWorld World { get; private set; }
    public RendererWorld(GameMgr mgr, LoadingProgress loading)
    {
        mapBlock = new MapBlockRenderer(mgr, loading);
        loading.Progress = 1;
    }
    public void Load(LogicWorld world, LoadingProgress loading)
    {
        Unload();
        if (world == null) loading.Progress = 1;
        else
        {
            loading.Progress = .2f;
            World = world;
            lock (world)
            {
                
                world.OnEntityChanged += OnEntityChanged;
                world.OnEntityRemoved += OnEntityRemoved;
                world.OnUpdateUnitEntity += OnUpdateUnitEntity;
                world.OnRemoveUnitEntity += OnRemoveUnitEntity;
                world.OnUpdateBuffEntity += OnUpdateBuffEntity;
                world.OnRemoveBuffEntity += OnRemoveBuffEntity;
                world.OnUpdateMagicNodeEntity += OnUpdateMagicNodeEntity;
                world.OnRemoveMagicNodeEntity += OnRemoveMagicNodeEntity;
                world.OnPlayerBuffChanged += OnPlayerBuffChanged;
                world.OnPlayerBagMagicNodeChanged += OnPlayerBagMagicNodeChanged;
                world.OnPlayerWandMagicNodeChanged += OnPlayerWandMagicNodeChanged;
            }
            loading.Progress = 1;
        }
    }
    public void Unload()
    {
        if (World == null) return;
        World.OnPlayerWandMagicNodeChanged -= OnPlayerWandMagicNodeChanged;
        World.OnPlayerBagMagicNodeChanged -= OnPlayerBagMagicNodeChanged;
        World.OnPlayerBuffChanged -= OnPlayerBuffChanged;
        World.OnRemoveMagicNodeEntity -= OnRemoveMagicNodeEntity;
        World.OnUpdateMagicNodeEntity -= OnUpdateMagicNodeEntity;
        World.OnRemoveBuffEntity -= OnRemoveBuffEntity;
        World.OnUpdateBuffEntity -= OnUpdateBuffEntity;
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
    private void OnPlayerBuffChanged(long player, long buff, bool addition)
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
