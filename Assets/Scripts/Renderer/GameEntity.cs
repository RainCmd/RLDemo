using System;
using UnityEngine;

public class GameEntity
{
    public event Action OnOwnerChanged;
    public long id;
    public long owner;
    private Vector3 forward;
    private Vector3 trgForward;
    private Vector3 position;
    private Vector3 trgPosition;
    private float trgTime;
    public Vector3 Position
    {
        get
        {
            return position;
        }
    }
    public void UpdateTransform(Vector3 forward, Vector3 position, bool immediately)
    {
        trgForward = forward;
        trgPosition = position;
        trgTime = Time.time + 1f / Config.LFPS;
        if (immediately) ImmediatelyTransform();
    }
    private void ImmediatelyTransform()
    {
        forward = trgForward;
        position = trgPosition;
        trgTime = Time.time;
    }
    public void OnUpdate(float deltaTime)
    {
        UpdateMove(deltaTime);
    }
    public void UpdateMove(float deltaTime)
    {
        if (trgTime > Time.time)
        {
            var t = deltaTime / (deltaTime + trgTime - Time.time);
            forward = Vector3.Lerp(forward, trgForward, t);
            position = Vector3.Lerp(position, trgPosition, t);
        }
    }
    public virtual void PlayAnimation(string animation)
    {

    }
    public void OnRemove(bool immediately)
    {

    }
}
