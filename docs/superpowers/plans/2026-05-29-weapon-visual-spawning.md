# Weapon Visual Spawning Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Spawn weapon prefabs as visible first-person models with a muzzle point for effects and basic weapon bob while moving.

**Architecture:** Three new MonoBehaviours — `WeaponModel` (on weapon prefab, holds barrel position reference), `WeaponVisualController` (on Player, manages prefab lifecycle), `WeaponBob` (on WeaponHolder, sine-based motion). `PlayerWeaponController.EquipWeapon()` delegates visual spawning to `WeaponVisualController`. Hit detection stays camera-based.

**Tech Stack:** Unity 6, C#

---

## File Map

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `Tempest/Assets/Scripts/Weapons/WeaponModel.cs` | MonoBehaviour on weapon prefab root — holds muzzle point Transform ref |
| Create | `Tempest/Assets/Scripts/Weapons/WeaponVisualController.cs` | MonoBehaviour on Player — spawn/despawn weapon prefab, cache WeaponModel |
| Create | `Tempest/Assets/Scripts/Weapons/WeaponBob.cs` | MonoBehaviour on WeaponHolder — sine-based position bob from movement speed |
| Modify | `Tempest/Assets/Scripts/Weapons/PlayerWeaponController.cs` | Add WeaponVisualController lookup + call in EquipWeapon |
| Manual | `Tempest/Assets/Prefabs/Weapons/Rifle.prefab` | Add WeaponModel component, add MuzzlePoint child Transform |
| Manual | Scene: Player hierarchy | Add WeaponHolder child under CameraHolder, wire components |

### Namespace conventions (matching existing code)

- `WeaponModel` → `Tempest.Weapons` (weapon-specific data, like `WeaponDefinition`)
- `WeaponVisualController` → global namespace (player-level MonoBehaviour, like `PlayerWeaponController`)
- `WeaponBob` → global namespace (player-level MonoBehaviour, like `PlayerMotor`)

---

### Task 1: WeaponModel component

**Files:**
- Create: `Tempest/Assets/Scripts/Weapons/WeaponModel.cs`

- [ ] **Step 1: Create WeaponModel.cs**

```csharp
using UnityEngine;

namespace Tempest.Weapons
{
    public class WeaponModel : MonoBehaviour
    {
        [SerializeField] private Transform muzzlePoint;

        public Transform MuzzlePoint => muzzlePoint;

        private void Awake()
        {
            if (muzzlePoint == null)
                Debug.LogWarning($"[WeaponModel] MuzzlePoint not assigned on {gameObject.name}.", this);
        }
    }
}
```

- [ ] **Step 2: Verify it compiles**

Run: Open Unity / wait for domain reload. Console should show no errors.

- [ ] **Step 3: Commit**

```bash
git add Tempest/Assets/Scripts/Weapons/WeaponModel.cs
git commit -m "feat(weapons): add WeaponModel component for barrel position reference"
```

---

### Task 2: WeaponVisualController component

**Files:**
- Create: `Tempest/Assets/Scripts/Weapons/WeaponVisualController.cs`

- [ ] **Step 1: Create WeaponVisualController.cs**

```csharp
using Tempest.Weapons;
using UnityEngine;

public class WeaponVisualController : MonoBehaviour
{
    [SerializeField] private Transform weaponHolder;

    private GameObject _currentInstance;

    public WeaponModel ActiveModel { get; private set; }

    public void SpawnWeapon(WeaponDefinition weapon)
    {
        DespawnWeapon();

        if (weapon == null || weapon.weaponPrefab == null)
            return;

        _currentInstance = Instantiate(weapon.weaponPrefab, weaponHolder);
        _currentInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        ActiveModel = _currentInstance.GetComponent<WeaponModel>();
        if (ActiveModel == null)
            Debug.LogError($"[WeaponVisualController] Weapon prefab '{weapon.weaponPrefab.name}' is missing a WeaponModel component.", this);
    }

    public void DespawnWeapon()
    {
        if (_currentInstance != null)
        {
            Destroy(_currentInstance);
            _currentInstance = null;
        }
        ActiveModel = null;
    }
}
```

- [ ] **Step 2: Verify it compiles**

Run: Unity domain reload. Console should show no errors.

