using UnityEngine;

public class CameraMgr
{
    public readonly Camera camera;
    public Rect CameraArea { get; private set; }
    public Vector3 Target
    {
        get
        {
            var position = camera.transform.position;
            var forward = camera.transform.forward;
            return position - forward * (position.y / forward.y);
        }
        set
        {
            var position = camera.transform.position;
            var forward = camera.transform.forward;
            position -= forward * (position.y / forward.y);
            value -= position;
            value.y = 0;
            camera.transform.position += value;
        }
    }
    public float Height
    {
        get
        {
            return (Target - camera.transform.position).magnitude;
        }
        set
        {
            camera.transform.position = Target - camera.transform.forward * Height;
        }
    }
    public CameraMgr(Camera camera)
    {
        this.camera = camera;
    }
    private Vector2 CameraV2P(float x, float y)
    {
        var r = camera.ViewportPointToRay(new Vector3(x, y));
        var position = r.origin - r.direction * (r.origin.y / r.direction.y);
        return new Vector2(position.x, position.z);
    }
    public void Update()
    {
        if (camera)
        {
            var p00 = CameraV2P(0, 0);
            var p01 = CameraV2P(0, 1);
            var p11 = CameraV2P(1, 1);
            var p10 = CameraV2P(1, 0);
            var max = Vector2.Max(Vector2.Max(p00, p01), Vector2.Max(p11, p10));
            var min = Vector2.Min(Vector2.Min(p00, p01), Vector2.Min(p11, p10));
            CameraArea = new Rect(min, max - min);
        }
    }
    public bool TryGetViewportPoint(Vector3 worldPosition, out Vector3 viewportPoint)
    {
        viewportPoint = camera.WorldToViewportPoint(worldPosition);
        return viewportPoint.x > 0 && viewportPoint.x < 1 && viewportPoint.y > 0 && viewportPoint.y < 1;
    }
}
