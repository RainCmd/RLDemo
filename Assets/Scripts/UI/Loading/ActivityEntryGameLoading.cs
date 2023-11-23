using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public struct LoadingProgress
{
    private readonly Action<float> action;
    private readonly float start, end;
    private float progress;
    public float Progress
    {
        get { return progress; }
        set
        {
            value = Mathf.Clamp01(value);
            if (progress != value)
            {
                progress = value;
                action?.Invoke(Mathf.Lerp(start, end, progress));
            }
        }
    }
    public LoadingProgress(Action<float> action, float start, float end)
    {
        this.action = action;
        this.start = start;
        this.end = end;
        progress = 0;
    }
    private float TotalProgress(float progress)
    {
        return Mathf.Lerp(start, end, progress);
    }
    public LoadingProgress Split(float start, float end)
    {
        return new LoadingProgress(action, TotalProgress(start), TotalProgress(end));
    }
    public static LoadingProgress Create(Action<float> action)
    {
        return new LoadingProgress(action, 0, 1);
    }
}
public class ActivityEntryGameLoading : UIActivity
{
    public RectTransform content;
    public GameObject playerPrefab;
    private List<ActivityEntryGameLoadingItem> players;
    private IRoom room;
    private void Awake()
    {
        players = new List<ActivityEntryGameLoadingItem>();
    }
    public void Init(IRoom room)
    {
        this.room = room;
        room.OnRemovePlayerInfo += OnRemovePlayerInfo;
        room.OnUpdateMember += OnUpdateMember;
        room.OnUpdatePlayerLoading += OnUpdatePlayerLoading;
        CreatePlayer(new RoomInfo.MemberInfo(room.Info.owner, true, 0));
        lock (room.Info.members)
            foreach (var item in room.Info.members)
                CreatePlayer(item);
    }
    private void Deinit()
    {
        room.OnUpdatePlayerLoading -= OnUpdatePlayerLoading;
        room.OnUpdateMember -= OnUpdateMember;
        room.OnRemovePlayerInfo -= OnRemovePlayerInfo;
    }
    private void CreatePlayer(RoomInfo.MemberInfo info)
    {
        var player = Instantiate(playerPrefab, content).GetComponent<ActivityEntryGameLoadingItem>();
        player.gameObject.SetActive(true);
        player.Init(info);
        lock (players) players.Add(player);
    }
    private void OnUpdatePlayerLoading(Guid id, float progress)
    {
        lock (players) players.Find(v => v.member.player.id == id)?.UpdateProgress(progress);
    }
    private void OnUpdateMember(RoomInfo.MemberInfo member)
    {
        Do(() =>
        {
            lock (players) players.Find(v => v.member.player.id == member.player.id)?.UpdateInfo(member);
        });
    }
    private void OnRemovePlayerInfo(Guid id)
    {
        Do(() =>
        {
            lock (players) players.Find(v => v.member.player.id == id)?.OnLeave();
        });
    }
    public override void OnDelete()
    {
        if (room != null)
        {
            Deinit();
            room.Dispose();
            room = null;
        }
    }
}
