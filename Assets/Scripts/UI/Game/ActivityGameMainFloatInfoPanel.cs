using System.Collections.Generic;
using UnityEngine;
public class ActivityGameMainFloatInfoPanel : MonoBehaviour
{
    public ActivityGameMain main;
    public GameObject playerPrefab;
    public GameObject npcPrefab;
    public GameObject floatTextPrefab;
    private RectTransform rt;
    private List<GameUnit> entities;
    private HashSet<long> removes;
    private Dictionary<long, ActivityGameMainPlayerFloatInfo> playerInfos;
    private Dictionary<long, ActivityGameMainNpcFloatInfo> npcInfos;
    private Stack<ActivityGameMainPlayerFloatInfo> playerPool;
    private Stack<ActivityGameMainNpcFloatInfo> npcPool;
    private List<ActivityGameMainFloatText> floatTexts;
    private Stack<ActivityGameMainFloatText> floatTextPool;
    private void Awake()
    {
        rt = transform as RectTransform;
        entities = new List<GameUnit>();
        removes = new HashSet<long>();
        playerInfos = new Dictionary<long, ActivityGameMainPlayerFloatInfo>();
        npcInfos = new Dictionary<long, ActivityGameMainNpcFloatInfo>();
        playerPool = new Stack<ActivityGameMainPlayerFloatInfo>();
        npcPool = new Stack<ActivityGameMainNpcFloatInfo>();
        floatTexts = new List<ActivityGameMainFloatText>();
        floatTextPool = new Stack<ActivityGameMainFloatText>();
    }
    public void ShowFloatText(FloatText text)
    {
        if (TryGetViewportPoint(text.Position, out var vp))
        {
            var ft = floatTextPool.Count > 0 ? floatTextPool.Pop() : Instantiate(floatTextPrefab, transform).GetComponent<ActivityGameMainFloatText>();
            ft.gameObject.SetActive(true);
            ft.Init(text);
            UpdatePosition(ft.rectTransform, vp);
            floatTexts.Add(ft);
        }
    }
    public void CreateInfo(GameUnit unit)
    {
        if (removes.Remove(unit.entity.id)) return;
        entities.Add(unit);
    }
    public void RemoveInfo(long entity)
    {
        removes.Add(entity);
    }
    private void Recycle(long entity)
    {
        if (playerInfos.TryGetValue(entity, out var player))
        {
            player.gameObject.SetActive(false);
            playerPool.Push(player);
            playerInfos.Remove(entity);
        }
        else if (npcInfos.TryGetValue(entity, out var npc))
        {
            npc.gameObject.SetActive(false);
            npcPool.Push(npc);
            npcInfos.Remove(entity);
        }
    }
    private bool TryGetViewportPoint(Vector3 worldPosition, out Vector3 viewportPoint)
    {
        viewportPoint = main.Manager.GameCamera.WorldToViewportPoint(worldPosition);
        return viewportPoint.x > 0 && viewportPoint.x < 1 && viewportPoint.y > 0 && viewportPoint.y < 1;
    }
    private void UpdatePosition(RectTransform rt, Vector2 viewPosition)
    {
        var rect = this.rt.rect;
        rt.anchoredPosition = rect.position + Vector2.Scale(rect.size, viewPosition);
    }
    private void ShowEntity(GameUnit unit, Vector2 viewPosisition)
    {
        switch (unit.UnitType)
        {
            case UnitType.Player:
                if (!playerInfos.TryGetValue(unit.entity.id, out var player))
                {
                    player = playerPool.Count > 0 ? playerPool.Pop() : Instantiate(playerPrefab, transform).GetComponent<ActivityGameMainPlayerFloatInfo>();
                    playerInfos.Add(unit.entity.id, player);
                    player.Init(main.Manager, unit);
                }
                UpdatePosition(player.RectTransform, viewPosisition);
                break;
            case UnitType.Npc:
            case UnitType.NpcNoMana:
                if (!npcInfos.TryGetValue(unit.entity.id, out var npc))
                {
                    npc = npcPool.Count > 0 ? npcPool.Pop() : Instantiate(npcPrefab, transform).GetComponent<ActivityGameMainNpcFloatInfo>();
                    npcInfos.Add(unit.entity.id, npc);
                    npc.Init(main.Manager, unit);
                }
                UpdatePosition(npc.RectTransform, viewPosisition);
                break;
        }
    }
    public void UpdatePanel()
    {
        entities.RemoveAll(unit =>
        {
            var id = unit.entity.id;
            if (removes.Contains(id))
            {
                Recycle(id);
                return true;
            }
            else
            {
                if (unit.VisableFloatInfo && TryGetViewportPoint(unit.FloatInfoPosition, out var vp)) ShowEntity(unit, vp);
                else Recycle(id);
                return false;
            }
        });
        removes.Clear();
        floatTexts.RemoveAll(text =>
        {
            if (TryGetViewportPoint(text.FloatText.Position, out var vp))
            {
                UpdatePosition(text.rectTransform, vp);
                if (!text.FloatText.Active) return false;
            }
            text.gameObject.SetActive(false);
            floatTextPool.Push(text);
            return true;
        });
    }
}