- [ ] **Step 3: Commit**

```bash
git add Tempest/Assets/Scripts/Weapons/WeaponVisualController.cs
git commit -m "feat(weapons): add WeaponVisualController for weapon prefab spawning"
```

---

### Task 3: Integrate PlayerWeaponController with WeaponVisualController

**Files:**
- Modify: `Tempest/Assets/Scripts/Weapons/PlayerWeaponController.cs`

- [ ] **Step 1: Add field and Awake lookup**

In `PlayerWeaponController`, add a private field after the existing fields (after line 24):

```csharp
private WeaponVisualController _visualController;
```

In `Awake()`, after the existing `GetComponent` calls (after `_stateMachine = GetComponent<PlayerMovementStateMachine>();`), add:

```csharp
_visualController = GetComponent<WeaponVisualController>();
```

- [ ] **Step 2: Call SpawnWeapon from EquipWeapon**

In `EquipWeapon()`, after the line `OnAmmoChanged?.Invoke(_currentAmmo, weapon.magazineSize);`, add:

```csharp
_visualController?.SpawnWeapon(weapon);
```

The `?.` null-conditional means it still works if `WeaponVisualController` isn't attached yet (backwards compatible during development).

- [ ] **Step 3: Verify it compiles**

Run: Unity domain reload. Console should show no errors.

- [ ] **Step 4: Commit**

```bash
git add Tempest/Assets/Scripts/Weapons/PlayerWeaponController.cs
git commit -m "feat(weapons): wire EquipWeapon to spawn weapon visual model"
```

---

### Task 4: WeaponBob component

**Files:**
- Create: `Tempest/Assets/Scripts/Weapons/WeaponBob.cs`

- [ ] **Step 1: Create WeaponBob.cs**

```csharp
using UnityEngine;

public class WeaponBob : MonoBehaviour
{
    [Header("Bob Settings")]
    [SerializeField] private float bobFrequency = 10f;
    [SerializeField] private float bobHorizontalAmplitude = 0.05f;
    [SerializeField] private float bobVerticalAmplitude = 0.03f;

    [Header("Reset")]
    [SerializeField] private float resetSmoothing = 5f;
    [SerializeField] private float speedThreshold = 0.1f;

    [Header("References")]
    [SerializeField] private PlayerMotor motor;

    private float _bobTimer;
    private Vector3 _restPosition;

    private void Start()
    {
        _restPosition = transform.localPosition;
    }

    private void LateUpdate()
    {
        if (motor == null) return;

        float speed = motor.HorizontalSpeed;

        if (speed > speedThreshold)
        {
            _bobTimer += Time.deltaTime * bobFrequency;

            float xOffset = Mathf.Sin(_bobTimer) * bobHorizontalAmplitude;
            float yOffset = Mathf.Sin(_bobTimer * 2f) * bobVerticalAmplitude;

            transform.localPosition = _restPosition + new Vector3(xOffset, yOffset, 0f);
        }
        else
        {
            _bobTimer = 0f;
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                _restPosition,
                Time.deltaTime * resetSmoothing
            );
        }
    }
}
```

