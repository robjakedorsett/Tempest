# Weapon Feedback Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add screen shake, muzzle flash, hit/kill VFX, audio with pitch randomization, crosshair with hitmarker, and test target dummies so every shot feels satisfying.

**Architecture:** Centralized WeaponFeedbackController on the Player subscribes to new events (OnWeaponFired, OnHitConfirmed, OnKillConfirmed) raised by PlayerWeaponController and orchestrates all feedback — screen shake, muzzle flash, hit/kill VFX, audio, and camera punch. CrosshairHUD handles the static crosshair and hitmarker overlay via UI Toolkit. IDamageable changes from void to bool return to signal kills.

**Tech Stack:** Unity 6, URP, UI Toolkit, C#

**Spec:** `docs/superpowers/specs/2026-05-29-weapon-feedback-design.md`

---

## File Map

| File | Action | Responsibility |
|------|--------|----------------|
| `Assets/Scripts/Core/Utility/SelfDestruct.cs` | Create | Auto-destroy spawned effects after configurable lifetime |
| `Assets/Scripts/Weapons/IDamageable.cs` | Modify | Change return type from `void` to `bool` |
| `Assets/Scripts/Player/Context/PlayerHealth.cs` | Modify | Return `bool` from TakeDamage |
| `Assets/Scripts/Weapons/Data/WeaponDefinition.cs` | Modify | Add `killEffectPrefab` field |
| `Assets/Scripts/Player/Camera/PlayerCameraController.cs` | Modify | Add `PitchOffset` property for camera punch |
| `Assets/Scripts/Weapons/PlayerWeaponController.cs` | Modify | Add events, `CurrentWeapon`, modify `Fire()`, add audio source ref |
| `Assets/Scripts/Weapons/WeaponFeedbackController.cs` | Create | Central feedback hub — shake, flash, VFX, audio, punch |
| `Assets/Scripts/UI/CrosshairHUD.cs` | Create | Crosshair rendering + hitmarker flash logic |
| `Assets/UI/CrosshairHUD.uxml` | Create | Crosshair + hitmarker element layout |
| `Assets/UI/CrosshairHUD.uss` | Create | Crosshair styles + hitmarker transitions |
| `Assets/Scripts/Testing/TargetDummy.cs` | Create | Destructible test target implementing IDamageable |

---

### Task 1: SelfDestruct Utility

**Files:**
- Create: `Tempest/Assets/Scripts/Core/Utility/SelfDestruct.cs`

- [ ] **Step 1: Create SelfDestruct.cs**

```csharp
using UnityEngine;

namespace Tempest.Core.Utility
{
    public class SelfDestruct : MonoBehaviour
    {
        [SerializeField] private float lifetime = 5f;

        private void Start()
        {
            Destroy(gameObject, lifetime);
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add "Tempest/Assets/Scripts/Core/Utility/SelfDestruct.cs"
git commit -m "feat(utility): add SelfDestruct component for auto-destroying spawned effects"
```

---

### Task 2: IDamageable Bool Return + PlayerHealth

**Files:**
- Modify: `Tempest/Assets/Scripts/Weapons/IDamageable.cs:7`
- Modify: `Tempest/Assets/Scripts/Player/Context/PlayerHealth.cs:17-26`

- [ ] **Step 1: Change IDamageable return type**

In `Tempest/Assets/Scripts/Weapons/IDamageable.cs`, change line 7:

```csharp
// Before
void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal);

// After
bool TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal);
```

- [ ] **Step 2: Update PlayerHealth.TakeDamage to return bool**

In `Tempest/Assets/Scripts/Player/Context/PlayerHealth.cs`, replace the TakeDamage method (lines 17-26):

```csharp
// Before
public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
{
    if (IsDown) return;

    CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
    GameEventBus.RaisePlayerDamaged(damage);

    if (CurrentHealth <= 0f)
        GoDown();
}

// After
public bool TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
{
    if (IsDown) return false;

    CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
    GameEventBus.RaisePlayerDamaged(damage);

    if (CurrentHealth <= 0f)
    {
        GoDown();
        return true;
    }

    return false;
}
```

- [ ] **Step 3: Commit**

