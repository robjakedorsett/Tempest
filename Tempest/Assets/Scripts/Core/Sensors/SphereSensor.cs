using UnityEngine;

public class SphereSensor
{
    private readonly LayerMask _layer;
    private readonly float _radius;
    private readonly Transform _tr;
    private readonly Vector3 _offset;

    public bool Hit { get; private set; }

    public SphereSensor(LayerMask layer, float radius, Transform tr, Vector3 offset)
    {
        _layer = layer;
        _radius = radius;
        _tr = tr;
        _offset = offset;
    }

    public void Monitor()
    {
        var center = _tr.position + _offset;
        Hit = Physics.CheckSphere(center, _radius, _layer);
    }

    public void DrawGizmos()
    {
        var center = _tr.position + _offset;
        Gizmos.DrawWireSphere(center, _radius);
    }
}
