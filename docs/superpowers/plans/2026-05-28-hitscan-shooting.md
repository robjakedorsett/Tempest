# Hitscan Shooting Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement hitscan weapon firing with raycast hit detection, damage via IDamageable, fire rate limiting, ammo tracking, spread, and movement state gating.

**Architecture:** PlayerWeaponController MonoBehaviour reads weapon stats from PlayerLoadout, uses RaycastSensor for traces from the camera, and applies damage through IDamageable. Firing is gated by movement state (blocked during Run/Death). Fire mode (SemiAuto/FullAuto) is driven by a new enum on WeaponDefinition.

**Tech Stack:** Unity 6 (6000.x), C#, Unity Input System, existing hierarchical state machine and sensor patterns.

**Spec:** `docs/superpowers/specs/2026-05-28-hitscan-shooting-design.md`

---

## File Map

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `Tempest/Assets/Scripts/Weapons/IDamageable.cs` | Damage interface |
| Create | `Tempest/Assets/Scripts/Weapons/PlayerWeaponController.cs` | Firing logic, ammo, spread, raycast |
| Modify | `Tempest/Assets/Scripts/Weapons/Enums/WeaponEnums.cs` | Add FireMode enum |
| Modify | `Tempest/Assets/Scripts/Weapons/Data/WeaponDefinition.cs` | Add fireMode field |
| Modify | `Tempest/Assets/Scripts/Player/Context/PlayerHealth.cs` | Implement IDamageable |
| Modify | `Tempest/Assets/Scripts/Player/Context/PlayerContext.cs` | Add WeaponController property |
| Modify | `Tempest/Assets/Scripts/Player/PlayerMovement/PlayerMovementStateMachine.cs` | Wire WeaponController into context |

---

### Task 1: Add FireMode enum and field to WeaponDefinition

**Files:**
- Modify: `Tempest/Assets/Scripts/Weapons/Enums/WeaponEnums.cs`
- Modify: `Tempest/Assets/Scripts/Weapons/Data/WeaponDefinition.cs`

- [ ] **Step 1: Add FireMode enum to WeaponEnums.cs**

Add `FireMode` enum after the existing `WeaponType` enum in the `Tempest.Weapons` namespace:

```csharp
// In Tempest/Assets/Scripts/Weapons/Enums/WeaponEnums.cs
// After the closing brace of WeaponType enum (line 13), add:

    public enum FireMode
    {
        SemiAuto,
        FullAuto
    }
```

The full file should read:

```csharp
namespace Tempest.Weapons
{
    public enum WeaponSlot
    {
        Primary,
        Secondary
    }

    public enum WeaponType
    {
        Hitscan,
        Projectile
    }

    public enum FireMode
    {
        SemiAuto,
        FullAuto
    }
}
```

- [ ] **Step 2: Add fireMode field to WeaponDefinition**

In `Tempest/Assets/Scripts/Weapons/Data/WeaponDefinition.cs`, add `fireMode` to the Stats header block. Insert after line 23 (`public float spread;`):

```csharp
        public FireMode fireMode;
```

The Stats section should now read:

```csharp
        [Header("Stats")]
        public float damage;
        public float fireRate;
        public int magazineSize;
        public float reloadTime;
        public float range;
        public float spread;
        public FireMode fireMode;
```

- [ ] **Step 3: Commit**

```bash
git add Tempest/Assets/Scripts/Weapons/Enums/WeaponEnums.cs Tempest/Assets/Scripts/Weapons/Data/WeaponDefinition.cs
git commit -m "feat(weapons): add FireMode enum and field to WeaponDefinition"
```

---

### Task 2: Create IDamageable interface and refactor PlayerHealth

**Files:**
- Create: `Tempest/Assets/Scripts/Weapons/IDamageable.cs`
- Modify: `Tempest/Assets/Scripts/Player/Context/PlayerHealth.cs`

- [ ] **Step 1: Create IDamageable interface**

Create `Tempest/Assets/Scripts/Weapons/IDamageable.cs`:

```csharp
using UnityEngine;

namespace Tempest.Weapons
{
    public interface IDamageable
    {
        void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal);
    }
}
```

- [ ] **Step 2: Refactor PlayerHealth to implement IDamageable**

Modify `Tempest/Assets/Scripts/Player/Context/PlayerHealth.cs`:

1. Add `using UnityEngine;` (already present implicitly via MonoBehaviour) and `using Tempest.Weapons;`
2. Implement `IDamageable` on the class declaration
3. Change the `TakeDamage` signature to match the interface

The full file should read:

