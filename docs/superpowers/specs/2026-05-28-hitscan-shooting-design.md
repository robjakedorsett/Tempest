# Hitscan Shooting — Design Spec

**Issue:** [#2 — Hitscan Shooting: Raycast weapon firing, hit detection, and damage](https://github.com/robjakedorsett/Tempest/issues/2)
**Date:** 2026-05-28

## Overview

Player can fire hitscan weapons via raycast from the camera center. Bullets instantly hit targets, apply damage through an `IDamageable` interface, and respect fire rate, ammo, spread, and movement state constraints.

## Files Created

| File | Purpose |
|------|---------|
| `Scripts/Weapons/IDamageable.cs` | Interface for anything that can receive damage |
| `Scripts/Weapons/PlayerWeaponController.cs` | MonoBehaviour — firing logic, ammo, spread, raycast |

## Files Modified

| File | Change |
|------|--------|
| `Scripts/Weapons/Enums/WeaponEnums.cs` | Add `FireMode { SemiAuto, FullAuto }` enum |
| `Scripts/Weapons/Data/WeaponDefinition.cs` | Add `public FireMode fireMode;` field |
| `Scripts/Player/Context/PlayerHealth.cs` | Implement `IDamageable`, update `TakeDamage` signature to include `hitPoint`/`hitNormal` |
| `Scripts/Player/Context/PlayerContext.cs` | Add `PlayerWeaponController WeaponController` property |
| `Scripts/Player/PlayerMovement/PlayerMovementStateMachine.cs` | Wire `WeaponController` into `PlayerContext` during `Awake()` |

## 1. IDamageable Interface

```csharp
namespace Tempest.Weapons
{
    public interface IDamageable
    {
        void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal);
    }
}
```

- Lives in `Scripts/Weapons/IDamageable.cs`
- `hitPoint`/`hitNormal` enable future hit effects and VFX at the correct location
- `PlayerHealth` implements this immediately; enemies will implement it later
- Damage goes through direct method calls, not the event bus — this is the network-ready seam for future Netcode ServerRpc insertion

## 2. PlayerHealth Refactor

- Implement `IDamageable`
- Change signature: `TakeDamage(float amount)` → `TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)`
- `hitPoint`/`hitNormal` are ignored for now (PlayerHealth only cares about the damage value)
- Internal logic (clamp, event raise, GoDown) unchanged

## 3. Fire Mode

Add to `WeaponEnums.cs`:

```csharp
public enum FireMode { SemiAuto, FullAuto }
```

Add to `WeaponDefinition.cs`:

```csharp
public FireMode fireMode;
```

- **SemiAuto**: reads `PlayerInput.ShootPressed`, calls `ConsumeShoot()` after firing — one shot per click
- **FullAuto**: reads `PlayerInput.ShootHeld`, fires continuously while held, gated by fire rate cooldown

## 4. PlayerWeaponController

New MonoBehaviour on the Player GameObject. Core responsibilities:

### Initialization
- Serialized references: `Transform cameraHolder`, `LayerMask hitLayers`, `bool debugMode`
- On start: create `RaycastSensor` with weapon range and `hitLayers`
- Read weapon definition from `PlayerContext.Loadout.PrimaryWeapon`
- Initialize `currentAmmo` from `weapon.magazineSize`

### Per-Frame Update (in `Update()`)
1. **Firing gate**: check `CanFire()`
2. **Input check**: SemiAuto reads `ShootPressed`, FullAuto reads `ShootHeld`
3. **Fire rate cooldown**: `Time.time >= nextFireTime`
4. **Ammo check**: `currentAmmo > 0`
5. **Fire**: calculate spread direction, raycast, apply damage, decrement ammo

### Firing Gate (`CanFire()`)
Returns false if:
- `Health.IsDown` is true (dead)
- Current movement leaf state is `Run` (sprinting)
- No weapon equipped

Returns true in all other states: Idle, Walk, Crouch, CrouchWalk, Airborne, Slide, Jump.

Reads `PlayerMovementStateMachine.CurrentState` and traverses sub-states to find the leaf. Weapon controller reads state but does not participate in state transitions.

### Spread Calculation
- `weapon.spread` is the half-angle of a cone in degrees
- Generate random point within unit circle, scale by `spread`
- Rotate offset into camera space and add to `cameraHolder.forward`
- Result: a direction vector with random deviation within the cone

### Raycast
- Uses `RaycastSensor` (from `Tempest.Core.Sensors`)
- Origin: `cameraHolder.position` (camera center)
- Direction: spread-adjusted forward vector
- Range: `weapon.range`
- LayerMask: serialized `hitLayers` (configured to exclude player's own collider layer)

### On Hit
- `hit.collider.GetComponent<IDamageable>()`
- If found: call `TakeDamage(weapon.damage, hit.point, hit.normal)`
- If not found: valid hit (wall/terrain) but no damage applied

### Ammo
- `currentAmmo` int, initialized from `weapon.magazineSize`
- Decremented by 1 on each shot
- At 0: firing blocked, no reload (reload is issue #3)
- Exposed as public read-only property for future UI

### Debug Mode
- Serialized `bool debugMode` toggle in inspector
- When enabled:
  - `RaycastSensor.CheckRay()` called with `debug: true` — draws ray in Scene view (green on hit, red on miss)
  - `Debug.Log` hit info: target name, damage dealt, distance
  - Gizmo draws spread cone from camera in Scene view

## 5. Integration

### PlayerContext
Add property:
```csharp
public PlayerWeaponController WeaponController { get; set; }
```

### PlayerMovementStateMachine.Awake()
Add after existing `GetComponent` calls:
```csharp
var weaponController = GetComponent<PlayerWeaponController>();
Context.WeaponController = weaponController;
```

`PlayerWeaponController` needs a reference to the state machine to check current state for the firing gate. It grabs this via `GetComponent<PlayerMovementStateMachine>()` in its own initialization — both components live on the same GameObject, so this is straightforward and doesn't require adding the state machine to `PlayerContext`.

## 6. What This Does NOT Include

- **Reload** — separate issue (#3)
- **GameEventBus changes** — damage goes through `IDamageable` directly, keeping a clean seam for future Netcode RPCs
- **Weapon switching** — only reads `PrimaryWeapon` from loadout
- **Visual/audio effects** — no muzzle flash, hit particles, or sound (future work, WeaponDefinition already has the fields)
- **Networked shooting** — prototype is local-only; the `IDamageable` call site is where ServerRpc will be inserted later

## 7. Acceptance Criteria Mapping

| Criteria | Implementation |
|----------|---------------|
| Player can fire weapon with left click | PlayerInput.Attack → ShootPressed/ShootHeld → PlayerWeaponController fires |
| Raycast hits objects and logs hit info | RaycastSensor traces from camera, debug mode logs hits |
| Damage applied to IDamageable targets | GetComponent&lt;IDamageable&gt;()?.TakeDamage() on hit collider |
| Fire rate limiting works | nextFireTime cooldown, `1f / weapon.fireRate` between shots |
| Ammo decrements on fire, blocks at 0 | currentAmmo tracked, decremented per shot, firing blocked at 0 |
| Spread applies random offset | Random cone offset on camera forward using weapon.spread half-angle |