```bash
git add "Tempest/Assets/Scripts/Weapons/IDamageable.cs" "Tempest/Assets/Scripts/Player/Context/PlayerHealth.cs"
git commit -m "feat(weapons): change IDamageable.TakeDamage to return bool for kill detection"
```

---

### Task 3: WeaponDefinition + PlayerCameraController Prep

**Files:**
- Modify: `Tempest/Assets/Scripts/Weapons/Data/WeaponDefinition.cs:28-32`
- Modify: `Tempest/Assets/Scripts/Player/Camera/PlayerCameraController.cs:17-18,49-52`

- [ ] **Step 1: Add killEffectPrefab to WeaponDefinition**

In `Tempest/Assets/Scripts/Weapons/Data/WeaponDefinition.cs`, add after `hitEffectPrefab` (line 30):

```csharp
// Before (lines 28-32)
[Header("Effects")]
public GameObject muzzleFlashPrefab;
public GameObject hitEffectPrefab;
public AudioClip fireSound;
public AudioClip reloadSound;

// After
[Header("Effects")]
public GameObject muzzleFlashPrefab;
public GameObject hitEffectPrefab;
public GameObject killEffectPrefab;
public AudioClip fireSound;
public AudioClip reloadSound;
```

- [ ] **Step 2: Add PitchOffset to PlayerCameraController**

In `Tempest/Assets/Scripts/Player/Camera/PlayerCameraController.cs`, add a public property after the private fields (after line 18):

```csharp
private float _pitch;
private float _yaw;

public float PitchOffset { get; set; }
```

Then update the cameraHolder rotation in LateUpdate (line 52) to include the offset:

```csharp
// Before
if (cameraHolder != null)
    cameraHolder.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

// After
if (cameraHolder != null)
    cameraHolder.localRotation = Quaternion.Euler(_pitch + PitchOffset, 0f, 0f);
```

- [ ] **Step 3: Commit**

```bash
git add "Tempest/Assets/Scripts/Weapons/Data/WeaponDefinition.cs" "Tempest/Assets/Scripts/Player/Camera/PlayerCameraController.cs"
git commit -m "feat(weapons): add killEffectPrefab to WeaponDefinition and PitchOffset to camera controller"
```

---

### Task 4: PlayerWeaponController Events and Fire() Changes

**Files:**
- Modify: `Tempest/Assets/Scripts/Weapons/PlayerWeaponController.cs`

This is the largest modification. Changes:
1. Add three new events
2. Add `CurrentWeapon` property
3. Add `weaponAudioSource` serialized field
4. Rewrite `Fire()` to raise events and check kill return value
5. Migrate reload audio to use AudioSource

- [ ] **Step 1: Add events, CurrentWeapon property, and weaponAudioSource field**

In `Tempest/Assets/Scripts/Weapons/PlayerWeaponController.cs`:

Add to the using directives at top (no change needed — `System` and `UnityEngine` already imported).

Add the new serialized field after the existing header block (after line 18):

```csharp
[Header("Debug")]
[SerializeField] private bool debugMode;

[Header("Audio")]
[SerializeField] private AudioSource weaponAudioSource;
```

Add the new events and property after the existing events (after line 38):

```csharp
public event Action<int, int> OnAmmoChanged;
public event Action<bool> OnReloadStateChanged;
public event Action OnWeaponFired;
public event Action<Vector3, Vector3> OnHitConfirmed;
public event Action<Vector3, Vector3> OnKillConfirmed;

public WeaponDefinition CurrentWeapon => _weapon;
```

- [ ] **Step 2: Rewrite Fire() to raise events**

Replace the `Fire()` method (lines 157-186):

