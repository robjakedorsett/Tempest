using UnityEngine;

public class GroundSensor
{
    public bool IsGrounded { get; private set; }
    public float RelativeHeight { get; private set; }

    public float LastGroundedTime { get; private set; }
    public bool InCoyoteWindow => !IsGrounded && Time.time - LastGroundedTime <= _coyoteTime;

    private readonly LayerMask _groundLayer;
    private readonly float _radius;
    private readonly Collider _collider;
    private readonly Transform _tr;
    private readonly float _coyoteTime;

    public GroundSensor(LayerMask groundLayer, float radius, Collider collider, Transform tr, float coyoteTime)
    {
        _groundLayer = groundLayer;
        _radius = radius;
        _collider = collider;
        _tr = tr;
        _coyoteTime = coyoteTime;
    }

    public void Monitor()
    {
        Vector3 groundCheckPosition =
            _collider.bounds.center + Vector3.down * (_collider.bounds.extents.y + _radius * 0.5f);

        bool groundedNow = Physics.CheckSphere(groundCheckPosition, _radius, _groundLayer);

        if (groundedNow)
        {
            LastGroundedTime = Time.time;
            RelativeHeight = 0f;
        }
        else
        {
            if (Physics.Raycast(_tr.position, Vector3.down, out RaycastHit hit, 100f, _groundLayer))
            {
                RelativeHeight = hit.distance;
            }
        }

        IsGrounded = groundedNow;
    }

    public void DrawGizmos()
    {
        if (_collider == null) return;
        Gizmos.DrawWireSphere(
            _collider.bounds.center + Vector3.down * (_collider.bounds.extents.y + _radius * 0.5f),
            _radius
        );
        Gizmos.DrawLine(_tr.position, _tr.position + Vector3.down * RelativeHeight);
    }
}
