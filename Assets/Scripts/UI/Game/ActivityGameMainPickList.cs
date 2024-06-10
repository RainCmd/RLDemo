using System.Collections.Generic;
using UnityEngine;

public class ActivityGameMainPickList : MonoBehaviour
{
    [SerializeField]
    private GameObject prefab;
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
                list.Add(go.GetComponent<ActivityGameMainPickListItem>());
            }
        }
        var idx = 0;
        foreach (var item in localPlayerData.pickList)
        {
            list[idx].gameObject.SetActive(true);
            list[idx].Init(gameMgr, item);
            list[idx].transform.SetAsLastSibling();
            idx++;
        }
    }
}