```csharp
// Before
private void Fire()
{
    _currentAmmo--;
    OnAmmoChanged?.Invoke(_currentAmmo, _weapon.magazineSize);
    _nextFireTime = _weapon.fireRate > 0f
        ? Time.time + 1f / _weapon.fireRate
        : Time.time;

    Vector3 direction = GetSpreadDirection();
    bool hit = _sensor.CheckRay(cameraHolder, direction, debugMode);

    if (hit)
    {
        RaycastHit hitInfo = _sensor.Hit;
        var damageable = hitInfo.collider.GetComponent<IDamageable>();
        damageable?.TakeDamage(_weapon.damage, hitInfo.point, hitInfo.normal);

        if (debugMode)
        {
            string damageInfo = damageable != null
                ? $"{_weapon.damage} damage"
                : "no damageable";
            Debug.Log($"[Weapon] Hit {hitInfo.collider.name} at {hitInfo.distance:F1}m — {damageInfo}");
        }
    }
    else if (debugMode)
    {
        Debug.Log("[Weapon] Missed");
    }
}

// After
private void Fire()
{
    _currentAmmo--;
    OnAmmoChanged?.Invoke(_currentAmmo, _weapon.magazineSize);
    _nextFireTime = _weapon.fireRate > 0f
        ? Time.time + 1f / _weapon.fireRate
        : Time.time;

    OnWeaponFired?.Invoke();

    Vector3 direction = GetSpreadDirection();
    bool hit = _sensor.CheckRay(cameraHolder, direction, debugMode);

    if (hit)
    {
        RaycastHit hitInfo = _sensor.Hit;
        var damageable = hitInfo.collider.GetComponent<IDamageable>();

        if (damageable != null)
        {
            bool killed = damageable.TakeDamage(_weapon.damage, hitInfo.point, hitInfo.normal);
            if (killed)
                OnKillConfirmed?.Invoke(hitInfo.point, hitInfo.normal);
            else
                OnHitConfirmed?.Invoke(hitInfo.point, hitInfo.normal);
        }
        else
        {
            OnHitConfirmed?.Invoke(hitInfo.point, hitInfo.normal);
        }

        if (debugMode)
        {
            string damageInfo = damageable != null
                ? $"{_weapon.damage} damage"
                : "no damageable";
            Debug.Log($"[Weapon] Hit {hitInfo.collider.name} at {hitInfo.distance:F1}m — {damageInfo}");
        }
    }
    else if (debugMode)
    {
        Debug.Log("[Weapon] Missed");
    }
}
```

- [ ] **Step 3: Migrate reload audio to AudioSource**

In `StartReload()` (lines 188-199), replace the PlayClipAtPoint call:

```csharp
// Before
private void StartReload()
{
    if (_weapon == null || _isReloading || _health.IsDown) return;
    if (_currentAmmo >= _weapon.magazineSize) return;

    _isReloading = true;
    _reloadEndTime = Time.time + _weapon.reloadTime;
    OnReloadStateChanged?.Invoke(true);

    if (_weapon.reloadSound != null)
        AudioSource.PlayClipAtPoint(_weapon.reloadSound, transform.position);
}

// After
private void StartReload()
{
    if (_weapon == null || _isReloading || _health.IsDown) return;
    if (_currentAmmo >= _weapon.magazineSize) return;

    _isReloading = true;
    _reloadEndTime = Time.time + _weapon.reloadTime;
    OnReloadStateChanged?.Invoke(true);

    if (_weapon.reloadSound != null && weaponAudioSource != null)
        weaponAudioSource.PlayOneShot(_weapon.reloadSound);
}
```

- [ ] **Step 4: Commit**

```bash
git add "Tempest/Assets/Scripts/Weapons/PlayerWeaponController.cs"
git commit -m "feat(weapons): add feedback events, CurrentWeapon, and AudioSource to PlayerWeaponController"
```

---

### Task 5: WeaponFeedbackController

**Files:**
- Create: `Tempest/Assets/Scripts/Weapons/WeaponFeedbackController.cs`

- [ ] **Step 1: Create WeaponFeedbackController.cs**

