# Weapon Reload & Ammo HUD Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add weapon reload mechanic and on-screen ammo HUD so players can reload, see ammo count, and get visual feedback during combat.

**Architecture:** Reload state lives in the existing `PlayerWeaponController` alongside fire/ammo logic. Events bridge weapon state to a new `AmmoHUD` MonoBehaviour using UI Toolkit. Input goes through the Unity Input System action asset (auto-generated C# wrapper).

**Tech Stack:** Unity 6, Input System, UI Toolkit (UXML + USS), C# events

**Spec:** `docs/superpowers/specs/2026-05-28-weapon-reload-ammo-hud-design.md`

---

### Task 1: Add Reload Action to Input System

**Files:**
- Modify: `Tempest/Assets/InputSystem_Actions.inputactions` (lines 7-89, actions array; lines 90-476, bindings array)

The `.inputactions` JSON is the source of truth. Unity auto-regenerates `InputSystem_Actions.cs` from it on import. We only edit the JSON.

- [ ] **Step 1: Add Reload action to the Player actions array**

In `InputSystem_Actions.inputactions`, add the Reload action object after the Sprint action (line 88, before the closing `]` of the actions array). Insert before the `]` on line 89:

```json
,
{
    "name": "Reload",
    "type": "Button",
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "expectedControlType": "Button",
    "processors": "",
    "interactions": "",
    "initialStateCheck": false
}
```

**Note:** The `id` must be a unique GUID. Generate a fresh one (e.g., via `[System.Guid]::NewGuid()` in PowerShell). The exact value doesn't matter as long as it's unique within the file.

- [ ] **Step 2: Add Reload bindings to the Player bindings array**

Add two bindings at the end of the Player bindings array (after the Crouch keyboard binding at line 474, before the `]` on line 476):

```json
,
{
    "name": "",
    "id": "GENERATE-GUID-1",
    "path": "<Keyboard>/r",
    "interactions": "",
    "processors": "",
    "groups": "Keyboard&Mouse",
    "action": "Reload",
    "isComposite": false,
    "isPartOfComposite": false
},
{
    "name": "",
    "id": "GENERATE-GUID-2",
    "path": "<Gamepad>/buttonWest",
    "interactions": "",
    "processors": "",
    "groups": "Gamepad",
    "action": "Reload",
    "isComposite": false,
    "isPartOfComposite": false
}
```

**Note:** Generate fresh GUIDs for each binding's `id` field. `buttonWest` is Xbox X / PlayStation Square.

- [ ] **Step 3: Commit**

```
git add Tempest/Assets/InputSystem_Actions.inputactions
git commit -m "feat(input): add Reload action with keyboard R and gamepad X bindings"
```

**Verification:** After Unity reimports, `InputSystem_Actions.cs` will be auto-regenerated with a `Reload` property on `PlayerActions`. This happens automatically when Unity processes the asset — no manual step needed.

---

### Task 2: Wire Reload Input into PlayerInput

**Files:**
- Modify: `Tempest/Assets/Scripts/Player/Context/PlayerInput.cs`

Add buffered `ReloadPressed` input following the exact same pattern as `ShootPressed` (lines 18-19, 34-40, 84-90, 103).

- [ ] **Step 1: Add reload buffer fields**

After the interact buffer fields (line 21-22), add:

```csharp
private float _lastReloadTime = -999f;
private bool _reloadConsumed;
```

- [ ] **Step 2: Add ReloadPressed property**

After the `InteractPressed` property (lines 42-49), add:

```csharp
public bool ReloadPressed
{
    get
    {
        if (_reloadConsumed) return false;
        return Time.time - _lastReloadTime <= pressBufferTime;
    }
}
```

- [ ] **Step 3: Wire the Reload action callback in ConfigureInputs()**

After the Interact callbacks (lines 92-98), add:

```csharp
InputActions.Player.Reload.performed += _ =>
{
    _reloadConsumed = false;
    _lastReloadTime = Time.time;
};
```

Note: Reload is a press-only action (no held state needed), so we only need the `performed` callback. No `canceled` handler.

- [ ] **Step 4: Add ConsumeReload method**

After `ConsumeInteract()` (line 104), add:

```csharp
public void ConsumeReload() => _reloadConsumed = true;
```

- [ ] **Step 5: Commit**

```
git add Tempest/Assets/Scripts/Player/Context/PlayerInput.cs
git commit -m "feat(input): wire ReloadPressed buffered input to Reload action"
```

---

### Task 3: Add Reload Logic to PlayerWeaponController

**Files:**
- Modify: `Tempest/Assets/Scripts/Weapons/PlayerWeaponController.cs`

This is the core task. Add reload state, events, and the reload flow to the existing weapon controller.

- [ ] **Step 1: Add using directive and event fields**

Add `using System;` at the top of the file (before the existing `using` statements on line 1).

Add new fields after `_currentAmmo` (line 26):

```csharp
private bool _isReloading;
private float _reloadEndTime;
```

- [ ] **Step 2: Add public API (properties and events)**

After the `HasWeapon` property (line 30), add:

```csharp
public bool IsReloading => _isReloading;
public string WeaponName => _weapon != null ? _weapon.weaponName : "";

public event Action<int, int> OnAmmoChanged;
public event Action<bool> OnReloadStateChanged;
```

- [ ] **Step 3: Fire OnAmmoChanged from EquipWeapon**

In `EquipWeapon()` (line 49-55), add after `_nextFireTime = 0f;`:

```csharp
_isReloading = false;
OnAmmoChanged?.Invoke(_currentAmmo, weapon.magazineSize);
```

- [ ] **Step 4: Fire OnAmmoChanged from Fire()**

In `Fire()` (line 105), after `_currentAmmo--;` (line 107), add:

```csharp
OnAmmoChanged?.Invoke(_currentAmmo, _weapon.magazineSize);
```

- [ ] **Step 5: Replace the Update method**

Replace the entire `Update()` method (lines 57-65) with:

```csharp
private void Update()
{
    if (_weapon == null) return;

    if (_isReloading)
    {
        if (_health.IsDown)
        {
            CancelReload();
            return;
        }

        if (Time.time >= _reloadEndTime)
            CompleteReload();

        return;
    }

    if (_input.ReloadPressed)
    {
        _input.ConsumeReload();
        if (_currentAmmo < _weapon.magazineSize && !_health.IsDown)
            StartReload();
        return;
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
```

- [ ] **Step 6: Add StartReload, CompleteReload, and CancelReload methods**

Add these methods after the `Fire()` method (after line 133, before `GetSpreadDirection()`):

```csharp
private void StartReload()
{
    if (_weapon == null || _isReloading || _health.IsDown) return;
    if (_currentAmmo >= _weapon.magazineSize) return;

    _isReloading = true;
    _reloadEndTime = Time.time + _weapon.reloadTime;
    OnReloadStateChanged?.Invoke(true);
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
```

- [ ] **Step 7: Commit**

```
git add Tempest/Assets/Scripts/Weapons/PlayerWeaponController.cs
git commit -m "feat(weapons): implement reload mechanic with manual/auto reload and death interrupt"
```

---

### Task 4: Create AmmoHUD USS Stylesheet

**Files:**
- Create: `Tempest/Assets/UI/AmmoHUD.uss`

- [ ] **Step 1: Create the UI directory**

```powershell
New-Item -ItemType Directory -Force "Tempest/Assets/UI"
```

- [ ] **Step 2: Write the USS stylesheet**

Create `Tempest/Assets/UI/AmmoHUD.uss`:

```css
.ammo-hud {
    position: absolute;
    right: 40px;
    bottom: 40px;
    flex-direction: column;
    align-items: flex-end;
}

.ammo-container {
    background-color: rgba(0, 0, 0, 0.6);
    padding: 8px 16px;
    border-radius: 4px;
}

.ammo-label {
    font-size: 28px;
    color: rgb(255, 255, 255);
    -unity-font-style: bold;
}

.ammo-label--low {
    color: rgb(255, 70, 50);
}

.reload-label {
    font-size: 18px;
    color: rgb(255, 220, 50);
    -unity-font-style: bold;
    margin-top: 4px;
    display: none;
}

.reload-label--visible {
    display: flex;
}
```

- [ ] **Step 3: Commit**

```
git add Tempest/Assets/UI/AmmoHUD.uss
git commit -m "feat(ui): add AmmoHUD stylesheet"
```

---

### Task 5: Create AmmoHUD UXML Layout

**Files:**
- Create: `Tempest/Assets/UI/AmmoHUD.uxml`

- [ ] **Step 1: Write the UXML layout**

Create `Tempest/Assets/UI/AmmoHUD.uxml`:

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <Style src="AmmoHUD.uss" />
    <ui:VisualElement name="ammo-hud" class="ammo-hud" picking-mode="Ignore">
        <ui:VisualElement class="ammo-container" picking-mode="Ignore">
            <ui:Label name="ammo-label" class="ammo-label" text="30 / 30" picking-mode="Ignore" />
        </ui:VisualElement>
        <ui:Label name="reload-label" class="reload-label" text="RELOADING" picking-mode="Ignore" />
    </ui:VisualElement>
</ui:UXML>
```

`picking-mode="Ignore"` ensures the HUD doesn't intercept mouse clicks (important since cursor is locked for FPS).

- [ ] **Step 2: Commit**

```
git add Tempest/Assets/UI/AmmoHUD.uxml
git commit -m "feat(ui): add AmmoHUD UXML layout"
```

---

### Task 6: Create AmmoHUD MonoBehaviour

**Files:**
- Create: `Tempest/Assets/Scripts/UI/AmmoHUD.cs`

- [ ] **Step 1: Create the Scripts/UI directory**

```powershell
New-Item -ItemType Directory -Force "Tempest/Assets/Scripts/UI"
```

- [ ] **Step 2: Write AmmoHUD.cs**

Create `Tempest/Assets/Scripts/UI/AmmoHUD.cs`:

```csharp
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class AmmoHUD : MonoBehaviour
{
    private PlayerWeaponController _weaponController;
    private Label _ammoLabel;
    private Label _reloadLabel;

    private void Awake()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _ammoLabel = root.Q<Label>("ammo-label");
        _reloadLabel = root.Q<Label>("reload-label");
    }

    private void OnEnable()
    {
        _weaponController = FindFirstObjectByType<PlayerWeaponController>();
        if (_weaponController == null) return;

        _weaponController.OnAmmoChanged += HandleAmmoChanged;
        _weaponController.OnReloadStateChanged += HandleReloadStateChanged;

        if (_weaponController.HasWeapon)
            HandleAmmoChanged(_weaponController.CurrentAmmo, _weaponController.MaxAmmo);
    }

    private void OnDisable()
    {
        if (_weaponController == null) return;

        _weaponController.OnAmmoChanged -= HandleAmmoChanged;
        _weaponController.OnReloadStateChanged -= HandleReloadStateChanged;
    }

    private void HandleAmmoChanged(int current, int max)
    {
        _ammoLabel.text = $"{current} / {max}";

        bool isLow = max > 0 && current <= max / 4;
        _ammoLabel.EnableInClassList("ammo-label--low", isLow);
    }

    private void HandleReloadStateChanged(bool isReloading)
    {
        _reloadLabel.EnableInClassList("reload-label--visible", isReloading);
    }
}
```

- [ ] **Step 3: Commit**

```
git add Tempest/Assets/Scripts/UI/AmmoHUD.cs
git commit -m "feat(ui): add AmmoHUD MonoBehaviour with event-driven updates"
```

---

### Task 7: Scene Setup and Verification

**Files:**
- Modify: Scene file (manual in Unity Editor)

This task is done manually in the Unity Editor — it cannot be automated via code.

- [ ] **Step 1: Create AmmoHUD GameObject in the scene**

1. In the scene hierarchy, create an empty GameObject named `AmmoHUD`
2. Add a `UI Document` component to it
3. Assign `Assets/UI/AmmoHUD.uxml` as the Source Asset on the UI Document
4. The `Panel Settings` field on UIDocument — if no PanelSettings asset exists, create one via `Assets > Create > UI Toolkit > Panel Settings Asset` at `Assets/UI/DefaultPanelSettings.asset`. Assign it.
5. Add the `AmmoHUD` script component to the same GameObject

- [ ] **Step 2: Verify in Play Mode**

Enter Play Mode and check:
- Ammo counter shows in bottom-right (e.g., "30 / 30")
- Firing reduces the counter in real-time
- When ammo hits 0 + fire attempt, auto-reload starts and "RELOADING" appears
- After reload duration, ammo restores to full and "RELOADING" disappears
- Press R to manually reload when magazine is not full
- Ammo label turns red when below 25% of magazine size
- Reload does NOT cancel when sprinting
- Dying/going down cancels reload

- [ ] **Step 3: Commit scene changes**

```
git add Tempest/Assets/Scenes/SampleScene.unity
git commit -m "feat(scene): add AmmoHUD UI Document to scene"
```
