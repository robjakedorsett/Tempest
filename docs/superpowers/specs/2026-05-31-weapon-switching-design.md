# Weapon Switching Design

**Issue:** [#5 — Weapon Switching: Primary/Secondary toggle](https://github.com/robjakedorsett/Tempest/issues/5)
**Date:** 2026-05-31
**Status:** Approved

## Overview

Players carry a primary and secondary weapon per run. This system lets them switch between the two with scroll wheel or number keys, with a per-weapon equip delay that blocks firing during the transition.

## Architecture: Holder + Controller Split

A new `PlayerWeaponHolder` component manages the weapon inventory and switching. The existing `PlayerWeaponController` stays focused on firing, reloading, and spread for a single active weapon.

### New Types

#### WeaponRuntimeState (struct)

Snapshots per-weapon state so it persists across switches:

```csharp
public struct WeaponRuntimeState
{
    public WeaponDefinition definition;
    public int currentAmmo;
    public float currentSpread;
}
```

Only ammo and spread are preserved. Reload and fire timing are deliberately excluded — see State Reset Rules below.

### PlayerWeaponHolder (new component)

Lives on the Player alongside `PlayerWeaponController`.

**Responsibilities:**
- Stores two `WeaponRuntimeState` slots (primary = index 0, secondary = index 1)
- Reads switch input from `PlayerInput` (scroll wheel, number keys 1/2)
- Manages the swap sequence: save → cancel reload → equip delay → restore
- Exposes `ActiveSlot`, `IsEquipping`, and `OnWeaponSwitched` event

**Serialized fields:**
- None beyond component references (gets weapons from `PlayerLoadout`)

**Switch rules:**
- Ignored if already on the requested slot
- Ignored if currently mid-equip (already switching)
- Cancels reload on the outgoing weapon

**Equip delay:**
- Uses the **incoming** weapon's `equipTime` (raising the new weapon)
- During delay: `IsEquipping = true`, controller blocks firing
- On delay complete: calls `PlayerWeaponController.EquipFromState()` to restore weapon

**Initialization (Start):**
1. Read `PlayerLoadout` from `PlayerMovementStateMachine.Context`
2. Create `WeaponRuntimeState` for both slots (full ammo, base spread)
3. Set active slot to primary (index 0)
4. Call `PlayerWeaponController.EquipFromState()` with primary state

### State Reset Rules

When switching away from a weapon:
- **Reload is cancelled.** State saves `isReloading = false`, ammo stays at current partial amount. Player must restart the reload when switching back.
- **Fire rate cooldown (`nextFireTime`) is not saved.** Reset to 0 on restore. The equip delay already prevents instant firing; stale cooldowns from a previous weapon shouldn't carry over.
- **Spread is saved.** If bloom was high when you switched away, it's still high when you come back (though it will decay normally).

### PlayerWeaponController Changes

Minimal modifications to the existing controller:

- **Remove** `startingWeapon` serialized field
- **Remove** `Start()` auto-equip logic (holder handles initialization)
- **Add** `EquipFromState(WeaponRuntimeState state)` — restores ammo, spread, spawns visual, fires events. Does NOT reset ammo to magazine size (uses state's ammo).
- **Add** `SaveState() → WeaponRuntimeState` — snapshots current ammo and spread. Cancels reload first.
- **Add** `IsEquipping` property — checked in `CanFire()` to block firing during swap delay
- **Keep** `EquipWeapon(WeaponDefinition)` — still used by holder for fresh equip (full ammo), and potentially by other systems

### WeaponDefinition Change

One new field:

```csharp
[Header("Handling")]
public float equipTime = 0.25f;
```

Per-weapon equip delay in seconds. A pistol might be 0.15s, a heavy rifle 0.35s.

### PlayerInput Changes

New input bindings for weapon switching:

- `SwitchWeaponInput` (float) — scroll wheel delta (positive = next slot, negative = previous)
- `Weapon1Pressed` / `Weapon2Pressed` — buffered edge-triggered, same pattern as existing inputs
- `ConsumeWeapon1()`, `ConsumeWeapon2()`, `ConsumeSwitchWeapon()` — consumption methods

Requires new actions in the InputActions asset:
- Scroll wheel bound to a new `SwitchWeapon` action (Value, Axis)
- `1` key bound to `Weapon1` action (Button)
- `2` key bound to `Weapon2` action (Button)

### HUD & Feedback — No Changes Required

All downstream systems already work through events on `PlayerWeaponController`:

- **AmmoHUD** — listens to `OnAmmoChanged`, `OnReloadStateChanged`. Fires automatically when `EquipFromState()` restores state.
- **CrosshairHUD** — reads `CurrentSpread` each frame. Updates automatically.
- **WeaponVisualController** — `SpawnWeapon()` already destroys old model and instantiates new. Called during `EquipFromState()`.
- **WeaponFeedbackController / WeaponBob** — read from `CurrentWeapon` property. Adapts automatically.

## Swap Sequence (Step by Step)

1. Player presses `2` (or scrolls) while on primary
2. `PlayerWeaponHolder` receives input, validates (not already on slot 1, not mid-equip)
3. Holder calls `PlayerWeaponController.SaveState()` — controller cancels any active reload, returns `WeaponRuntimeState` with current ammo/spread
4. Holder stores returned state in slot 0 (primary)
5. Holder sets `IsEquipping = true`, starts equip timer using incoming weapon's `equipTime`
6. `WeaponVisualController.DespawnWeapon()` called (old weapon disappears)
7. Equip timer elapses
8. Holder calls `PlayerWeaponController.EquipFromState(slots[1])` — controller restores secondary weapon's ammo/spread, spawns visual, fires events
9. Holder sets `IsEquipping = false`, fires `OnWeaponSwitched` event

## Files Changed

| File | Change |
|------|--------|
| `Scripts/Weapons/Data/WeaponDefinition.cs` | Add `equipTime` field |
| `Scripts/Weapons/Data/WeaponRuntimeState.cs` | **New** — struct |
| `Scripts/Weapons/PlayerWeaponHolder.cs` | **New** — inventory + switching |
| `Scripts/Weapons/PlayerWeaponController.cs` | Add `SaveState`/`EquipFromState`/`IsEquipping`, remove `startingWeapon`/`Start` |
| `Scripts/Player/Context/PlayerInput.cs` | Add weapon switch input bindings |
| InputActions asset | Add SwitchWeapon, Weapon1, Weapon2 actions |

## Out of Scope

- Consumable slot (slot 3) — separate system, future issue
- Swap animation (lower/raise) — stretch goal, not in this spec
- Different weapon view positions per weapon — future tuning on WeaponModel prefab