```csharp
using Tempest.Weapons;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public bool IsDown { get; private set; }

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (IsDown) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        GameEventBus.RaisePlayerDamaged(damage);

        if (CurrentHealth <= 0f)
            GoDown();
    }

    public void Heal(float amount)
    {
        if (IsDown) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
    }

    private void GoDown()
    {
        IsDown = true;
        GameEventBus.RaisePlayerDowned();
    }

    public void Revive(float healthPercent = 0.5f)
    {
        IsDown = false;
        CurrentHealth = maxHealth * healthPercent;
        GameEventBus.RaisePlayerRevived();
    }

    public void ResetHealth()
    {
        CurrentHealth = maxHealth;
        IsDown = false;
    }
}
```

- [ ] **Step 3: Check for existing callers of TakeDamage that need updating**

Search the codebase for any calls to `TakeDamage` on PlayerHealth. If found, update them to pass `Vector3.zero, Vector3.zero` for hitPoint and hitNormal. At the time of this plan, no callers exist beyond the interface itself.

Run:
```bash
cd Tempest && grep -r "TakeDamage" --include="*.cs" Assets/Scripts/
```

Expected: Only hits in `PlayerHealth.cs` and `IDamageable.cs`. If other callers appear, update them.

- [ ] **Step 4: Commit**

```bash
git add Tempest/Assets/Scripts/Weapons/IDamageable.cs Tempest/Assets/Scripts/Player/Context/PlayerHealth.cs
git commit -m "feat(weapons): add IDamageable interface, refactor PlayerHealth to implement it"
```

---

### Task 3: Wire PlayerWeaponController into PlayerContext and StateMachine

**Files:**
- Modify: `Tempest/Assets/Scripts/Player/Context/PlayerContext.cs`
- Modify: `Tempest/Assets/Scripts/Player/PlayerMovement/PlayerMovementStateMachine.cs`

- [ ] **Step 1: Add WeaponController property to PlayerContext**

In `Tempest/Assets/Scripts/Player/Context/PlayerContext.cs`, add the property. The `using Tempest.Weapons;` import is already present (for PlayerLoadout).

Add after line 17 (`public PlayerLoadout Loadout { get; set; }`):

```csharp
    public PlayerWeaponController WeaponController { get; set; }
```

The full file should read:

```csharp
using Tempest.Weapons;

public class PlayerContext
{
    public PlayerContext(PlayerMotor motor, PlayerInput input, PlayerStance stance, PlayerHealth health)
    {
        Motor = motor;
        Input = input;
        Stance = stance;
        Health = health;
    }

    public PlayerMotor Motor { get; set; }
    public PlayerInput Input { get; set; }
    public PlayerStance Stance { get; set; }
    public PlayerHealth Health { get; set; }
    public PlayerLoadout Loadout { get; set; }
    public PlayerWeaponController WeaponController { get; set; }
}
```

- [ ] **Step 2: Wire WeaponController in PlayerMovementStateMachine.Awake()**

In `Tempest/Assets/Scripts/Player/PlayerMovement/PlayerMovementStateMachine.cs`, inside `Awake()`, add after line 16 (`Context = new PlayerContext(motor, input, stance, health);`):

```csharp
        var weaponController = GetComponent<PlayerWeaponController>();
        if (weaponController != null)
            Context.WeaponController = weaponController;
```

The null check ensures the game still works if `PlayerWeaponController` hasn't been added to the prefab yet.

- [ ] **Step 3: Commit**

```bash
git add Tempest/Assets/Scripts/Player/Context/PlayerContext.cs Tempest/Assets/Scripts/Player/PlayerMovement/PlayerMovementStateMachine.cs
git commit -m "feat(weapons): wire PlayerWeaponController into PlayerContext"
```

---

### Task 4: Implement PlayerWeaponController

This is the core task. The component handles firing input, fire rate cooldown, ammo, spread, raycasting, and damage application.

**Files:**
- Create: `Tempest/Assets/Scripts/Weapons/PlayerWeaponController.cs`

- [ ] **Step 1: Create PlayerWeaponController with initialization and state references**

Create `Tempest/Assets/Scripts/Weapons/PlayerWeaponController.cs`:

