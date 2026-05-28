# Weapon Reload & Ammo HUD — Design Spec

**Issue:** #3 — Weapon Reload and Ammo HUD
**Date:** 2026-05-28

## Goal

Player can reload their weapon and see ammo state on screen. Ammo management is core to combat feel: "Swarm pressure forces ammo management and positioning."

## 1. Input System

Add a `Reload` action to the `Player` action map in `InputSystem_Actions.inputactions`.

- **Type:** Button
- **Bindings:** Keyboard `R`, Gamepad West (Xbox X / PS Square)
- **PlayerInput.cs:** Add `ReloadPressed` buffered input (same pattern as `ShootPressed`) and `ConsumeReload()` method. Wire to `InputActions.Player.Reload`.

## 2. Reload Mechanic

All reload logic lives in `PlayerWeaponController.cs` alongside existing fire/ammo state.

### New State

- `bool _isReloading` — whether a reload is in progress
- `float _reloadEndTime` — `Time.time` when reload completes

### New Public API

- `bool IsReloading` — read-only property
- `string WeaponName` — from current `WeaponDefinition.weaponName`, for HUD display
- `event Action<int, int> OnAmmoChanged` — fires `(currentAmmo, maxAmmo)` on fire and reload complete
- `event Action<bool> OnReloadStateChanged` — fires on reload start (`true`) and end (`false`)

### Update Loop (order matters)

1. **Reload completion:** If `_isReloading && Time.time >= _reloadEndTime`, set `_currentAmmo = magazineSize`, clear `_isReloading`, fire `OnReloadStateChanged(false)` and `OnAmmoChanged`.
2. **Death interrupt:** If `_health.IsDown` while reloading, cancel reload (clear state, fire `OnReloadStateChanged(false)`). No ammo restore.
3. **Manual reload:** If `ReloadPressed` and magazine not full and not already reloading and weapon equipped and not dead, start reload.
4. **Auto-reload:** If `_currentAmmo <= 0` and fire input detected, start reload instead of firing.
5. **Fire blocking:** `CanFire()` returns `false` when `_isReloading`.

### StartReload()

- Guard: no weapon, already reloading, ammo already full, dead
- Set `_isReloading = true`, `_reloadEndTime = Time.time + _weapon.reloadTime`
- Fire `OnReloadStateChanged(true)`

### Interruption Rules

- **Interrupts reload:** Death, downed
- **Does NOT interrupt:** Sprinting, sliding, jumping, any other movement

### Event Firing

- `OnAmmoChanged(current, max)` fires from: `Fire()` (after decrement), reload complete, weapon equip
- `OnReloadStateChanged(bool)` fires from: `StartReload()`, reload complete, death interrupt

## 3. Ammo HUD (UI Toolkit)

### Files

- `Assets/UI/AmmoHUD.uxml` — layout
- `Assets/UI/AmmoHUD.uss` — styling
- `Assets/Scripts/UI/AmmoHUD.cs` — MonoBehaviour

### Layout

Bottom-right corner of screen:
- Ammo counter label: `currentAmmo / magazineSize` (e.g., "30 / 30")
- Reload indicator label: "RELOADING" — hidden by default, shown during reload

### Styling

- White text on semi-transparent dark background
- Monospace or bold font for readability
- Low ammo state (< 25% magazine): ammo label turns red/orange
- "RELOADING" text in distinct color (yellow or white)
- Prototype quality — functional, not polished

### Behavior (AmmoHUD.cs)

- Finds `PlayerWeaponController` via `FindFirstObjectByType` (single-player prototype)
- Subscribes to `OnAmmoChanged` and `OnReloadStateChanged` events
- Updates labels reactively (no polling in Update)
- Applies/removes low-ammo USS class based on threshold
- Shows/hides reload indicator based on reload state

## 4. Reload Sound Placeholder

- `WeaponDefinition` already has `reloadSound` field (AudioClip)
- Play via `AudioSource.PlayOneShot()` when reload starts (if clip assigned)
- No AudioSource exists on the player yet — add one or use `AudioSource.PlayClipAtPoint()`

## Acceptance Criteria (from issue)

- [ ] Press reload button to reload
- [ ] Reload takes correct duration, blocks firing during
- [ ] Ammo count visible on HUD
- [ ] Auto-reload on empty magazine + fire attempt
- [ ] HUD updates in real-time

## Files Modified

- `InputSystem_Actions.inputactions` — add Reload action with keyboard + gamepad bindings
- `PlayerInput.cs` — add ReloadPressed, ConsumeReload
- `PlayerWeaponController.cs` — reload logic, events, public API

## Files Created

- `Assets/UI/AmmoHUD.uxml` — UI layout
- `Assets/UI/AmmoHUD.uss` — UI styling
- `Assets/Scripts/UI/AmmoHUD.cs` — HUD MonoBehaviour
