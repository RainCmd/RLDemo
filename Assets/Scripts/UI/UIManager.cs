using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class UIActivity : MonoBehaviour
{
    public void Do(Action task)
    {
        UIManager.Do(task);
    }
    public virtual void OnCreate() { }
    public virtual void OnDelete() { }
    public virtual void OnShow() { }
    public virtual void OnHide() { }
    public T Find<T>(string name) where T : UIActivity
    {
        return UIManager.Find(name) as T;
    }
    public void Show(string name)
    {
        UIManager.Show(name);
    }
    public T Show<T>(string name) where T : UIActivity
    {
        return UIManager.Show(name) as T;
    }
    public void Close()
    {
        UIManager.Close(this);
    }
}
public class UIManager : MonoBehaviour
{
    public GameObject eventSystem;
    public Transform gameLog;
    private Queue<Action> tasks = new Queue<Action>();
    private Queue<Action> tasksExe = new Queue<Action>();
    private void Awake()
    {
        uiThread = Thread.CurrentThread;
        DontDestroyOnLoad(eventSystem);
        DontDestroyOnLoad(gameObject);
        manager = this;
        Show("Hall");
    }
    private void Update()
    {
        lock (tasks)
        {
            var tmp = tasks;
            tasks = tasksExe;
            tasksExe = tmp;
        }
        while (tasksExe.Count > 0) tasksExe.Dequeue()();
    }
    private void OnDestroy()
    {
        var cnt = activities.Count;
        while (cnt-- > 0)
        {
            if (activities[cnt].gameObject.activeSelf) activities[cnt].OnHide();
            activities[cnt].OnDelete();
        }
    }
    private static UIManager manager;
    private static readonly List<UIActivity> activities = new List<UIActivity>();
    public static UIActivity Find(string name)
    {
        return activities.Find(v => v.name == name);
    }
    public static T Show<T>(string name, bool createNew = true) where T : UIActivity
    {
        return Show(name, createNew) as T;
    }
    public static UIActivity Show(string name, bool createNew = true)
    {
        if (!createNew)
        {
            var idx = activities.FindIndex(v => v.name == name);
            if (idx >= 0)
            {
                var result = activities[idx];
                activities.RemoveAt(idx);
                if (activities.Count > 0)
                {
                    var current = activities[activities.Count - 1];
                    if (current.gameObject.activeSelf)
                    {
                        current.OnHide();
                        current.gameObject.SetActive(false);
                    }
                }
                result.transform.SetAsLastSibling();
                activities.Add(result);
                manager.gameLog.SetAsLastSibling();
                if (!result.gameObject.activeSelf)
                {
                    result.gameObject.SetActive(true);
                    result.OnShow();
                }
                return result;
            }
        }
        var prefab = Resources.Load<GameObject>("Prefabs/UI/" + name);
        if (prefab && prefab.GetComponent<UIActivity>())
        {
            var result = Instantiate(prefab, manager.transform).GetComponent<UIActivity>();
            if (activities.Count > 0)
            {
                var prev = activities[activities.Count - 1];
                prev.OnHide();
                prev.gameObject.SetActive(false);
            }
            activities.Add(result);
            manager.gameLog.SetAsLastSibling();
            result.name = name;
            result.gameObject.SetActive(true);
            result.OnCreate();
            result.OnShow();
            return result;
        }
        return null;
    }
    public static void Close(UIActivity activity)
    {
        var idx = activities.IndexOf(activity);
        if (idx >= 0)
        {
            if (activity.gameObject.activeSelf) activity.OnHide();
            activity.OnDelete();
            DestroyImmediate(activity.gameObject);
            activities.RemoveAt(idx);
            if (idx == activities.Count && idx > 0)
            {
                activity = activities[idx - 1];
                activity.OnShow();
                activity.gameObject.SetActive(true);
            }
        }
    }
    public static void CloseAll()
    {
        while (activities.Count > 0) Close(activities[activities.Count - 1]);
    }
    public static void Do(Action task)
    {
        lock (manager.tasks) manager.tasks.Enqueue(task);
    }
    private class LoadResourceHelper
    {
        public byte[] data = null;
        public bool finish = false;
    }
    private static Thread uiThread = null;
    public static byte[] LoadResource(string resouce, float timeout = -1)
    {
        var helper = new LoadResourceHelper();
        Action action = () =>
        {
            var asset = Resources.Load<TextAsset>(resouce);
            if (asset) helper.data = asset.bytes;
            helper.finish = true;
        };
        if (Thread.CurrentThread == uiThread) action();
        else Do(action);
        while (!helper.finish)
        {
            Thread.Sleep(100);
            if (timeout > 0)
            {
                timeout -= .1f;
                if (timeout < 0) break;
            }
        }
        return helper.data;
    }
}
