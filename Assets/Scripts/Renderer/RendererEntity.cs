using UnityEngine;

public abstract class RendererEntity : MonoBehaviour
{
    public RendererWorld World { get; private set; }
    public string Resource { get; private set; }
    public RendererEntity(RendererWorld world, string resource)
    {
        World = world;
        Resource = resource;
    }
    public virtual void SetPosition(Vector3 position) { }
    public virtual void SetRotation(Quaternion rotation) { }
    public void Init()
    {
        OnInit();
    }
    protected virtual void OnInit() { }
    public void PlayAnim(string name) { PlayAnim(name, 0); }
    public virtual void PlayAnim(string name, float time) { }
    public virtual void SetAnimSpeed(float speed) { }
    public abstract void OnRecycle(bool immediately);
    protected void Recycle()
    {
        World.RecycleRendererEntity(this);
    }
    public abstract void OnDestroy();
}