```csharp
using Tempest.Weapons;
using UnityEngine;

public class WeaponFeedbackController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private PlayerWeaponController weaponController;
    [SerializeField] private WeaponVisualController weaponVisualController;
    [SerializeField] private PlayerCameraController cameraController;
    [SerializeField] private AudioSource weaponAudioSource;
    [SerializeField] private AudioSource impactAudioSource;

    [Header("Screen Shake — Fire")]
    [SerializeField] private float fireShakeIntensity = 0.05f;
    [SerializeField] private float fireShakeDuration = 0.05f;

    [Header("Screen Shake — Hit")]
    [SerializeField] private float hitShakeIntensity = 0.1f;
    [SerializeField] private float hitShakeDuration = 0.1f;

    [Header("Screen Shake — Kill")]
    [SerializeField] private float killShakeIntensity = 0.2f;
    [SerializeField] private float killShakeDuration = 0.15f;

    [Header("Kill Camera Punch")]
    [SerializeField] private float killPunchAngle = 2f;
    [SerializeField] private float killPunchDuration = 0.1f;

    [Header("Muzzle Flash Light")]
    [SerializeField] private float muzzleLightIntensity = 3f;
    [SerializeField] private float muzzleLightDuration = 0.05f;
    [SerializeField] private Color muzzleLightColor = new(1f, 0.8f, 0.3f);

    [Header("Audio")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip killSound;
    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;

    [Header("Kill VFX")]
    [SerializeField] private GameObject killEffectPrefab;

    private float _shakeIntensity;
    private float _shakeDuration;
    private float _shakeTimer;
    private float _punchTimer;

    private void OnEnable()
    {
        if (weaponController == null) return;
        weaponController.OnWeaponFired += HandleWeaponFired;
        weaponController.OnHitConfirmed += HandleHitConfirmed;
        weaponController.OnKillConfirmed += HandleKillConfirmed;
    }

    private void OnDisable()
    {
        if (weaponController == null) return;
        weaponController.OnWeaponFired -= HandleWeaponFired;
        weaponController.OnHitConfirmed -= HandleHitConfirmed;
        weaponController.OnKillConfirmed -= HandleKillConfirmed;
    }

    private void Update()
    {
        UpdateShake();
        UpdatePunch();
    }

    private void UpdateShake()
    {
        if (_shakeTimer > 0f)
        {
            _shakeTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(_shakeTimer / _shakeDuration);
            Vector2 offset = Random.insideUnitCircle * (_shakeIntensity * t);
            cameraHolder.localPosition = new Vector3(offset.x, offset.y, 0f);
        }
        else
        {
            cameraHolder.localPosition = Vector3.zero;
        }
    }

    private void UpdatePunch()
    {
        if (_punchTimer > 0f)
        {
            _punchTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(_punchTimer / killPunchDuration);
            cameraController.PitchOffset = -killPunchAngle * t;
        }
        else if (cameraController.PitchOffset != 0f)
        {
            cameraController.PitchOffset = 0f;
        }
    }

    private void Shake(float intensity, float duration)
    {
        _shakeIntensity = intensity;
        _shakeDuration = duration;
        _shakeTimer = duration;
    }

    private void HandleWeaponFired()
    {
        Shake(fireShakeIntensity, fireShakeDuration);
        SpawnMuzzleFlash();
        PlayFireSound();
    }

    private void HandleHitConfirmed(Vector3 hitPoint, Vector3 hitNormal)
    {
        Shake(hitShakeIntensity, hitShakeDuration);
        SpawnHitEffect(hitPoint, hitNormal);
        PlaySound(impactAudioSource, hitSound);
    }

    private void HandleKillConfirmed(Vector3 hitPoint, Vector3 hitNormal)
    {
        Shake(killShakeIntensity, killShakeDuration);
        _punchTimer = killPunchDuration;
        SpawnHitEffect(hitPoint, hitNormal);
        SpawnKillEffect(hitPoint, hitNormal);
        PlaySound(impactAudioSource, killSound);
    }

    private void SpawnMuzzleFlash()
    {
        var model = weaponVisualController != null ? weaponVisualController.ActiveModel : null;
        if (model == null || model.MuzzlePoint == null) return;

        var weapon = weaponController.CurrentWeapon;
        if (weapon != null && weapon.muzzleFlashPrefab != null)
            Instantiate(weapon.muzzleFlashPrefab, model.MuzzlePoint.position, model.MuzzlePoint.rotation);

        SpawnMuzzleLight(model.MuzzlePoint);
    }

    private void SpawnMuzzleLight(Transform muzzlePoint)
    {
        var lightObj = new GameObject("MuzzleLight");
        lightObj.transform.position = muzzlePoint.position;
        var pointLight = lightObj.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.intensity = muzzleLightIntensity;
        pointLight.color = muzzleLightColor;
        pointLight.range = 5f;
        Destroy(lightObj, muzzleLightDuration);
    }

    private void SpawnHitEffect(Vector3 hitPoint, Vector3 hitNormal)
    {
        var weapon = weaponController.CurrentWeapon;
        if (weapon == null || weapon.hitEffectPrefab == null) return;
        Instantiate(weapon.hitEffectPrefab, hitPoint, Quaternion.LookRotation(hitNormal));
    }

    private void SpawnKillEffect(Vector3 hitPoint, Vector3 hitNormal)
    {
        var weapon = weaponController.CurrentWeapon;
        var prefab = weapon != null && weapon.killEffectPrefab != null
            ? weapon.killEffectPrefab
            : killEffectPrefab;
        if (prefab == null) return;
        Instantiate(prefab, hitPoint, Quaternion.LookRotation(hitNormal));
    }

    private void PlayFireSound()
    {
        var weapon = weaponController.CurrentWeapon;
        if (weapon == null || weapon.fireSound == null || weaponAudioSource == null) return;
        weaponAudioSource.pitch = Random.Range(minPitch, maxPitch);
        weaponAudioSource.PlayOneShot(weapon.fireSound);
    }

    private void PlaySound(AudioSource source, AudioClip clip)
    {
        if (source == null || clip == null) return;
        source.pitch = Random.Range(minPitch, maxPitch);
        source.PlayOneShot(clip);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add "Tempest/Assets/Scripts/Weapons/WeaponFeedbackController.cs"
git commit -m "feat(weapons): add WeaponFeedbackController for screen shake, VFX, and audio feedback"
```

