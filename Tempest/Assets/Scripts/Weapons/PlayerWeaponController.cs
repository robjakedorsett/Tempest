using Tempest.Core.Sensors;
using Tempest.Player.Enums;
using Tempest.Weapons;
using UnityEngine;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerMovementStateMachine))]
public class PlayerWeaponController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private LayerMask hitLayers;

    [Header("Debug")]
    [SerializeField] private bool debugMode;

    private WeaponDefinition _weapon;
    private RaycastSensor _sensor;
    private PlayerInput _input;
    private PlayerHealth _health;
    private PlayerMovementStateMachine _stateMachine;

    private float _nextFireTime;
    private int _currentAmmo;

    public int CurrentAmmo => _currentAmmo;
    public int MaxAmmo => _weapon != null ? _weapon.magazineSize : 0;
    public bool HasWeapon => _weapon != null;

    private void Awake()
    {
        _input = GetComponent<PlayerInput>();
        _health = GetComponent<PlayerHealth>();
        _stateMachine = GetComponent<PlayerMovementStateMachine>();

        if (cameraHolder == null)
            Debug.LogError("[WeaponController] cameraHolder not assigned.", this);
    }

    private void Start()
    {
        var weapon = _stateMachine.Context?.Loadout?.PrimaryWeapon;
        if (weapon != null)
            EquipWeapon(weapon);
    }

    public void EquipWeapon(WeaponDefinition weapon)
    {
        _weapon = weapon;
        _sensor = new RaycastSensor(weapon.range, hitLayers);
        _currentAmmo = weapon.magazineSize;
        _nextFireTime = 0f;
    }

    private void Update()
    {
        if (!CanFire()) return;
        if (Time.time < _nextFireTime) return;
        if (_currentAmmo <= 0) return;
        if (!HasFireInput()) return;

        Fire();
    }

    private bool CanFire()
    {
        if (_weapon == null) return false;
        if (cameraHolder == null) return false;
        if (_health.IsDown) return false;

        var leafState = GetLeafState();
        if (leafState != null && leafState.StateKey.Equals(PlayerMovementStates.Run))
            return false;

        return true;
    }

    private BaseState<PlayerMovementStates, PlayerContext> GetLeafState()
    {
        var state = _stateMachine.CurrentState;
        if (state == null) return null;

        while (state.SubState != null)
            state = state.SubState;

        return state;
    }

    private bool HasFireInput()
    {
        if (_weapon.fireMode == FireMode.FullAuto)
            return _input.ShootHeld;

        if (_input.ShootPressed)
        {
            _input.ConsumeShoot();
            return true;
        }

        return false;
    }

    private void Fire()
    {
        _currentAmmo--;
        _nextFireTime = _weapon.fireRate > 0f
            ? Time.time + 1f / _weapon.fireRate
            : Time.time;

        Vector3 direction = GetSpreadDirection();
        bool hit = _sensor.CheckRay(cameraHolder, direction, debugMode);

        if (hit)
        {
            RaycastHit hitInfo = _sensor.Hit;
            var damageable = hitInfo.collider.GetComponent<IDamageable>();
            damageable?.TakeDamage(_weapon.damage, hitInfo.point, hitInfo.normal);

            if (debugMode)
            {
                string damageInfo = damageable != null
                    ? $"{_weapon.damage} damage"
                    : "no damageable";
                Debug.Log($"[Weapon] Hit {hitInfo.collider.name} at {hitInfo.distance:F1}m — {damageInfo}");
            }
        }
        else if (debugMode)
        {
            Debug.Log("[Weapon] Missed");
        }
    }

    private Vector3 GetSpreadDirection()
    {
        if (_weapon.spread <= 0f)
            return cameraHolder.forward;

        float spreadRad = _weapon.spread * Mathf.Deg2Rad;
        Vector2 randomPoint = Random.insideUnitCircle * Mathf.Tan(spreadRad);

        Vector3 direction = cameraHolder.forward
            + cameraHolder.right * randomPoint.x
            + cameraHolder.up * randomPoint.y;

        return direction.normalized;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!debugMode || cameraHolder == null || _weapon == null) return;
        if (_weapon.spread <= 0f) return;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f);
        float coneLength = _weapon.range;
        float coneRadius = Mathf.Tan(_weapon.spread * Mathf.Deg2Rad) * coneLength;

        Vector3 origin = cameraHolder.position;
        Vector3 endCenter = origin + cameraHolder.forward * coneLength;

        int segments = 16;
        Vector3 prevPoint = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * 360f * Mathf.Deg2Rad;
            Vector3 offset = cameraHolder.right * (Mathf.Cos(angle) * coneRadius)
                           + cameraHolder.up * (Mathf.Sin(angle) * coneRadius);
            Vector3 point = endCenter + offset;

            Gizmos.DrawLine(origin, point);
            if (i > 0)
                Gizmos.DrawLine(prevPoint, point);

            prevPoint = point;
        }
    }
#endif
}
