using System;
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
    [SerializeField] private WeaponDefinition startingWeapon;

    [Header("Debug")]
    [SerializeField] private bool debugMode;

    [Header("Audio")]
    [SerializeField] private AudioSource weaponAudioSource;

    private WeaponDefinition _weapon;
    private RaycastSensor _sensor;
    private PlayerInput _input;
    private PlayerHealth _health;
    private PlayerMovementStateMachine _stateMachine;

    private float _nextFireTime;
    private int _currentAmmo;
    private bool _isReloading;
    private float _reloadEndTime;
    private WeaponVisualController _visualController;

    public int CurrentAmmo => _currentAmmo;
    public int MaxAmmo => _weapon != null ? _weapon.magazineSize : 0;
    public bool HasWeapon => _weapon != null;
    public bool IsReloading => _isReloading;

    public event Action<int, int> OnAmmoChanged;
    public event Action<bool> OnReloadStateChanged;
    public event Action OnWeaponFired;
    public event Action<Vector3, Vector3> OnHitConfirmed;
    public event Action<Vector3, Vector3> OnKillConfirmed;

    public WeaponDefinition CurrentWeapon => _weapon;

    private void Awake()
    {
        _input = GetComponent<PlayerInput>();
        _health = GetComponent<PlayerHealth>();
        _stateMachine = GetComponent<PlayerMovementStateMachine>();
        _visualController = GetComponent<WeaponVisualController>();

        if (cameraHolder == null)
            Debug.LogError("[WeaponController] cameraHolder not assigned.", this);
    }

    private void Start()
    {
        var weapon = _stateMachine.Context?.Loadout?.PrimaryWeapon ?? startingWeapon;
        if (weapon != null)
            EquipWeapon(weapon);
    }

    public void EquipWeapon(WeaponDefinition weapon)
    {
        if (_isReloading)
            OnReloadStateChanged?.Invoke(false);

        _weapon = weapon;
        _sensor = new RaycastSensor(weapon.range, hitLayers);
        _currentAmmo = weapon.magazineSize;
        _nextFireTime = 0f;
        _isReloading = false;
        OnAmmoChanged?.Invoke(_currentAmmo, weapon.magazineSize);
        _visualController?.SpawnWeapon(weapon);
    }

    private void Update()
    {
        if (_weapon == null) return;

        if (_isReloading)
        {
            if (Time.time >= _reloadEndTime)
            {
                CompleteReload();
                return;
            }

            if (_health.IsDown)
            {
                CancelReload();
                return;
            }

            if (_input.ReloadPressed)
                _input.ConsumeReload();

            return;
        }

        if (_input.ReloadPressed)
        {
            _input.ConsumeReload();
            if (_currentAmmo < _weapon.magazineSize && !_health.IsDown)
            {
                StartReload();
                return;
            }
        }

        if (!CanFire()) return;
        if (Time.time < _nextFireTime) return;

        if (_currentAmmo <= 0)
        {
            StartReload();
            return;
        }

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
        OnAmmoChanged?.Invoke(_currentAmmo, _weapon.magazineSize);
        _nextFireTime = _weapon.fireRate > 0f
            ? Time.time + 1f / _weapon.fireRate
            : Time.time;

        OnWeaponFired?.Invoke();

        Vector3 direction = GetSpreadDirection();
        bool hit = _sensor.CheckRay(cameraHolder, direction, debugMode);

        if (hit)
        {
            RaycastHit hitInfo = _sensor.Hit;
            var damageable = hitInfo.collider.GetComponent<IDamageable>();

            if (damageable != null)
            {
                bool killed = damageable.TakeDamage(_weapon.damage, hitInfo.point, hitInfo.normal);
                if (killed)
                    OnKillConfirmed?.Invoke(hitInfo.point, hitInfo.normal);
                else
                    OnHitConfirmed?.Invoke(hitInfo.point, hitInfo.normal);
            }
            else
            {
                OnHitConfirmed?.Invoke(hitInfo.point, hitInfo.normal);
            }

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

    private void StartReload()
    {
        if (_weapon == null || _isReloading || _health.IsDown) return;
        if (_currentAmmo >= _weapon.magazineSize) return;

        _isReloading = true;
        _reloadEndTime = Time.time + _weapon.reloadTime;
        OnReloadStateChanged?.Invoke(true);

        if (_weapon.reloadSound != null && weaponAudioSource != null)
            weaponAudioSource.PlayOneShot(_weapon.reloadSound);
    }

    private void CompleteReload()
    {
        _isReloading = false;
        _currentAmmo = _weapon.magazineSize;
        OnReloadStateChanged?.Invoke(false);
        OnAmmoChanged?.Invoke(_currentAmmo, _weapon.magazineSize);
    }

    private void CancelReload()
    {
        _isReloading = false;
        OnReloadStateChanged?.Invoke(false);
    }

    private Vector3 GetSpreadDirection()
    {
        if (_weapon.spread <= 0f)
            return cameraHolder.forward;

        float spreadRad = _weapon.spread * Mathf.Deg2Rad;
        Vector2 randomPoint = UnityEngine.Random.insideUnitCircle * Mathf.Tan(spreadRad);

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