---

### Task 6: CrosshairHUD

**Files:**
- Create: `Tempest/Assets/UI/CrosshairHUD.uxml`
- Create: `Tempest/Assets/UI/CrosshairHUD.uss`
- Create: `Tempest/Assets/Scripts/UI/CrosshairHUD.cs`

- [ ] **Step 1: Create CrosshairHUD.uxml**

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <Style src="CrosshairHUD.uss" />
    <ui:VisualElement name="crosshair-root" class="crosshair-root" picking-mode="Ignore">
        <ui:VisualElement name="crosshair-top" class="crosshair-line crosshair-top" picking-mode="Ignore" />
        <ui:VisualElement name="crosshair-bottom" class="crosshair-line crosshair-bottom" picking-mode="Ignore" />
        <ui:VisualElement name="crosshair-left" class="crosshair-line crosshair-left" picking-mode="Ignore" />
        <ui:VisualElement name="crosshair-right" class="crosshair-line crosshair-right" picking-mode="Ignore" />
        <ui:VisualElement name="hitmarker" class="hitmarker" picking-mode="Ignore">
            <ui:VisualElement class="hitmarker-line hitmarker-nw" picking-mode="Ignore" />
            <ui:VisualElement class="hitmarker-line hitmarker-ne" picking-mode="Ignore" />
            <ui:VisualElement class="hitmarker-line hitmarker-sw" picking-mode="Ignore" />
            <ui:VisualElement class="hitmarker-line hitmarker-se" picking-mode="Ignore" />
        </ui:VisualElement>
    </ui:VisualElement>
</ui:UXML>
```

- [ ] **Step 2: Create CrosshairHUD.uss**

```css
.crosshair-root {
    position: absolute;
    left: 0;
    right: 0;
    top: 0;
    bottom: 0;
}

/* --- Crosshair lines --- */

.crosshair-line {
    position: absolute;
    background-color: rgba(255, 255, 255, 0.8);
}

.crosshair-top {
    width: 2px;
    height: 16px;
    left: 50%;
    top: 50%;
    translate: -1px -20px;
}

.crosshair-bottom {
    width: 2px;
    height: 16px;
    left: 50%;
    top: 50%;
    translate: -1px 4px;
}

.crosshair-left {
    width: 16px;
    height: 2px;
    left: 50%;
    top: 50%;
    translate: -20px -1px;
}

