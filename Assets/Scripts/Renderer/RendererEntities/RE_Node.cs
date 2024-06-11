using UnityEngine;

public class RE_Node : RendererEntity
{
    private void Update()
    {
        var rotate = Quaternion.AngleAxis(Time.deltaTime * Mathf.PI * 4, Vector3.up);
        transform.rotation = rotate * transform.rotation;
    }
}
