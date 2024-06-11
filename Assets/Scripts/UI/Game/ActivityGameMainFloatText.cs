using UnityEngine;
using UnityEngine.UI;

public struct FloatText
{
    private Vector3 start, end, ctrl;
    private readonly float startTime, time;
    public readonly Color color;
    public readonly string text;

    public FloatText(Vector3 start, Vector3 end, Vector3 ctrl, float time, Color color, string text)
    {
        this.start = start;
        this.end = end;
        this.ctrl = ctrl;
        startTime = Time.time;
        this.time = time;
        this.color = color;
        this.text = text;
    }
    public FloatText(Vector3 start, Vector3 end, float time, Color color, string text) : this(start, end, (start + end) * .5f, time, color, text) { }
    public FloatText(Vector3 start, Vector3 end, Vector3 ctrl, Color color, string text) : this(start, end, ctrl, 1, color, text) { }
    public FloatText(Vector3 start, Vector3 end, Color color, string text) : this(start, end, 1, color, text) { }

    public Vector3 Position
    {
        get
        {
            var t = (Time.time - startTime) / time;
            return Vector3.Lerp(Vector3.Lerp(start, ctrl, t), Vector3.Lerp(ctrl, end, t), t);
        }
    }
    public bool Active { get { return Time.time > startTime + time; } }
}
public class ActivityGameMainFloatText : MonoBehaviour
{
    public RectTransform rectTransform;
    [SerializeField]
    private Text text;
    public FloatText FloatText { get; private set; }
    public void Init(FloatText floatText)
    {
        FloatText = floatText;
        text.text = floatText.text;
        text.color = floatText.color;
    }
}