Key details:
- Vertical bob runs at `2x` frequency (standard — head bobs twice per stride cycle).
- `_bobTimer` resets to 0 when stopped so the bob always starts from the same phase.
- `LateUpdate` runs after camera positioning in `PlayerCameraController.LateUpdate` (both on the same frame, order handled by Unity's execution within LateUpdate — since WeaponBob is on a child of CameraHolder, the parent transform is already set by the time its localPosition is applied).

- [ ] **Step 2: Verify it compiles**

Run: Unity domain reload. Console should show no errors.

- [ ] **Step 3: Commit**

```bash
git add Tempest/Assets/Scripts/Weapons/WeaponBob.cs
git commit -m "feat(weapons): add WeaponBob for first-person weapon movement"
```

---

### Task 5: Prefab & scene wiring (Unity Editor — manual)

These steps must be done in the Unity Editor. They cannot be scripted.

**Files:**
- Manual: `Tempest/Assets/Prefabs/Weapons/Rifle.prefab`
- Manual: Scene hierarchy (Player object)

- [ ] **Step 1: Add MuzzlePoint to Rifle prefab**

1. Open `Rifle.prefab` in the Prefab Editor (double-click it)
2. Right-click the root "Rifle" GameObject → Create Empty → name it **MuzzlePoint**
3. Position `MuzzlePoint` at the barrel tip. The Rifle is made of Cubes — find the barrel piece (the long forward-extending cube) and place MuzzlePoint at its far end. The position should be roughly at the forward-most Z extent of the barrel geometry, centered on its cross-section.
4. `MuzzlePoint` needs no components — it's just a Transform marker.

- [ ] **Step 2: Add WeaponModel component to Rifle prefab**

1. Select the root "Rifle" GameObject in the Prefab Editor
2. Add Component → search "WeaponModel" → add it
3. Drag the `MuzzlePoint` child into the `Muzzle Point` serialized field
4. Save the prefab (Ctrl+S)

- [ ] **Step 3: Add WeaponHolder to scene hierarchy**

1. In the Scene hierarchy, expand the Player → CameraHolder
2. Right-click CameraHolder → Create Empty → name it **WeaponHolder**
3. Set WeaponHolder's local position to an FPS-appropriate offset. Start with approximately **(0.3, -0.25, 0.5)** — right of center, slightly below eye level, forward of the camera. This will need tuning in Play Mode.
4. Set WeaponHolder's local rotation to **(0, 0, 0)**

- [ ] **Step 4: Add WeaponVisualController to Player**

1. Select the root Player GameObject
2. Add Component → search "WeaponVisualController" → add it
3. Drag the `WeaponHolder` Transform into the `Weapon Holder` serialized field

- [ ] **Step 5: Add WeaponBob to WeaponHolder**

1. Select the `WeaponHolder` GameObject
2. Add Component → search "WeaponBob" → add it
3. Drag the Player's `PlayerMotor` component (on the root Player object) into the `Motor` serialized field
4. Leave default values (bobFrequency=10, horizontal=0.05, vertical=0.03, resetSmoothing=5)

- [ ] **Step 6: Save scene and commit**

Save the scene (Ctrl+S), then:

```bash
git add Tempest/Assets/Scenes/SampleScene.unity
git add Tempest/Assets/Prefabs/Weapons/Rifle.prefab
git commit -m "feat(weapons): wire WeaponHolder, WeaponModel, and WeaponBob in scene and prefab"
```

---

### Task 6: Play Mode verification

- [ ] **Step 1: Enter Play Mode and verify weapon spawns**

1. Press Play in Unity Editor
2. The Naga Fang Rifle should appear in the viewport, positioned in front of the camera (lower-right area)
3. Check the Hierarchy — under Player → CameraHolder → WeaponHolder, there should be a "Rifle(Clone)" with a "MuzzlePoint" child
4. If the weapon is not visible or poorly positioned, adjust WeaponHolder's local position in the scene (exit Play Mode first, adjust, re-enter). Typical FPS values: X 0.2–0.4, Y -0.2 to -0.3, Z 0.4–0.6.

- [ ] **Step 2: Verify weapon bob**

1. Move the player (WASD) — the weapon should bob subtly
2. Stop moving — the weapon should settle smoothly back to rest position
3. If bob is too strong/weak, adjust `bobHorizontalAmplitude` and `bobVerticalAmplitude` on the `WeaponBob` component
4. Sprint (if implemented) should produce more pronounced bob since `HorizontalSpeed` is higher

- [ ] **Step 3: Verify muzzle point is accessible**

1. While in Play Mode, select the Rifle(Clone) in the Hierarchy
2. Expand it — MuzzlePoint should be visible as a child Transform
3. Select MuzzlePoint — its world position should be at the barrel tip of the visible weapon model
4. In the Console, confirm no `[WeaponModel]` or `[WeaponVisualController]` errors

- [ ] **Step 4: Verify hit detection is unchanged**

1. Shoot at something (if there's a target in the scene) — raycasts should still work from camera center
2. Console debug logs (if debugMode is on) should show hit/miss as before

- [ ] **Step 5: Tune and commit final adjustments**

If any positions or values were adjusted during testing:

```bash
git add Tempest/Assets/Scenes/SampleScene.unity
git add Tempest/Assets/Prefabs/Weapons/Rifle.prefab
git commit -m "tweak(weapons): tune weapon holder position and bob parameters"
```
