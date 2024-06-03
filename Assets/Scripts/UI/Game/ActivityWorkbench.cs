using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ActivityWorkbench : UIActivity
{
    public GameObject operableNodePrefab;
    public RectTransform bagViewport;
    public RectTransform bagContent;
    private List<ActivityWorkbenchOperableNode> bagNodes = new List<ActivityWorkbenchOperableNode>();
    public RectTransform[] wandViewport;
    public RectTransform[] wandContents;
    public RectTransform dropArea;
    public GameObject weaponSlotPrefab;
    private List<ActivityWorkbenchWeaponSlot>[] weaponSlots = new List<ActivityWorkbenchWeaponSlot>[3];
    [SerializeField]
    private RectTransform poolRoot;
    private GameMgr gameMgr;
    private Stack<ActivityWorkbenchOperableNode> nodePool = new Stack<ActivityWorkbenchOperableNode>();
    public void Init(GameMgr gameMgr)
    {
        this.gameMgr = gameMgr;
        for (int i = 0; i < 3; i++)
        {
            var slots = weaponSlots[i] = new List<ActivityWorkbenchWeaponSlot>();
            for (var c = 0; c < Config.WandSlotSize; c++)
            {
                var go = Instantiate(weaponSlotPrefab);
                go.transform.SetParent(wandContents[i], false);
                go.gameObject.SetActive(true);
                var slot = go.GetComponent<ActivityWorkbenchWeaponSlot>();
                slot.Init(this, i, c);
                slots.Add(slot);
            }
            RefreshWeapon(i);
        }
        RefreshBag();
        gameMgr.Renderer.OnWandUpdate += RefreshWeapon;
        gameMgr.Renderer.OnBagListUpdate += RefreshBag;
    }
    public override void OnDelete()
    {
        gameMgr.Renderer.OnBagListUpdate -= RefreshBag;
        gameMgr.Renderer.OnWandUpdate -= RefreshWeapon;
    }
    private void RefreshWeapon(int wandId)
    {
        var slots = weaponSlots[wandId];
        var wand = gameMgr.Renderer.wands[wandId];
        for (int i = 0; i < wand.Length; i++)
        {
            slots[i].SetNode(wand[i]);
        }
    }
    public void RefreshBag()
    {
        var bag = gameMgr.Renderer.bagList;
        while (bag.Count > bagNodes.Count)
        {
            bagNodes.Add(GetNode(bagContent));
        }
        while (bag.Count < bagNodes.Count)
        {
            RecyleNode(bagNodes[bagNodes.Count - 1]);
            bagNodes.RemoveAt(bagNodes.Count - 1);
        }
        var idx = 0;
        foreach (var node in bag)
        {
            bagNodes[idx++].SetNode(node);
        }
    }
    public void OnClickWandDetail(int wandId)
    {

    }
    private ActivityWorkbenchOperableNode draggedNode;
    private void OnBeginDragNode(PointerEventData data)
    {
        var src = data.pointerDrag?.GetComponent<ActivityWorkbenchOperableNode>();
        if (src)
        {
            draggedNode = GetNode(transform);
            draggedNode.SetNode(src.nodeId);
            draggedNode.transform.position = data.position;
        }
    }
    private void OnDragNode(PointerEventData data)
    {
        if (draggedNode) draggedNode.transform.position = data.position;
    }
    private void OnEndDragNode(PointerEventData data)
    {
        if (draggedNode)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(bagViewport, data.position))
            {
                if (!gameMgr.Renderer.bagList.Contains(draggedNode.nodeId))
                {
                    gameMgr.Room.UpdateOperator(Operator.Pick(draggedNode.nodeId));
                }
            }
            else if (RectTransformUtility.RectangleContainsScreenPoint(dropArea, data.position))
            {
                gameMgr.Room.UpdateOperator(Operator.Drop(draggedNode.nodeId));
            }
            else
            {
                for (int i = 0; i < wandViewport.Length; i++)
                {
                    if (RectTransformUtility.RectangleContainsScreenPoint(wandViewport[i], data.position))
                    {
                        var content = wandContents[i];
                        for (int j = 0; j < content.childCount; j++)
                        {
                            var rt = content.GetChild(j) as RectTransform;
                            if (RectTransformUtility.RectangleContainsScreenPoint(rt, data.position))
                            {
                                var slot = rt.GetComponent<ActivityWorkbenchWeaponSlot>();
                                gameMgr.Room.UpdateOperator(Operator.Equip(draggedNode.nodeId, slot.wand, slot.slot));
                                break;
                            }
                        }
                        break;
                    }
                }
            }
            RecyleNode(draggedNode);
            draggedNode = null;
        }
    }
    private void RegNodeDragEvent(ActivityWorkbenchOperableNode node)
    {
        node.BeginDrag.AddListener(OnBeginDragNode);
        node.Drag.AddListener(OnDragNode);
        node.EndDrag.AddListener(OnEndDragNode);
    }
    public ActivityWorkbenchOperableNode GetNode(Transform parent)
    {
        if (nodePool.Count > 0)
        {
            var result = nodePool.Pop();
            result.Init(gameMgr, parent);
            RegNodeDragEvent(result);
            return result;
        }
        else
        {
            var go = Instantiate(operableNodePrefab);
            var result = go.GetComponent<ActivityWorkbenchOperableNode>();
            result.Init(gameMgr, parent);
            RegNodeDragEvent(result);
            return result;
        }
    }
    public void RecyleNode(ActivityWorkbenchOperableNode node)
    {
        if (!node) throw new ArgumentNullException("node");
        node.OnRecycle();
        node.transform.SetParent(poolRoot, false);
        nodePool.Push(node);
    }
}