.crosshair-right {
    width: 16px;
    height: 2px;
    left: 50%;
    top: 50%;
    translate: 4px -1px;
}

/* --- Hitmarker --- */

.hitmarker {
    position: absolute;
    left: 50%;
    top: 50%;
    width: 0;
    height: 0;
    opacity: 0;
    scale: 1;
    transition: opacity 0.15s ease-out, scale 0.15s ease-out;
}

.hitmarker-line {
    position: absolute;
    width: 2px;
    height: 10px;
    background-color: rgba(255, 255, 255, 0.9);
}

.hitmarker-nw {
    translate: -8px -12px;
    rotate: -45deg;
}

.hitmarker-ne {
    translate: 6px -12px;
    rotate: 45deg;
}

.hitmarker-sw {
    translate: -8px 2px;
    rotate: 45deg;
}

.hitmarker-se {
    translate: 6px 2px;
    rotate: -45deg;
}

/* --- Hitmarker states --- */

.hitmarker--hit {
    opacity: 1;
    scale: 1.2;
}

.hitmarker--kill {
    opacity: 1;
    scale: 1.5;
}

.hitmarker--kill .hitmarker-line {
    background-color: rgb(255, 50, 50);
}
```

- [ ] **Step 3: Create CrosshairHUD.cs**

```csharp
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class CrosshairHUD : MonoBehaviour
{
    private PlayerWeaponController _weaponController;
    private VisualElement _hitmarker;
    private IVisualElementScheduledItem _hideTask;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _hitmarker = root.Q<VisualElement>("hitmarker");

        _weaponController = FindFirstObjectByType<PlayerWeaponController>();
        if (_weaponController == null) return;

        _weaponController.OnHitConfirmed += HandleHit;
        _weaponController.OnKillConfirmed += HandleKill;
    }

    private void OnDisable()
    {
        if (_weaponController == null) return;
        _weaponController.OnHitConfirmed -= HandleHit;
        _weaponController.OnKillConfirmed -= HandleKill;
    }

    private void HandleHit(Vector3 hitPoint, Vector3 hitNormal)
    {
        ShowHitmarker(false);
    }

    private void HandleKill(Vector3 hitPoint, Vector3 hitNormal)
    {
        ShowHitmarker(true);
    }

    private void ShowHitmarker(bool isKill)
    {
        _hideTask?.Pause();

        _hitmarker.RemoveFromClassList("hitmarker--hit");
        _hitmarker.RemoveFromClassList("hitmarker--kill");

        _hitmarker.AddToClassList(isKill ? "hitmarker--kill" : "hitmarker--hit");

        long durationMs = isKill ? 150 : 100;
        _hideTask = _hitmarker.schedule.Execute(() =>
        {
            _hitmarker.RemoveFromClassList("hitmarker--hit");
            _hitmarker.RemoveFromClassList("hitmarker--kill");
        }).StartingIn(durationMs);
    }
}
```

- [ ] **Step 4: Commit**

```bash
git add "Tempest/Assets/UI/CrosshairHUD.uxml" "Tempest/Assets/UI/CrosshairHUD.uss" "Tempest/Assets/Scripts/UI/CrosshairHUD.cs"
git commit -m "feat(ui): add CrosshairHUD with static crosshair and hitmarker feedback"
```

---

### Task 7: TargetDummy

**Files:**
- Create: `Tempest/Assets/Scripts/Testing/TargetDummy.cs`

- [ ] **Step 1: Create TargetDummy.cs**

```csharp
using System.Collections;
using Tempest.Weapons;
using UnityEngine;

namespace Tempest.Testing
{
    public class TargetDummy : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float respawnDelay = 2f;

        private float _currentHealth;
        private Collider _collider;
        private Renderer _renderer;

        private void Awake()
        {
            _currentHealth = maxHealth;
            _collider = GetComponent<Collider>();
            _renderer = GetComponent<Renderer>();
        }

        public bool TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
        {
            _currentHealth -= damage;
            if (_currentHealth <= 0f)
            {
                Die();
                return true;
            }
            return false;
        }

