using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;

public class ActivityWorkbenchOperableNode : MonoBehaviour
{
    [System.Serializable]
    public class NodeDragEvent : UnityEvent<PointerEventData> { }
    private GameMgr mgr;
    public long nodeId;
    public Image icon;
    public Image type;
    public Text num;
    public EventTrigger trigger;
    private ScrollRect scroll;
    private float time;
    private bool dragSelf = false;
    private ScrollRect GetScrollRect()
    {
        if (scroll == null)
        {
            var t = transform;
            while (t)
            {
                scroll = t.GetComponent<ScrollRect>();
                if (scroll) break;
                t = t.parent;
            }
        }
        return scroll;
    }
    public NodeDragEvent BeginDrag = new NodeDragEvent();
    public NodeDragEvent Drag = new NodeDragEvent();
    public NodeDragEvent EndDrag = new NodeDragEvent();
    private void RegTrg(EventTriggerType type, UnityAction<BaseEventData> cb)
    {
        var entity = new EventTrigger.Entry() { eventID = type };
        entity.callback.AddListener(cb);
        trigger.triggers.Add(entity);
    }
    public void Init(GameMgr mgr, Transform parent)
    {
        this.mgr = mgr;
        transform.SetParent(parent);
        transform.localScale = Vector3.one;
        var rt = transform as RectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
        rt.localPosition = Vector3.zero;
        var scroll = GetScrollRect();
        if (scroll)
        {
            RegTrg(EventTriggerType.PointerDown, data => OnPointDown());
            RegTrg(EventTriggerType.InitializePotentialDrag, data => OnInitializePotentialDrag(data as PointerEventData));
            RegTrg(EventTriggerType.BeginDrag, data => OnBeginDrag(data as PointerEventData));
            RegTrg(EventTriggerType.Drag, data => OnDrag(data as PointerEventData));
            RegTrg(EventTriggerType.EndDrag, data => OnEndDrag(data as PointerEventData));
        }
    }
    public void SetNode(long nodeId)
    {
        this.nodeId = nodeId;
        gameObject.SetActive(nodeId != 0);
        if (mgr.Renderer.magicNodes.TryGetValue(nodeId, out var node))
        {
            if (LogicConfig.magicNodes.TryGet(item => item.id == node.configId, out var cfg))
            {
                icon.sprite = Config.NodeIconList[(int)cfg.icon];
                type.sprite = Config.MagicNodeTypeIcons[(int)cfg.type];
                num.text = node.number.ToString();
                num.gameObject.SetActive(node.number > 0);
            }
            else Debug.LogError($"节点id:{nodeId} 的configID:{node.configId} 未找到对应的配置");
        }
    }
    private void OnPointDown()
    {
        time = Time.time;
        dragSelf = false;
    }
    private void OnInitializePotentialDrag(PointerEventData data)
    {
        scroll?.OnInitializePotentialDrag(data);
    }
    private void OnBeginDrag(PointerEventData data)
    {
        dragSelf = Time.time - time > .1;
        data.pointerDrag = gameObject;
        if (dragSelf) BeginDrag.Invoke(data);
        else scroll.OnBeginDrag(data);
    }
    private void OnDrag(PointerEventData data)
    {
        data.pointerDrag = gameObject;
        if (dragSelf) Drag.Invoke(data);
        else scroll.OnDrag(data);
    }
    private void OnEndDrag(PointerEventData data)
    {
        data.pointerDrag = gameObject;
        if (dragSelf) EndDrag.Invoke(data);
        else scroll.OnEndDrag(data);
    }
    public void OnRecycle()
    {
        BeginDrag.RemoveAllListeners();
        Drag.RemoveAllListeners();
        EndDrag.RemoveAllListeners();
        trigger.triggers.Clear();
        scroll = null;
    }
}
