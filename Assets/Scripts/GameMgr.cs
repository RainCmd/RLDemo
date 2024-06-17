﻿using System;
using System.Collections;
using System.Threading;
using UnityEngine;

public class GameMgr : MonoBehaviour
{
    private bool entryGame = false;
    public event Action OnRenderLateUpdate;
    private Thread loadLogicThread;
    private CameraMgr cameraMgr;
    public CameraMgr CameraMgr
    {
        get
        {
            if (cameraMgr == null) cameraMgr = new CameraMgr(Camera.main);
            return cameraMgr;
        }
    }
    public IRoom Room { get; private set; }
    public LogicWorld Logic { get; private set; }
    public RendererWorld Renderer { get; private set; }
    private float loadingProgress = 0;
    public void Init(IRoom room)
    {
        Room = room;
        UIManager.Show<ActivityEntryGameLoading>("EntryGameLoading").Init(room);
        room.OnEntryGame += OnEntryGame;
        LogicConfig.LoadConfigs();
        StartCoroutine(StartLoading());
    }
    private IEnumerator StartLoading()
    {
        var loading = LoadingProgress.Create(v => loadingProgress = v);
        Renderer = new RendererWorld(this, loading.Split(0, .25f));
        var ctrls = new long[Room.Info.members.Count + 1];
        ctrls[0] = Room.Info.ctrlId;
        for (int i = 0; i < Room.Info.members.Count; i++)
            ctrls[i + 1] = Room.Info.members[i].ctrlId;
        loadLogicThread = new Thread(() => Logic = new LogicWorld(ctrls, Room.Info.seed, loading.Split(.25f, .8f)));
        loadLogicThread.Start();
        while (Logic == null) yield return null;
        loadLogicThread = null;
        Renderer.Load(this, Logic, loading);
    }
    private void Update()
    {
        Logic?.ShowDebugLine();
        if (Room == null) return;
        if (entryGame)
        {
            CameraMgr?.Update();
            Renderer.Update(Time.deltaTime);
        }
        else Room.UpdateLoading(loadingProgress);
    }
    private void LateUpdate()
    {
        Renderer.LateUpdate();
    }
    private void OnEntryGame()
    {
        UIManager.Do(() =>
        {
            UIManager.CloseAll();
            entryGame = true;
            Logic.EntryGame(Room);
            UIManager.Show<ActivityGameMain>("GameMain").Init(this);
        });
    }
    private void OnDestroy()
    {
        Room.OnEntryGame -= OnEntryGame;
        Room = null;
        Renderer.Dispose();
        Renderer = null;
        Logic.Dispose();
        Logic = null;
        loadLogicThread?.Abort();
    }
    public long GetPlayerId(Guid id)
    {
        if (Room == null) return -1;
        var info = Room.Info;
        if (info.owner.id == id) return info.ctrlId;
        foreach (var item in info.members)
            if (item.player.id == id)
                return item.ctrlId;
        return -1;
    }
    public bool TryGetPlayer(long playerId, out PlayerInfo info)
    {
        if (Room != null)
        {
            var roomInfo = Room.Info;
            if (roomInfo.ctrlId == playerId)
            {
                info = roomInfo.owner;
                return true;
            }
            foreach (var item in roomInfo.members)
                if (item.ctrlId == playerId)
                {
                    info = item.player;
                    return true;
                }
        }
        info = default;
        return false;
    }
}
