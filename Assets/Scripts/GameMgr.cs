using System;
using System.Collections;
using System.Threading;
using UnityEngine;

public class GameMgr : MonoBehaviour
{
    private bool entryGame = false;
    public event Action OnRenderLateUpdate;
    public Camera GameCamera { get; private set; }
    public Rect CameraArea { get; private set; }
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
        new Thread(() => Logic = new LogicWorld(ctrls, Room.Info.seed, loading.Split(.25f, .8f))).Start();
        while (Logic == null) yield return null;
        Renderer.Load(this, Logic, loading);
    }
    private Vector2 CameraV2P(float x, float y)
    {
        var r = GameCamera.ViewportPointToRay(new Vector3(x, y));
        var d = r.direction;
        d.y = 0;
        return r.origin + r.direction * (d.magnitude / r.direction.y);
    }
    private void Update()
    {
        if (Room == null) return;
        if (Logic != null && Logic.TryDeMsg(out var msg))
        {
            Debug.Log(msg);
        }
        if (entryGame)
        {
            if (GameCamera)
            {
                var p00 = CameraV2P(0, 0);
                var p01 = CameraV2P(0, 1);
                var p11 = CameraV2P(1, 1);
                var p10 = CameraV2P(1, 0);
                var max = Vector2.Max(Vector2.Max(p00, p01), Vector2.Max(p11, p10));
                var min = Vector2.Min(Vector2.Min(p00, p01), Vector2.Min(p11, p10));
                CameraArea = new Rect(min, max - min);
            }
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
    }
    public long GetPlayerId(Guid id)
    {
        if (Room == null) return -1;
        var info = Room.Info;
        if (info.owner.id == id) return Logic.GetPlayerId(info.ctrlId);
        foreach (var item in info.members)
            if (item.player.id == id)
                return Logic.GetPlayerId(item.ctrlId);
        return -1;
    }
    public bool TryGetPlayer(long playerId, out PlayerInfo info)
    {
        if (Room != null)
        {
            var roomInfo = Room.Info;
            var ctrl = Logic.GetCtrlId(playerId);
            if (roomInfo.ctrlId == ctrl)
            {
                info = roomInfo.owner;
                return true;
            }
            foreach (var item in roomInfo.members)
                if (item.ctrlId == ctrl)
                {
                    info = item.player;
                    return true;
                }
        }
        info = default;
        return false;
    }
}
