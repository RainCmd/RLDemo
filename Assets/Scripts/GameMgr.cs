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
        new Thread(() => Logic = new LogicWorld(this, loading.Split(.25f, .8f))).Start();
        while (Logic == null) yield return null;
        Renderer.Load(Logic, loading);
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
            Logic.EntryGame();
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
    public bool TryGetPlayer(long playerId, out PlayerInfo info)
    {
        if (Room != null)
        {
            if (playerId == 0)
            {
                info = Room.Info.owner;
                return true;
            }
            else if (playerId < Room.Info.members.Count + 1)
            {
                info = Room.Info.members[(int)playerId - 1].player;
                return true;
            }
        }
        info = default;
        return false;
    }
}
