# Weapon Data Architecture — Design Spec

**Issue:** [#1 — Weapon Data Architecture](https://github.com/robjakedorsett/Tempest/issues/1)
**Date:** 2026-05-28
**Status:** Approved

## Overview

Create the data layer for weapons and loadouts: ScriptableObject definitions for weapons and deployables, a weapon pool config for filtering available weapons, and a PlayerLoadout class that holds the player's chosen equipment for a run. Pure data — no firing mechanics, no UI.

## Decisions

- **Approach A: Pure Data SOs + Plain C# Loadout** — WeaponDefinition and DeployableDefinition are data-only ScriptableObjects. WeaponPoolConfig is a SO with a manual list and filtering. PlayerLoadout is a plain C# class added to PlayerContext.
- **Deployables are separate** — Fire Totem and future consumables use DeployableDefinition, not WeaponDefinition. Gun stats (fireRate, magazineSize, etc.) don't apply to placed objects.
- **Folder:** `Assets/Scripts/Weapons/` with `Tempest.Weapons` namespace.

## File Structure

```
Assets/Scripts/Weapons/
├── Enums/
│   └── WeaponEnums.cs          # WeaponSlot, WeaponType enums
├── Data/
│   ├── WeaponDefinition.cs     # SO for gun-type weapons
│   ├── DeployableDefinition.cs # SO for placed consumables
│   └── WeaponPoolConfig.cs     # SO holding all weapon/deployable refs
└── PlayerLoadout.cs            # Plain C# class for run loadout

Assets/ScriptableObjects/Weapons/
├── SpiritCannon.asset
├── NagaFangRifle.asset
├── BonePistol.asset
├── FireTotem.asset
└── WeaponPoolConfig.asset
```

## 1. Enums

`WeaponEnums.cs` in `Tempest.Weapons`:

```csharp
public enum WeaponSlot { Primary, Secondary }
public enum WeaponType { Hitscan, Projectile }
```

No `Consumable` in WeaponSlot — consumables use DeployableDefinition.

## 2. WeaponDefinition ScriptableObject

`[CreateAssetMenu]` decorated. All fields serialized. No methods — pure data.

| Field | Type | Purpose |
|-------|------|---------|
| `weaponName` | `string` | Display name |
| `slot` | `WeaponSlot` | Primary or Secondary |
| `type` | `WeaponType` | Hitscan or Projectile |
| `damage` | `float` | Per-hit damage |
| `fireRate` | `float` | Shots per second |
| `magazineSize` | `int` | Rounds before reload |
| `reloadTime` | `float` | Seconds to reload |
| `range` | `float` | Max effective range (hitscan) or projectile lifetime |
| `spread` | `float` | Accuracy cone in degrees (0 = perfect) |
| `muzzleFlashPrefab` | `GameObject` | VFX on fire |
| `hitEffectPrefab` | `GameObject` | VFX on hit |
| `fireSound` | `AudioClip` | SFX on fire |
| `reloadSound` | `AudioClip` | SFX on reload |
| `icon` | `Sprite` | HUD/selection screen icon |
| `description` | `string` | Flavour text |
| `unlockedByDefault` | `bool` | Available without Armoury upgrade |

## 3. DeployableDefinition ScriptableObject

Minimal placeholder for consumables. Expand when deployable mechanics are built.

| Field | Type | Purpose |
|-------|------|---------|
| `deployableName` | `string` | Display name |
| `description` | `string` | Flavour text |
| `icon` | `Sprite` | HUD icon |
| `deployablePrefab` | `GameObject` | World-placed object |
| `cooldown` | `float` | Seconds between uses |
| `maxCharges` | `int` | Uses per run |
| `unlockedByDefault` | `bool` | Available without Armoury upgrade |

## 4. WeaponPoolConfig ScriptableObject

Single asset holding references to all definitions. Provides filtering for the loadout selection screen.

**Fields:**
- `List<WeaponDefinition> allWeapons`
- `List<DeployableDefinition> allDeployables`

**Methods:**
- `List<WeaponDefinition> GetAvailableWeapons(WeaponSlot slot)` — filters by slot and `unlockedByDefault == true`. Future: takes unlock state from Armoury progression.
- `List<DeployableDefinition> GetAvailableDeployables()` — filters by `unlockedByDefault == true`.

## 5. PlayerLoadout

Plain C# class (not MonoBehaviour, not SO). Holds the player's chosen equipment for the current run.

**Fields:**
- `WeaponDefinition PrimaryWeapon { get; }`
- `WeaponDefinition SecondaryWeapon { get; }`
- `DeployableDefinition Consumable { get; }`

**Methods:**
- Constructor taking all three definitions
- `bool IsValid()` — confirms primary has `WeaponSlot.Primary`, secondary has `WeaponSlot.Secondary`, and neither is null

**Integration:** Added as a field on `PlayerContext` alongside Motor, Input, Health, Stance. For the prototype, PlayerContext hardcodes the loadout (Spirit Cannon + Bone Pistol, no consumable).

## 6. Starter Weapon Assets

All unlocked by default. Stats are placeholder — tuned when firing mechanics exist.

| Asset | Slot | Type | Damage | FireRate | Mag | Reload | Range | Spread |
|-------|------|------|--------|----------|-----|--------|-------|--------|
| Spirit Cannon | Primary | Hitscan | 25 | 5 | 30 | 1.5s | 50 | 2° |
| Naga Fang Rifle | Primary | Hitscan | 60 | 1.5 | 8 | 2.0s | 80 | 0.5° |
| Bone Pistol | Secondary | Hitscan | 12 | 8 | 15 | 1.0s | 30 | 3° |
| Fire Totem | (Deployable) | — | — | — | cooldown: 10s | charges: 3 | — | — |

Prefab/audio/icon fields left null — populated when art and audio assets are created.

## Out of Scope

- Weapon firing/reloading mechanics (future WeaponController MonoBehaviour)
- Ammo tracking at runtime (future WeaponRuntime class)
- Loadout selection UI (future pre-run screen)
- Armoury unlock integration (future meta progression)
- Blessing modifications to weapon stats (future blessing system)
- Networking/syncing of loadout data (future Netcode integration)
