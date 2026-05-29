# Weapon Feedback System Design

**Issue:** #4 — Weapon Feedback: Screen shake, muzzle flash, hit effects, kill effects  
**Date:** 2026-05-29  
**Status:** Approved  

## Goal

Every shot and every kill produces satisfying audiovisual feedback. This is the "gun feel" system — the difference between shooting a nerf gun and a shotgun.

## Decisions

- **Approach A: Centralized WeaponFeedbackController** — one MonoBehaviour on the Player that listens for weapon events and orchestrates all feedback. Keeps firing logic clean, consolidates tuning in one inspector.
- **Audio via AudioSource on WeaponHolder** — not PlayClipAtPoint. Supports pitch randomization, spatial audio for multiplayer later, volume/priority control.
- **Crosshair + hitmarker built here** (not deferred to #5). Full feedback loop in one issue.
- **Test target dummy** for validating the fire→hit→kill chain without enemies.

## Architecture

### Event Flow

```
PlayerWeaponController.Fire()
  ├── OnWeaponFired                          → muzzle flash, fire sound, small screen shake
  ├── raycast hit + IDamageable.TakeDamage()
  │   ├── returns false (alive)              → OnHitConfirmed(hitPoint, hitNormal) → hit VFX, hit sound, medium shake, white hitmarker
  │   └── returns true (died)                → OnKillConfirmed(hitPoint, hitNormal) → kill VFX, kill sound, big shake, camera punch, red hitmarker
  └── (miss)                                 → no hit/kill feedback
```

### Component Diagram

```
Player (root)
├── WeaponFeedbackController        ← NEW: subscribes to weapon events, orchestrates all feedback
├── PlayerWeaponController          ← MODIFIED: raises OnWeaponFired, OnHitConfirmed, OnKillConfirmed
├── CameraHolder
│   ├── Camera
│   ├── WeaponCamera
│   └── WeaponHolder
│       ├── AudioSource (weapon SFX) ← NEW: fire sound, reload sound
│       └── [spawned weapon prefab]
│           └── MuzzlePoint
├── AudioSource (impact SFX)         ← NEW: hit/kill sounds (separate so they don't clip fire sound)
└── UI
    └── CrosshairHUD                 ← NEW: static crosshair + hitmarker overlay
```

## Section 1: Events — Trigger Points

### New Events on PlayerWeaponController

```csharp
public event Action OnWeaponFired;
public event Action<Vector3, Vector3> OnHitConfirmed;   // hitPoint, hitNormal
public event Action<Vector3, Vector3> OnKillConfirmed;  // hitPoint, hitNormal
```

Raised inside `Fire()`:
1. `OnWeaponFired` — immediately after consuming ammo, before raycast.
2. On raycast hit, call `IDamageable.TakeDamage()`. If it returns `true` (target died), raise `OnKillConfirmed`. Otherwise raise `OnHitConfirmed`.
3. On raycast miss or non-damageable hit — raise `OnHitConfirmed` if something was hit (for impact particles on walls), nothing if complete miss.

### IDamageable Interface Change

```csharp
// Before
public interface IDamageable
{
    void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal);
}

// After
public interface IDamageable
{
    bool TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal);
}
```

Returns `true` if the target died from this damage. `PlayerHealth.TakeDamage` returns `true` when it triggers `GoDown()`. All other implementors follow the same pattern.

## Section 2: WeaponFeedbackController

MonoBehaviour on the Player root. Central hub for all weapon juice.

### Serialized Fields

```
[Header("References")]
Transform cameraHolder
PlayerWeaponController weaponController
WeaponVisualController weaponVisualController
AudioSource weaponAudioSource       // on WeaponHolder — fire/reload sounds
AudioSource impactAudioSource       // on Player root — hit/kill sounds

[Header("Screen Shake — Fire")]
float fireShakeIntensity = 0.05f
float fireShakeDuration = 0.05f

[Header("Screen Shake — Hit")]
float hitShakeIntensity = 0.1f
float hitShakeDuration = 0.1f

[Header("Screen Shake — Kill")]
float killShakeIntensity = 0.2f
float killShakeDuration = 0.15f

[Header("Kill Camera Punch")]
float killPunchAngle = 2.0f         // degrees of forward pitch impulse
float killPunchDuration = 0.1f      // lerp-back time

[Header("Muzzle Flash Light")]
float muzzleLightIntensity = 3.0f
float muzzleLightDuration = 0.05f
Color muzzleLightColor = (1.0, 0.8, 0.3, 1.0)  // warm amber

[Header("Audio")]
AudioClip hitSound
AudioClip killSound
float minPitch = 0.95f
float maxPitch = 1.05f

[Header("Kill VFX")]
GameObject killEffectPrefab         // distinct from per-weapon hitEffectPrefab
```

### Screen Shake

Coroutine-based. Each call starts a new coroutine:

1. Generate random XY offset: `Random.insideUnitCircle * intensity`
2. Apply to `cameraHolder.localPosition`
3. Each frame, lerp intensity toward zero over `duration`
4. On complete, ensure `cameraHolder.localPosition = Vector3.zero`

Multiple shakes overlap additively — the bigger one dominates as smaller ones decay. Shake is purely positional (XY offset), never rotational. Does NOT fight `PlayerCameraController`'s mouse look because mouse look writes to `localEulerAngles` while shake writes to `localPosition`.

### Kill Camera Punch

Separate from shake. On kill:

1. Apply negative pitch offset to `cameraHolder.localEulerAngles.x` (look-down impulse)
2. Lerp back to original over `killPunchDuration`

This gives visceral recoil kick on kills. Implemented as a coroutine that temporarily overrides pitch, then restores.

**Conflict avoidance with PlayerCameraController:** The punch adds to the camera controller's pitch value rather than fighting it. Implementation: expose a `PitchOffset` float on `PlayerCameraController` that gets added to the final rotation. WeaponFeedbackController writes to that offset and lerps it back. Camera controller always owns the base rotation.

### Muzzle Flash

On `OnWeaponFired`:
1. Get `MuzzlePoint` from `weaponVisualController.ActiveModel`
2. If `currentWeapon.muzzleFlashPrefab != null`, instantiate at MuzzlePoint position/rotation
3. Prefab has `SelfDestruct` component (0.1s lifetime)
4. Spawn a temporary point light at MuzzlePoint: intensity `muzzleLightIntensity`, color `muzzleLightColor`, range ~5m. Fade out over `muzzleLightDuration` via coroutine, then destroy.

### Hit VFX

On `OnHitConfirmed`:
1. If `currentWeapon.hitEffectPrefab != null`, instantiate at `hitPoint` with `Quaternion.LookRotation(hitNormal)`
2. Prefab has `SelfDestruct` component (~1s lifetime, enough for particles to finish)

### Kill VFX

On `OnKillConfirmed`:
1. Spawn hit effect (same as above) AND kill effect (`killEffectPrefab`) at hitPoint
2. Kill effect is larger/more dramatic — different color, more particles

### Audio

**Fire sound** (on `OnWeaponFired`):
1. `weaponAudioSource.pitch = Random.Range(minPitch, maxPitch)`
2. `weaponAudioSource.PlayOneShot(currentWeapon.fireSound)`

PlayOneShot allows overlapping shots at high fire rates.

**Hit sound** (on `OnHitConfirmed`):
1. `impactAudioSource.pitch = Random.Range(minPitch, maxPitch)`
2. `impactAudioSource.PlayOneShot(hitSound)`

**Kill sound** (on `OnKillConfirmed`):
1. `impactAudioSource.pitch = Random.Range(minPitch, maxPitch)`
2. `impactAudioSource.PlayOneShot(killSound)`

Two separate AudioSources so fire and impact sounds don't cut each other off.

### Lifecycle

- `OnEnable`: subscribe to `OnWeaponFired`, `OnHitConfirmed`, `OnKillConfirmed`
- `OnDisable`: unsubscribe from all three

## Section 3: Crosshair & Hitmarker UI

### CrosshairHUD

MonoBehaviour requiring a `UIDocument`. Uses UI Toolkit (consistent with AmmoHUD).

**Crosshair elements:** Four `VisualElement` rectangles arranged as a cross with a center gap.
- Horizontal left/right bars: ~20px wide, 2px tall, offset from center by gap (~4px)
- Vertical top/bottom bars: 2px wide, ~20px tall, same gap offset
- Color: white with slight transparency (rgba 255,255,255,0.8)

**Hitmarker elements:** Four small diagonal lines forming an X at center. Hidden by default (`opacity: 0`).

**Hit feedback flow:**
1. Subscribe to `PlayerWeaponController.OnHitConfirmed` and `OnKillConfirmed`
2. On hit: add USS class `.hitmarker--hit` — sets opacity 1, white color, slight scale-up
3. On kill: add USS class `.hitmarker--kill` — sets opacity 1, red color, larger scale-up
4. Schedule class removal after duration (0.1s hit, 0.15s kill) via `VisualElement.schedule.Execute`
5. USS transitions on opacity and scale handle the smooth fade-out

**USS file:** `CrosshairHUD.uss` with styles for crosshair bars, hitmarker lines, and transition classes.

**UXML file:** `CrosshairHUD.uxml` with the element hierarchy — crosshair container with four bars, hitmarker container with four diagonal lines.

### No Canvas — stays in UI Toolkit. No world-space elements needed.

## Section 4: Test Target Dummy

### TargetDummy MonoBehaviour

Implements `IDamageable`. Placed on a capsule/cube in the test scene.

```
[SerializeField] float maxHealth = 100f
[SerializeField] float respawnDelay = 2f
```

**Behavior:**
- `TakeDamage(float, Vector3, Vector3)` — decrement health. If `health <= 0`, die and return `true`. Otherwise return `false`.
- On death: disable collider and renderer. After `respawnDelay`, re-enable both and reset health to `maxHealth`. Coroutine-based.
- No state machine, no animation, no AI. Just a health sponge.

**Scene placement:** Drop 3-5 at varying ranges in SampleScene. Must be on a layer included in the weapon's `hitLayers` mask (Default layer works, or a dedicated "Enemy" layer for clarity).

## Section 5: SelfDestruct Utility

Port from PogoPizza. Simple component for auto-destroying spawned effects.

```csharp
// Assets/Scripts/Core/Utility/SelfDestruct.cs
public class SelfDestruct : MonoBehaviour
{
    [SerializeField] float lifetime = 5f;

    void Start() => Destroy(gameObject, lifetime);
}
```

Attach to muzzle flash prefabs (0.1s), hit effect prefabs (1s), kill effect prefabs (1.5s).

## Section 6: Modifications to Existing Code

### IDamageable.cs
- Return type: `void` → `bool`

### PlayerHealth.cs
- `TakeDamage` returns `bool` — `true` when `GoDown()` is called, `false` otherwise

### PlayerWeaponController.cs
- Add three events: `OnWeaponFired`, `OnHitConfirmed`, `OnKillConfirmed`
- In `Fire()`: raise `OnWeaponFired` after ammo decrement
- In `Fire()`: check `TakeDamage` return value, raise `OnKillConfirmed` or `OnHitConfirmed` accordingly
- On raycast hit against non-damageable surface: raise `OnHitConfirmed` (for wall impact particles)
- Expose `CurrentWeapon` property (read-only) so feedback controller can access weapon definition for prefab/sound references

### PlayerCameraController.cs
- Add `public float PitchOffset { get; set; }` — additive pitch applied in LateUpdate
- Final rotation: `_pitch + PitchOffset` instead of just `_pitch`
- WeaponFeedbackController writes and lerps this for kill camera punch

### WeaponDefinition.cs
- Add `killEffectPrefab` field (GameObject, nullable) — distinct kill VFX. Falls back to hitEffectPrefab if null.

### Reload audio migration
- Replace `AudioSource.PlayClipAtPoint` in `PlayerWeaponController.StartReload()` with `weaponAudioSource.PlayOneShot(weapon.reloadSound)`. PlayerWeaponController gets a serialized reference to the weapon AudioSource directly — reload is part of weapon mechanics, not feedback.

## New Files

| File | Purpose |
|------|---------|
| `Assets/Scripts/Weapons/WeaponFeedbackController.cs` | Central feedback hub |
| `Assets/Scripts/UI/CrosshairHUD.cs` | Crosshair + hitmarker logic |
| `Assets/UI/CrosshairHUD.uxml` | Crosshair element layout |
| `Assets/UI/CrosshairHUD.uss` | Crosshair styles + transitions |
| `Assets/Scripts/Core/Utility/SelfDestruct.cs` | Auto-destroy for spawned effects |
| `Assets/Scripts/Testing/TargetDummy.cs` | Destructible test target |

## Out of Scope

- Particle/VFX prefab creation (art assets — placeholder cubes/spheres with SelfDestruct are fine)
- Audio clip creation (use placeholder or null — system handles null clips gracefully)
- Spread-based crosshair expansion (stretch goal noted in issue)
- Surface-type-specific hit effects (stretch goal noted in issue)
- Networked feedback synchronization (future Netcode work)
- Weapon switching (separate issue)

## Acceptance Criteria

- [ ] Screen shakes on fire, hit, and kill at different intensities
- [ ] Muzzle flash spawns at weapon barrel on fire
- [ ] Hit particles spawn at impact point with correct orientation (normal-aligned)
- [ ] Kill produces distinct, larger feedback (VFX + camera punch + red hitmarker)
- [ ] Fire sound plays with slight pitch variation per shot
- [ ] Hit and kill sounds play on respective events
- [ ] Static crosshair visible at screen center
- [ ] Hitmarker flashes white on hit, red on kill
- [ ] Test target dummies can be shot and killed, triggering full feedback chain
- [ ] Test dummies respawn after delay for repeated testing
