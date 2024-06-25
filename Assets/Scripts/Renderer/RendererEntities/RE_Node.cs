using UnityEngine;

public class RE_Node : RendererEntity
{
    public Transform rotater;
    private void Update()
    {
        var rotate = Quaternion.AngleAxis(Time.deltaTime * Mathf.PI * 4, Vector3.up);
        rotater.rotation = rotate * rotater.rotation;
    }
}
