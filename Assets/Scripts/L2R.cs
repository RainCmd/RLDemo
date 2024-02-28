public enum L2RType
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
public struct L2RData
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
    public static L2RData PlayerWandChanged(long player, long wand)
    {
        return new L2RData()
        {
            type = L2RType.PlayerWandChanged,
            player = player,
            wand = wand
        };
    }
    public static L2RData PlayerMagicNodePickListChanged(long player, long nodeId, bool addition)
    {
        return new L2RData()
        {
            type = L2RType.PlayerMagicNodePickListChanged,
            player = player,
            node = new LogicMagicNodeEntity() { id = nodeId },
            addition = addition
        };
    }
    public static L2RData PlayerWandCDUpdate(long player, long wand, LogicTimeSpan cd)
    {
        return new L2RData
        {
            type = L2RType.PlayerWandCDUpdate,
            player = player,
            wand = wand,
            cd = cd
        };
    }
    public static L2RData EntityChanged(LogicEntity entity)
    {
        return new L2RData()
        {
            type = L2RType.EntityChanged,
            entity = entity,
            addition = true
        };
    }
    public static L2RData EntityRemoved(long id, bool immediately)
    {
        return new L2RData()
        {
            type = L2RType.EntityRemoved,
            entity = new LogicEntity() { id = id },
            immediately = immediately,
            addition = false
        };
    }
    public static L2RData UpdateUnitEntity(LogicUnitEntity unit)
    {
        return new L2RData()
        {
            type = L2RType.UpdateUnitEntity,
            unit = unit,
            addition = true
        };
    }
    public static L2RData RemoveUnitEntity(long id)
    {
        return new L2RData()
        {
            type = L2RType.RemoveUnitEntity,
            unit = new LogicUnitEntity() { id = id },
            addition = false
        };
    }
    public static L2RData UnitBuffChanged(long unit, long buff, bool addition)
    {
        return new L2RData()
        {
            type = L2RType.UnitBuffChanged,
            unit = new LogicUnitEntity() { id = unit },
            buff = new LogicBuffEntity() { id = buff },
            addition = addition
        };
    }
    public static L2RData UpdateBuffEntity(LogicBuffEntity buff)
    {
        return new L2RData()
        {
            type = L2RType.UpdateBuffEntity,
            buff = buff,
            addition = true
        };
    }
    public static L2RData RemoveBuffEntity(long id)
    {
        return new L2RData()
        {
            type = L2RType.RemoveBuffEntity,
            buff = new LogicBuffEntity() { id = id },
            addition = false
        };
    }
    public static L2RData UpdateMagicNodeEntity(LogicMagicNodeEntity node)
    {
        return new L2RData()
        {
            type = L2RType.UpdateMagicNodeEntity,
            node = node,
            addition = true
        };
    }
    public static L2RData RemoveMagicNodeEntity(long id)
    {
        return new L2RData()
        {
            type = L2RType.RemoveMagicNodeEntity,
            node = new LogicMagicNodeEntity() { id = id },
            addition = false
        };
    }
    public static L2RData PlayerBagMagicNodeChanged(long player, long node, bool addition)
    {
        return new L2RData()
        {
            type = L2RType.PlayerBagMagicNodeChanged,
            player = player,
            node = new LogicMagicNodeEntity() { id = node },
            addition = addition
        };
    }
    public static L2RData PlayerWandMagicNodeChanged(long player, long wand, long node, long slot)
    {
        return new L2RData()
        {
            type = L2RType.PlayerWandMagicNodeChanged,
            player = player,
            wand = wand,
            slot = slot,
            node = new LogicMagicNodeEntity() { id = node }
        };
    }
}