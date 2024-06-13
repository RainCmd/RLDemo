using UnityEngine;

public class GameEntity
{
    private readonly RendererWorld world;
    private RendererEntity entity;
    public long id;
    private string resource;
    private string anim;
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
    public bool Visiable { get { return entity != null; } }
    public GameEntity(RendererWorld world, LogicEntity entity)
    {
        this.world = world;
        id = entity.id;
        resource = entity.resource;
        anim = entity.anim;
        forward = trgForward = entity.forward.ToVector();
        position = trgForward = entity.position.ToVector();
    }
    public void Update(LogicEntity entity)
    {
        UpdateTransform(entity.forward.ToVector(), entity.position.ToVector(), false);
        if (entity.anim != anim)
        {
            anim = entity.anim;
            this.entity?.PlayAnim(anim);
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
        var lastForward = forward;
        var lastPosition = position;
        if (trgTime > Time.time)
        {
            var t = deltaTime / (deltaTime + trgTime - Time.time);
            forward = Vector3.Lerp(forward, trgForward, t);
            position = Vector3.Lerp(position, trgPosition, t);
        }
        if (world.Mgr.CameraMgr.CameraArea.Contains(new Vector2(position.x, position.z)))
        {
            if (entity == null)
            {
                entity = world.rendererEntityManager.Create(resource);
                entity.PlayAnim(anim);
                entity.SetRotation(Quaternion.LookRotation(forward, Vector3.up));
                entity.SetPosition(position);
            }
            else
            {
                if (lastForward != forward) entity.SetRotation(Quaternion.LookRotation(forward, Vector3.up));
                if (lastPosition != position) entity.SetPosition(position);
            }
        }
        else
        {
            world.rendererEntityManager.Recycle(entity);
            entity = null;
        }
    }
    public virtual void PlayAnimation(string animation)
    {
        entity?.PlayAnim(animation);
    }
    public void OnRemove(bool immediately)
    {
        if (entity != null)
        {
            if (immediately) world.rendererEntityManager.Recycle(entity);
            else entity.Kill();
            entity = null;
        }
    }
}