```csharp
using Tempest.Core.Sensors;
using Tempest.Player.Enums;
using Tempest.Weapons;
using UnityEngine;

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
    private StateMachine<PlayerMovementStates, PlayerContext> _stateMachine;

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
    }

    private void Start()
    {
        var context = _stateMachine as PlayerMovementStateMachine;
        if (context != null && context.Context?.Loadout?.PrimaryWeapon != null)
        {
            EquipWeapon(context.Context.Loadout.PrimaryWeapon);
        }
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
        if (_weapon == null) return;
        if (!CanFire()) return;
        if (!HasFireInput()) return;
        if (Time.time < _nextFireTime) return;
        if (_currentAmmo <= 0) return;

        Fire();
    }

    private bool CanFire()
    {
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
        _nextFireTime = Time.time + 1f / _weapon.fireRate;

        Vector3 direction = GetSpreadDirection();
        bool hit = _sensor.CheckRay(cameraHolder, direction, debugMode);

        if (hit)
        {
            RaycastHit hitInfo = _sensor.Hit;
            var damageable = hitInfo.collider.GetComponent<IDamageable>();
            damageable?.TakeDamage(_weapon.damage, hitInfo.point, hitInfo.normal);

            if (debugMode)
            {
                Debug.Log($"[Weapon] Hit {hitInfo.collider.name} at {hitInfo.distance:F1}m — {_weapon.damage} damage");
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
```

- [ ] **Step 2: Commit**

```bash
git add Tempest/Assets/Scripts/Weapons/PlayerWeaponController.cs
git commit -m "feat(weapons): implement PlayerWeaponController with hitscan firing"
```

---

### Task 5: Update starter weapon SO assets with FireMode

**Files:**
- Modify: Any `.asset` files for existing WeaponDefinition ScriptableObjects

- [ ] **Step 1: Find existing weapon SO assets**

Run:
```bash
cd Tempest && grep -rl "WeaponDefinition" --include="*.asset" Assets/
```

If weapon SO assets exist, they'll need the new `fireMode` field set. Unity will default new enum fields to 0 (SemiAuto), which is a safe default. If any weapons should be FullAuto (e.g., an assault rifle), update them in the Unity Editor by setting `fireMode` to `FullAuto`.

If no `.asset` files exist yet, skip this step — the field will appear when SOs are next created in the editor.

- [ ] **Step 2: Commit (if changes were made)**

```bash
git add Tempest/Assets/
git commit -m "feat(weapons): set fireMode on existing weapon assets"
```

---

### Task 6: Manual integration test in Unity Editor

This task verifies all acceptance criteria from the issue in the running game.

**Setup required:**
- A scene with the Player GameObject that has all existing components (PlayerMovementStateMachine, PlayerMotor, PlayerInput, PlayerStance, PlayerHealth, PlayerCameraController)
- A `PlayerWeaponController` component added to the Player GameObject
- `cameraHolder` reference set to the same transform used by `PlayerCameraController`
- `hitLayers` set to include Ground and Enemy layers (exclude Player layer)
- `debugMode` enabled
- A `PlayerLoadout` assigned to `PlayerContext.Loadout` with a valid `PrimaryWeapon` (a WeaponDefinition SO with hitscan type, some damage, fireRate ~5-10, magazineSize ~30, range ~100, spread ~1-2 degrees)
- Some target objects in the scene with colliders (cubes, etc.) — at least one with a component implementing `IDamageable`

- [ ] **Step 1: Verify firing with left click**

Enter Play mode. Left click should fire the weapon. With `debugMode` on, you should see:
- Red/green debug rays drawn in the Scene view
- Console logs showing hit/miss info

Expected: "Hit [objectName] at [distance]m — [damage] damage" on hits, "Missed" on misses.

- [ ] **Step 2: Verify fire rate limiting**

Click rapidly. With fireRate of 5, you should get max 5 shots per second regardless of click speed. For FullAuto weapons, hold left click and verify consistent fire rate.

- [ ] **Step 3: Verify ammo depletion**

Fire until ammo runs out (magazineSize shots). After depletion:
- No more shots fire
- No errors in console
- Verify `CurrentAmmo` reaches 0 via inspector or debug log

- [ ] **Step 4: Verify spread**

With spread set to ~5 degrees, fire at a wall from medium distance. Debug rays should show visible variation in direction. With spread at 0, all rays should go straight forward.

- [ ] **Step 5: Verify damage on IDamageable target**

Place a test object with a component implementing `IDamageable` (could temporarily add `PlayerHealth` to a cube for testing). Shoot it and verify:
- Console log shows damage applied
- If using PlayerHealth, verify `CurrentHealth` decreases in the inspector

- [ ] **Step 6: Verify sprint blocks firing**

Sprint (hold shift + move) and try to fire. No shots should occur. Stop sprinting and fire — should work immediately.

- [ ] **Step 7: Verify fire modes**

Test with a SemiAuto weapon: one shot per click, holding doesn't repeat.
Test with a FullAuto weapon: holding fires continuously at fire rate.

- [ ] **Step 8: Commit any test scene changes (optional)**

If you added test objects to the scene for verification, decide whether to keep or remove them before committing.
