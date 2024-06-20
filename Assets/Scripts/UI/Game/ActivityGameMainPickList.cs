using System.Collections.Generic;
using UnityEngine;

public class ActivityGameMainPickList : MonoBehaviour
{
    [SerializeField]
    private GameObject prefab;
    [SerializeField]
    private Transform content;
    private List<ActivityGameMainPickListItem> list = new List<ActivityGameMainPickListItem>();
    private Stack<ActivityGameMainPickListItem> pool = new Stack<ActivityGameMainPickListItem>();
    private GameMgr gameMgr;
    private PlayerData localPlayerData;
    public void Init(GameMgr gameMgr)
    {
        this.gameMgr = gameMgr;
        if (gameMgr.Renderer.playerDataManager.TryGet(gameMgr.Renderer.playerDataManager.localPlayer, out localPlayerData))
        {
            localPlayerData.PickListChanged += OnPickListUpdate;
            OnPickListUpdate();
        }
    }
    public void UnInit()
    {
        if (localPlayerData != null)
        {
            localPlayerData.PickListChanged -= OnPickListUpdate;
            localPlayerData = null;
        }
    }
    public void OnPickListUpdate()
    {
        if (localPlayerData == null) return;
        while (localPlayerData.pickList.Count < list.Count)
        {
            var item = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            item.gameObject.SetActive(false);
            pool.Push(item);
        }
        while (localPlayerData.pickList.Count > list.Count)
        {
            if (pool.Count > 0) list.Add(pool.Pop());
            else
            {
                var go = Instantiate(prefab);
                go.transform.SetParent(content);
                go.transform.localScale = Vector3.one;
                list.Add(go.GetComponent<ActivityGameMainPickListItem>());
            }
        }
        var idx = 0;
        foreach (var nodeId in localPlayerData.pickList)
        {
            if(gameMgr.Renderer.magicNodes.TryGetValue(nodeId,out var entity))
            {
                list[idx].gameObject.SetActive(true);
                list[idx].Init(gameMgr, entity);
                list[idx].transform.SetAsLastSibling();
            }
            else list[idx].gameObject.SetActive(false);
            idx++;
        }
    }
}
