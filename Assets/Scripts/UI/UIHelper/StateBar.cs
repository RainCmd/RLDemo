using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StateBar : Image
{
    [SerializeField]
    private GameUnitState state;
    public GameUnitState State
    {
        get { return state; }
        set
        {
            state = value;
            SetAllDirty();
        }
    }
    private static List<UIVertex> vertices = new List<UIVertex>();
    protected override void OnPopulateMesh(VertexHelper toFill)
    {
        base.OnPopulateMesh(toFill);
        toFill.GetUIVertexStream(vertices);
        var min = Vector2.one * 999999;
        var max = Vector2.one * -999999;
        foreach (var item in vertices)
        {
            min = Vector2.Min(min, item.position);
            max = Vector2.Max(max, item.position);
        }
        var range = max - min;
        for (int i = 0; i < vertices.Count; i++)
        {
            var v = vertices[i];
            v.uv1 = (Vector2)v.position - min;
            v.uv2 = range;
            v.uv3 = new Vector2(state.cur, state.max);
            vertices[i] = v;
        }
        toFill.Clear();
        toFill.AddUIVertexTriangleStream(vertices);
        vertices.Clear();
    }
}
