using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

public class RendererEntityManager : System.IDisposable
{
    private readonly GameObject root;
    private readonly Transform activeRoot;
    private readonly Transform poolRoot;
    public readonly RendererWorld world;
    private readonly HashSet<RendererEntity> entities = new HashSet<RendererEntity>();
    private readonly Dictionary<string, Stack<RendererEntity>> pool = new Dictionary<string, Stack<RendererEntity>>();
    public RendererEntityManager()
    {
        root = new GameObject("RendererEntities");
        Object.DontDestroyOnLoad(root);
        activeRoot = new GameObject("active").transform;
        activeRoot.SetParent(root.transform, false);
        poolRoot = new GameObject("pool").transform;
        poolRoot.SetParent(root.transform, false);
    }
    private T Load<T>(string resources) where T : Component
    {
        var source = Resources.Load(resources) as GameObject;
        if (!source)
        {
            Debug.LogError("资源加载失败:" + resources);
            return null;
        }
        if (!source.GetComponent<T>())
        {
            Debug.LogError($"资源<color=#ffcc00>{resources}</color>不包含组件:<{typeof(T)}>");
            return null;
        }
        return Object.Instantiate(source).GetComponent<T>();
    }
    private void Init(RendererEntity entity)
    {
        entity.gameObject.SetActive(true);
        entity.transform.SetParent(activeRoot);
        entities.Add(entity);
        entity.Init();
    }
    public RendererEntity Create(string resources)
    {
        if (pool.TryGetValue(resources, out var stack) && stack.Count > 0)
        {
            var result = stack.Pop();
            Init(result);
            return result;
        }
        else
        {
            var result = Load<RendererEntity>(resources);
            if (!result) return null;
            result.OnCreate(this, resources);
            Init(result);
            return result;
        }
    }
    public void Recycle(RendererEntity entity)
    {
        if (entity == null || !entities.Remove(entity)) return;
        entity.Recycle();
        entity.gameObject.SetActive(false);
        entity.transform.SetParent(poolRoot);
        if (pool.TryGetValue(entity.resources, out var stack)) stack.Push(entity);
        else
        {
            stack = new Stack<RendererEntity>();
            stack.Push(entity);
            pool.Add(entity.resources, stack);
        }
    }

    public void Dispose()
    {
        foreach (var entity in entities) entity.Recycle();
        entities.Clear();
        pool.Clear();
        Object.DestroyImmediate(root, true);
    }
}

public abstract class RendererEntity : MonoBehaviour
{
    private static readonly FieldInfo field_manager = typeof(RendererEntity).GetField("manager");
    private static readonly FieldInfo field_resources = typeof(RendererEntity).GetField("resources");
    public readonly RendererEntityManager manager;
    public readonly string resources;
    public void OnCreate(RendererEntityManager manager, string resource)
    {
        field_manager.SetValue(this, manager);
        field_resources.SetValue(this, resource);
    }
    public virtual void SetPosition(Vector3 position) { transform.position = position; }
    public virtual void SetRotation(Quaternion rotation) { transform.rotation = rotation; }
    public void Init()
    {
        OnInit();
    }
    protected virtual void OnInit() { }
    public void PlayAnim(string name) { PlayAnim(name, 0); }
    public virtual void PlayAnim(string name, float time) { }
    public virtual void SetAnimSpeed(float speed) { }
    /// <summary>
    /// 表示生命周期不再受逻辑控制，自行决定回收时机
    /// </summary>
    public virtual void Kill() 
    {
        manager.Recycle(this);
    }
    protected virtual void OnRecycle() { }
    public void Recycle()
    {
        OnRecycle();
    }
}