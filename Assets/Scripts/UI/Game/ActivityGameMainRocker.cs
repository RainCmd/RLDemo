using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ActivityGameMainRocker : MonoBehaviour
{
    public float limitDistance = 300;
    public RectTransform rocker;
    public Image arrow;
    private RectTransform rt;
    private EventTrigger trigger;
    public Action<float, float> OnRock;
    private void AddEvent(EventTriggerType type, UnityAction<BaseEventData> action)
    {
        var entry = new EventTrigger.Entry();
        entry.eventID = type;
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }
    private void Awake()
    {
        rt = transform as RectTransform;
        trigger = GetComponent<EventTrigger>();
        if (!trigger) trigger = gameObject.AddComponent<EventTrigger>();
        AddEvent(EventTriggerType.Drag, OnDrag);
        AddEvent(EventTriggerType.EndDrag, OnEndDrag);
    }
    private void OnDrag(BaseEventData data)
    {
        if (data is PointerEventData pointer)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, pointer.position, null, out var pos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, pointer.pressPosition, null, out var ppos);
            var d = pos - ppos;
            if (d.sqrMagnitude > limitDistance * limitDistance)
            {
                d = d.normalized * limitDistance;
            }
            var a = Mathf.Atan2(d.y, d.x);
            var r = d.magnitude / limitDistance;
            rocker.anchoredPosition = ppos + d;
            rocker.rotation = Quaternion.AngleAxis(a * Mathf.Rad2Deg, Vector3.forward);
            SetArrowAlpha(r);
            OnRock?.Invoke(a, r);
        }
    }
    private void OnEndDrag(BaseEventData data)
    {
        if (data is PointerEventData pointer)
        {
            OnRock?.Invoke(0, 0);
            rocker.anchoredPosition = Vector2.zero;
            SetArrowAlpha(0);
        }
    }
    private void SetArrowAlpha(float alpha)
    {
        var color = arrow.color;
        color.a = alpha;
        arrow.color = color;
    }
}

