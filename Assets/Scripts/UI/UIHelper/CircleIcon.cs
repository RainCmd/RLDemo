using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CircleIcon : Image
{
    [SerializeField, Range(0, 1)]
    private float grayscale = 0;
    public float Grayscale
    {
        get { return grayscale; }
        set
        {
            grayscale = value;
            GetComponent<Image>()?.SetVerticesDirty();
        }
    }
    private static List<UIVertex> vertices = new List<UIVertex>();
    protected override void OnPopulateMesh(VertexHelper toFill)
    {
        base.OnPopulateMesh(toFill);
        if (toFill.currentVertCount > 0)
        {
            toFill.GetUIVertexStream(vertices);
            var center = Vector2.zero;
            var min = Vector2.zero;
            for (int i = 0; i < vertices.Count; i++)
            {
                center += vertices[i].uv0;
                min = Vector2.Min(min, vertices[i].uv0);
            }
            center /= vertices.Count;
            var ratio = new Vector2(Mathf.Min(1 / (center.x - min.x), 1 / (center.y - min.y)), grayscale);
            for (int i = 0; i < vertices.Count; i++)
            {
                var vertex = vertices[i];
                vertex.uv1 = center;
                vertex.uv2 = ratio;
                vertices[i] = vertex;
            }
            toFill.Clear();
            toFill.AddUIVertexTriangleStream(vertices);
            vertices.Clear();
        }
    }
}
