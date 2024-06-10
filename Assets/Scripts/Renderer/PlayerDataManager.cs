using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDataManager
{
    public long localPlayer;
    private readonly Dictionary<long, PlayerData> players = new Dictionary<long, PlayerData>();
    public bool TryGet(long player, out PlayerData data)
    {
        return players.TryGetValue(player, out data);
    }
    private PlayerData GetPlayerData(long id)
    {
        if (players.TryGetValue(id, out var player)) return player;
        player = new PlayerData(id);
        players[id] = player;
        return player;
    }
    public void OnPlayerHeroChanged(long player, long hero)
    {
        GetPlayerData(player).OnHeroChanged(hero);
    }
    public void OnPlayerWandChanged(long player, long wand)
    {
        GetPlayerData(player).OnWandChanged(wand);
    }
    public void OnWandCDChanged(long player, long wand, LogicTimeSpan cd)
    {
        GetPlayerData(player).OnWandCDChanged(wand, cd);
    }
    public void OnPickListChanged(long player, long node, bool addition)
    {
        GetPlayerData(player).OnPickListChanged(node, addition);
    }
    public void OnBagListChanged(long player, long node, bool addition)
    {
        GetPlayerData(player).OnBagListChanged(node, addition);
    }
    public void OnWandNodeChanged(long player, long wand, long slot, long node)
    {
        GetPlayerData(player).OnWandNodeChanged(wand, slot, node);
    }
}
public class PlayerData
{
    public readonly long id;
    public long hero;
    public event Action HeroChanged;
    public long wand;
    public event Action WandChanged;
    public LogicTimeSpan[] wandCDs = new LogicTimeSpan[3];
    public event Action<long> WandCDChanged;
    public HashSet<long> pickList = new HashSet<long>();
    public event Action PickListChanged;
    public HashSet<long> bagList = new HashSet<long>();
    public event Action BagListChanged;
    public long[][] wands = { new long[Config.WandSlotSize], new long[Config.WandSlotSize], new long[Config.WandSlotSize] };
    public event Action<long> WandNodeChanged;
    public PlayerData(long id)
    {
        this.id = id;
    }
    public void OnHeroChanged(long hero)
    {
        if (this.hero != hero)
        {
            this.hero = hero;
            HeroChanged?.Invoke();
        }
    }
    public void OnWandChanged(long wand)
    {
        if (this.wand != wand)
        {
            this.wand = wand;
            WandChanged?.Invoke();
        }
    }
    public void OnWandCDChanged(long wand, LogicTimeSpan cd)
    {
        wandCDs[wand] = cd;
        WandCDChanged?.Invoke(wand);
    }
    public void OnPickListChanged(long node, bool addition)
    {
        if (addition) pickList.Add(node);
        else pickList.Remove(node);
        PickListChanged?.Invoke();
    }
    public void OnBagListChanged(long node, bool addition)
    {
        if (addition) bagList.Add(node);
        else bagList.Remove(node);
        BagListChanged?.Invoke();
    }
    public void OnWandNodeChanged(long wand, long slot, long node)
    {
        wands[wand][slot] = node;
        WandNodeChanged?.Invoke(wand);
    }
}