# Weapon Overlay Camera

**Date:** 2026-05-29
**Status:** Approved
**Part of:** #16 (Weapon Visual Spawning)

## Context

Weapon viewmodel clips through environment geometry (floors, walls) because it shares the main camera's depth buffer. Standard FPS solution: render the weapon with a separate overlay camera on its own layer.

## Goal

Eliminate weapon clipping by rendering weapon models on a dedicated overlay camera stacked on top of the main camera via URP camera stacking.

## Design

### Layer

Add "Weapon" layer to project Tag & Layer settings.

### Scene Hierarchy

```
CameraHolder (existing)
├── Camera (existing — base camera)
├── WeaponCamera (new — overlay camera)
└── WeaponHolder (existing)
    └── [spawned weapon] (set to Weapon layer at runtime)
```

### Main Camera Changes

- Culling mask: everything except Weapon layer
- Render type: Base (unchanged)

### WeaponCamera (new GameObject)

- Child of CameraHolder, local position/rotation zero
- Camera component with URP Universal Additional Camera Data
- Render type: Overlay
- Culling mask: Weapon layer only
- FOV: 55 (lower than main — makes weapon appear larger)
- Near clip: 0.01, Far clip: 10
- Clear flags: Depth only
- Added to Main Camera's camera stack list

### WeaponVisualController Change

After instantiating the weapon prefab in `SpawnWeapon()`, recursively set all GameObjects in the instance to the Weapon layer:

```
private static void SetLayerRecursive(GameObject obj, int layer)
{
    obj.layer = layer;
    foreach (Transform child in obj.transform)
        SetLayerRecursive(child.gameObject, layer);
}
```

Layer index resolved via `LayerMask.NameToLayer("Weapon")`.

### No New Scripts

All changes are configuration (layer, cameras) plus a small addition to `WeaponVisualController.SpawnWeapon()`.

## Acceptance Criteria

- [ ] Weapon renders on top of all world geometry — no clipping when looking at floors/walls
- [ ] Weapon FOV is independent of main camera FOV
- [ ] Main camera does not render weapon layer
- [ ] New weapon prefabs automatically get assigned the Weapon layer on spawn
