using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameLog : MonoBehaviour
{
    private struct Log
    {
        public Color color;
        public string msg;
        public Log(Color color, string msg)
        {
            this.color = color;
            this.msg = msg;
        }
    }
    private struct Msg
    {
        public Text text;
        public float time;
        public Msg(Text text)
        {
            this.text = text;
            time = 0;
        }
        public void SetLog(Log log)
        {
            time = Time.time + Mathf.Pow(log.msg.Length, .8f);
            text.gameObject.SetActive(true);
            text.text = log.msg;
            text.color = log.color;
            var sd = text.rectTransform.sizeDelta;
            sd.y = text.preferredHeight;
            text.rectTransform.sizeDelta = sd;
        }
        public bool Update(Stack<Msg> pool)
        {
            var d = Time.time - time;
            if (d > 1)
            {
                pool.Push(this);
                text.gameObject.SetActive(false);
                return true;
            }
            else if (d > 0)
            {
                var color = text.color;
                color.a = 1 - d * d;
                text.color = color;
            }
            return false;
        }
    }
    public GameObject prefab;
    private List<Msg> msgs = new List<Msg>();
    private Stack<Msg> pool = new Stack<Msg>();
    private void Update()
    {
        lock (logs)
        {
            foreach (var log in logs) Show(log);
            logs.Clear();
        }
        msgs.RemoveAll(msg => !msg.Update(pool));
    }
    private void Show(Log log)
    {
        var text = pool.Count > 0 ? pool.Pop() : new Msg(Instantiate(prefab, transform).GetComponent<Text>());
        text.SetLog(log);
        text.text.transform.SetAsLastSibling();
    }
    private static Queue<Log> logs = new Queue<Log>();
    public static void Show(Color color, string msg)
    {
        lock (logs) { logs.Enqueue(new Log(color, msg)); }
    }
}
