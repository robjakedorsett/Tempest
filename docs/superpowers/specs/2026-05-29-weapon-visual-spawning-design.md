# Weapon Visual Spawning & Barrel Position

**Date:** 2026-05-29
**Status:** Approved
**Prerequisite for:** #4 (Weapon Feedback)

## Context

The weapon system is currently data-only. `WeaponDefinition` ScriptableObjects reference a `weaponPrefab` but it is never instantiated. `PlayerWeaponController.Fire()` raycasts from `cameraHolder.position` with no visual weapon on screen. Issue #4 (Weapon Feedback) requires a muzzle point to spawn muzzle flash effects, which means the weapon prefab must be spawned and a barrel position defined.

## Goal

Spawn the weapon prefab as a visible first-person model, expose a muzzle point for effects, and add basic weapon bob so the gun feels alive.

## Design

### Scene Hierarchy

```
Player (existing)
├── CameraHolder (existing)
│   ├── Camera (existing)
│   └── WeaponHolder (new empty GameObject)
│       └── [spawned weapon prefab] (instantiated at runtime)
│           ├── Mesh (existing)
│           └── MuzzlePoint (new empty Transform at barrel tip)
```

`WeaponHolder` is a child of `CameraHolder` so the weapon follows camera look direction. The spawned prefab is positioned/rotated relative to `WeaponHolder`, which can be offset to achieve the correct "held in front of camera" first-person position.

### New Scripts

#### WeaponModel (`Tempest/Assets/Scripts/Weapons/WeaponModel.cs`)

MonoBehaviour placed on each weapon prefab root. Holds serialized Transform references to key points on the weapon model.

```
Fields:
  [SerializeField] Transform muzzlePoint

Properties:
  Transform MuzzlePoint { get; }
```

Starts with only `MuzzlePoint`. Future issues extend with `ejectionPort`, `gripLeft`, `gripRight`, etc. as needed.

#### WeaponVisualController (`Tempest/Assets/Scripts/Weapons/WeaponVisualController.cs`)

MonoBehaviour on the Player GameObject. Manages weapon prefab lifecycle.

```
Fields:
  [SerializeField] Transform weaponHolder

Properties:
  WeaponModel ActiveModel { get; }

Methods:
  void SpawnWeapon(WeaponDefinition weapon)
    - Destroys current model if any
    - Instantiates weapon.weaponPrefab as child of weaponHolder
    - Resets local position/rotation to zero
    - Caches WeaponModel via GetComponent<WeaponModel>()
    - Logs error if prefab lacks WeaponModel component

  void DespawnWeapon()
    - Destroys current model, nulls ActiveModel
```

#### WeaponBob (`Tempest/Assets/Scripts/Weapons/WeaponBob.cs`)

MonoBehaviour on the WeaponHolder GameObject. Applies sine-based position bob driven by player movement speed.

```
Fields:
  [SerializeField] float bobFrequency = 10f
  [SerializeField] float bobHorizontalAmplitude = 0.05f
  [SerializeField] float bobVerticalAmplitude = 0.03f
  [SerializeField] float resetSmoothing = 5f
  [SerializeField] PlayerMotor motor

Behavior:
  - Reads horizontal speed from PlayerMotor.HorizontalSpeed (Rigidbody-based)
  - When moving: applies sine-wave offset to localPosition (horizontal + vertical at different phases)
  - When stationary: smoothly lerps offset back to zero
  - Runs in LateUpdate to apply after camera positioning
```

### Integration with Existing Code

`PlayerWeaponController.EquipWeapon()` gains one line calling `WeaponVisualController.SpawnWeapon(weapon)`. The visual controller is found via `GetComponent<WeaponVisualController>()` in `Awake()`.

Hit detection remains camera-based — the standard FPS pattern where shots originate from screen center for accuracy. Visual effects (muzzle flash, tracers) originate from the barrel position via `WeaponModel.MuzzlePoint`.

### Prefab Changes

`Rifle.prefab`:
- Add `WeaponModel` component to root GameObject
- Add `MuzzlePoint` child Transform positioned at the barrel tip
- Wire `MuzzlePoint` reference in `WeaponModel`'s serialized field

### Scene Changes

Player prefab / scene hierarchy:
- Add `WeaponHolder` empty GameObject as child of `CameraHolder`
- Add `WeaponBob` component to `WeaponHolder`
- Add `WeaponVisualController` component to Player
- Wire `weaponHolder` reference on `WeaponVisualController`

## Acceptance Criteria

- [ ] Weapon prefab spawns as visible first-person model when equipped
- [ ] Switching weapons destroys old model and spawns new one
- [ ] `WeaponModel.MuzzlePoint` returns the barrel tip Transform
- [ ] Weapon bobs subtly while player moves, settles when stationary
- [ ] Hit detection unchanged (camera-based raycast)
- [ ] Error logged if weapon prefab is missing `WeaponModel` component

## Dependencies

- Existing `WeaponDefinition.weaponPrefab` field (already defined)
- Existing `Rifle.prefab` (already exists)
- `PlayerMotor` on player (exposes `HorizontalSpeed` from Rigidbody for WeaponBob)

## Downstream

- Issue #4 (Weapon Feedback) uses `WeaponModel.MuzzlePoint` for muzzle flash spawning