        private void Die()
        {
            _collider.enabled = false;
            _renderer.enabled = false;
            StartCoroutine(RespawnAfterDelay());
        }

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelay);
            _currentHealth = maxHealth;
            _collider.enabled = true;
            _renderer.enabled = true;
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add "Tempest/Assets/Scripts/Testing/TargetDummy.cs"
git commit -m "feat(testing): add TargetDummy for validating weapon feedback chain"
```

---

### Task 8: Scene Wiring and Verification

This task covers the Unity Editor scene setup. These are manual steps — no code files to create.

**Files:**
- Modify: `Tempest/Assets/Scenes/SampleScene.unity` (scene file, modified via Editor)

- [ ] **Step 1: Add AudioSources to Player**

In the Unity Editor, in SampleScene:

1. Select the **WeaponHolder** child of CameraHolder (under Player)
2. Add Component → **AudioSource**
   - Spatial Blend: 0 (2D for now — player's own weapon sounds don't need spatialization)
   - Play On Awake: unchecked
3. Select the **Player** root GameObject
4. Add Component → **AudioSource**
   - Spatial Blend: 0
   - Play On Awake: unchecked
   - This is the impact audio source (hit/kill sounds)

- [ ] **Step 2: Add WeaponFeedbackController to Player**

1. Select the **Player** root GameObject
2. Add Component → **WeaponFeedbackController**
3. Wire the serialized references in the Inspector:
   - Camera Holder → drag `CameraHolder` child
   - Weapon Controller → drag the `PlayerWeaponController` component on Player
   - Weapon Visual Controller → drag the `WeaponVisualController` component on Player
   - Camera Controller → drag the `PlayerCameraController` component on Player
   - Weapon Audio Source → drag the AudioSource on **WeaponHolder**
   - Impact Audio Source → drag the AudioSource on **Player** root
4. Leave Hit Sound, Kill Sound, Kill Effect Prefab as None for now (no audio assets yet — the code handles null gracefully)

- [ ] **Step 3: Wire weaponAudioSource on PlayerWeaponController**

1. Select the **Player** root GameObject
2. In the **PlayerWeaponController** Inspector, find the new "Audio" section
3. Weapon Audio Source → drag the AudioSource on **WeaponHolder** (same one used by feedback controller)

- [ ] **Step 4: Add CrosshairHUD to the scene**

1. Create a new empty GameObject in the scene root named **CrosshairUI**
2. Add Component → **UIDocument**
   - Panel Settings → drag `Assets/UI/PanelSettings.asset`
   - Source Asset → drag `Assets/UI/CrosshairHUD.uxml`
   - Sort Order: set to 1 (renders above AmmoHUD if same panel settings; adjust if needed)
3. Add Component → **CrosshairHUD**

- [ ] **Step 5: Create test target dummies**

1. Create a **Capsule** (3D Object → Capsule) in the scene
2. Position it ~10m in front of the player spawn
3. Add Component → **TargetDummy** (from Tempest.Testing namespace)
   - Max Health: 100
   - Respawn Delay: 2
4. Ensure the capsule's layer is included in the PlayerWeaponController's `hitLayers` mask (Default layer should work)
5. Duplicate the capsule 2-3 times at different ranges (5m, 15m, 25m)

- [ ] **Step 6: Play-mode verification**

Enter Play Mode and verify:

1. **Crosshair** — static white cross visible at screen center with gap in the middle
2. **Fire at empty space** — small screen shake on each shot, muzzle light flash at barrel
3. **Fire at ground/wall** — medium screen shake, white hitmarker flash
4. **Fire at target dummy** — medium shake on hit, white hitmarker; on kill shot: big shake, camera pitch punch downward, red hitmarker, dummy disappears
5. **Dummy respawn** — dummy reappears after ~2s, can be shot again
6. **Rapid fire** — shakes feel responsive, no audio clipping, hitmarkers don't get stuck
7. **Mouse look** — screen shake does not drift the camera, mouse look feels normal between shakes

- [ ] **Step 7: Commit scene changes**

```bash
git add "Tempest/Assets/Scenes/SampleScene.unity"
git commit -m "feat(weapons): scene wiring — feedback controller, crosshair UI, test target dummies"
```
