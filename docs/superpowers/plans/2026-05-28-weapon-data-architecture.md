# Weapon Data Architecture Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create the data layer for weapons and loadouts — ScriptableObject definitions, a weapon pool config, a PlayerLoadout class, and starter weapon assets.

**Architecture:** Pure data SOs (WeaponDefinition, DeployableDefinition) hold weapon config. WeaponPoolConfig SO references all definitions and provides filtered queries. PlayerLoadout is a plain C# class added to PlayerContext. No firing mechanics, no UI — data only.

**Tech Stack:** Unity 6, C# ScriptableObjects, no external dependencies.

**Spec:** `docs/superpowers/specs/2026-05-28-weapon-data-architecture-design.md`

---

### Task 1: WeaponSlot and WeaponType Enums

**Files:**
- Create: `Tempest/Assets/Scripts/Weapons/Enums/WeaponEnums.cs`

This is a Unity project without a test runner configured, and these are simple enum definitions. No tests for this task — correctness is verified by compilation in subsequent tasks.

- [ ] **Step 1: Create the enums file**

Create `Tempest/Assets/Scripts/Weapons/Enums/WeaponEnums.cs`:

```csharp
namespace Tempest.Weapons
{
    public enum WeaponSlot
    {
        Primary,
        Secondary
    }

    public enum WeaponType
    {
        Hitscan,
        Projectile
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Tempest/Assets/Scripts/Weapons/Enums/WeaponEnums.cs
git commit -m "feat(weapons): add WeaponSlot and WeaponType enums"
```

---

### Task 2: WeaponDefinition ScriptableObject

**Files:**
- Create: `Tempest/Assets/Scripts/Weapons/Data/WeaponDefinition.cs`

- [ ] **Step 1: Create the WeaponDefinition SO**

Create `Tempest/Assets/Scripts/Weapons/Data/WeaponDefinition.cs`:

```csharp
using UnityEngine;

namespace Tempest.Weapons
{
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "Tempest/Weapons/Weapon Definition")]
    public class WeaponDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string weaponName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        public WeaponSlot slot;
        public WeaponType type;

        [Header("Stats")]
        public float damage;
        public float fireRate;
        public int magazineSize;
        public float reloadTime;
        public float range;
        public float spread;

        [Header("Effects")]
        public GameObject muzzleFlashPrefab;
        public GameObject hitEffectPrefab;
        public AudioClip fireSound;
        public AudioClip reloadSound;

        [Header("Meta")]
        public bool unlockedByDefault = true;
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Tempest/Assets/Scripts/Weapons/Data/WeaponDefinition.cs
git commit -m "feat(weapons): add WeaponDefinition ScriptableObject"
```

---

### Task 3: DeployableDefinition ScriptableObject

**Files:**
- Create: `Tempest/Assets/Scripts/Weapons/Data/DeployableDefinition.cs`

- [ ] **Step 3: Create the DeployableDefinition SO**

Create `Tempest/Assets/Scripts/Weapons/Data/DeployableDefinition.cs`:

```csharp
using UnityEngine;

namespace Tempest.Weapons
{
    [CreateAssetMenu(fileName = "NewDeployable", menuName = "Tempest/Weapons/Deployable Definition")]
    public class DeployableDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string deployableName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;

        [Header("Config")]
        public GameObject deployablePrefab;
        public float cooldown;
        public int maxCharges;

        [Header("Meta")]
        public bool unlockedByDefault = true;
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Tempest/Assets/Scripts/Weapons/Data/DeployableDefinition.cs
git commit -m "feat(weapons): add DeployableDefinition ScriptableObject"
```

---

### Task 4: WeaponPoolConfig ScriptableObject

**Files:**
- Create: `Tempest/Assets/Scripts/Weapons/Data/WeaponPoolConfig.cs`

- [ ] **Step 1: Create the WeaponPoolConfig SO**

Create `Tempest/Assets/Scripts/Weapons/Data/WeaponPoolConfig.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tempest.Weapons
{
    [CreateAssetMenu(fileName = "WeaponPoolConfig", menuName = "Tempest/Weapons/Weapon Pool Config")]
    public class WeaponPoolConfig : ScriptableObject
    {
        public List<WeaponDefinition> allWeapons = new();
        public List<DeployableDefinition> allDeployables = new();

        public List<WeaponDefinition> GetAvailableWeapons(WeaponSlot slot)
        {
            return allWeapons
                .Where(w => w != null && w.slot == slot && w.unlockedByDefault)
                .ToList();
        }

        public List<DeployableDefinition> GetAvailableDeployables()
        {
            return allDeployables
                .Where(d => d != null && d.unlockedByDefault)
                .ToList();
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Tempest/Assets/Scripts/Weapons/Data/WeaponPoolConfig.cs
git commit -m "feat(weapons): add WeaponPoolConfig ScriptableObject with filtering"
```

---

### Task 5: PlayerLoadout and PlayerContext Integration

**Files:**
- Create: `Tempest/Assets/Scripts/Weapons/PlayerLoadout.cs`
- Modify: `Tempest/Assets/Scripts/Player/Context/PlayerContext.cs`

- [ ] **Step 1: Create the PlayerLoadout class**

Create `Tempest/Assets/Scripts/Weapons/PlayerLoadout.cs`:

```csharp
namespace Tempest.Weapons
{
    public class PlayerLoadout
    {
        public WeaponDefinition PrimaryWeapon { get; }
        public WeaponDefinition SecondaryWeapon { get; }
        public DeployableDefinition Consumable { get; }

        public PlayerLoadout(WeaponDefinition primary, WeaponDefinition secondary, DeployableDefinition consumable = null)
        {
            PrimaryWeapon = primary;
            SecondaryWeapon = secondary;
            Consumable = consumable;
        }

        public bool IsValid()
        {
            return PrimaryWeapon != null
                && SecondaryWeapon != null
                && PrimaryWeapon.slot == WeaponSlot.Primary
                && SecondaryWeapon.slot == WeaponSlot.Secondary;
        }
    }
}
```

- [ ] **Step 2: Add Loadout property to PlayerContext**

Modify `Tempest/Assets/Scripts/Player/Context/PlayerContext.cs`. The full file should read:

```csharp
using Tempest.Weapons;

public class PlayerContext
{
    public PlayerContext(PlayerMotor motor, PlayerInput input, PlayerStance stance, PlayerHealth health)
    {
        Motor = motor;
        Input = input;
        Stance = stance;
        Health = health;
    }

    public PlayerMotor Motor { get; set; }
    public PlayerInput Input { get; set; }
    public PlayerStance Stance { get; set; }
    public PlayerHealth Health { get; set; }
    public PlayerLoadout Loadout { get; set; }
}
```

Changes from original:
- Added `using Tempest.Weapons;` at top
- Added `public PlayerLoadout Loadout { get; set; }` after Health

Loadout is not a constructor parameter — it's set separately after construction because weapon assignment happens at a different lifecycle point (pre-run selection) than player spawning. For the prototype, it will be hardcoded in `PlayerMovementStateMachine.Awake()` or a future bootstrap component.

- [ ] **Step 3: Commit**

```bash
git add Tempest/Assets/Scripts/Weapons/PlayerLoadout.cs Tempest/Assets/Scripts/Player/Context/PlayerContext.cs
git commit -m "feat(weapons): add PlayerLoadout class and integrate with PlayerContext"
```

---

### Task 6: Create Starter Weapon ScriptableObject Assets

**Files:**
- Create: `Tempest/Assets/ScriptableObjects/Weapons/SpiritCannon.asset`
- Create: `Tempest/Assets/ScriptableObjects/Weapons/NagaFangRifle.asset`
- Create: `Tempest/Assets/ScriptableObjects/Weapons/BonePistol.asset`
- Create: `Tempest/Assets/ScriptableObjects/Weapons/FireTotem.asset`
- Create: `Tempest/Assets/ScriptableObjects/Weapons/WeaponPoolConfig.asset`

Unity ScriptableObject `.asset` files are YAML serialized by Unity's asset pipeline. They cannot be hand-authored reliably — they require Unity-generated GUIDs for the `m_Script` reference that ties the asset to its C# class. These assets **must be created through the Unity Editor**.

- [ ] **Step 1: Create assets via Unity Editor**

Open the Unity project. In the Project window:

1. Right-click `Assets/` → Create → Folder → name it `ScriptableObjects`
2. Inside `ScriptableObjects/`, create folder `Weapons`
3. Right-click `Assets/ScriptableObjects/Weapons/` → Create → Tempest → Weapons → Weapon Definition. Name it `SpiritCannon`. Set fields in Inspector:
   - weaponName: `Spirit Cannon`
   - description: `Standard-issue spirit energy cannon. Reliable and versatile.`
   - slot: `Primary`
   - type: `Hitscan`
   - damage: `25`
   - fireRate: `5`
   - magazineSize: `30`
   - reloadTime: `1.5`
   - range: `50`
   - spread: `2`
   - unlockedByDefault: `true`
   - (Leave icon, prefabs, and audio clips as None — populated when art assets exist)

4. Create another Weapon Definition. Name it `NagaFangRifle`. Set fields:
   - weaponName: `Naga Fang Rifle`
   - description: `Serpent-tooth precision rifle. Hits hard, fires slow.`
   - slot: `Primary`
   - type: `Hitscan`
   - damage: `60`
   - fireRate: `1.5`
   - magazineSize: `8`
   - reloadTime: `2`
   - range: `80`
   - spread: `0.5`
   - unlockedByDefault: `true`

5. Create another Weapon Definition. Name it `BonePistol`. Set fields:
   - weaponName: `Bone Pistol`
   - description: `Rapid-fire sidearm carved from ancient bone.`
   - slot: `Secondary`
   - type: `Hitscan`
   - damage: `12`
   - fireRate: `8`
   - magazineSize: `15`
   - reloadTime: `1`
   - range: `30`
   - spread: `3`
   - unlockedByDefault: `true`

6. Right-click `Assets/ScriptableObjects/Weapons/` → Create → Tempest → Weapons → Deployable Definition. Name it `FireTotem`. Set fields:
   - deployableName: `Fire Totem`
   - description: `Deployable spirit flame that scorches nearby enemies.`
   - cooldown: `10`
   - maxCharges: `3`
   - unlockedByDefault: `true`

7. Right-click `Assets/ScriptableObjects/Weapons/` → Create → Tempest → Weapons → Weapon Pool Config. Name it `WeaponPoolConfig`. In Inspector:
   - allWeapons: drag in `SpiritCannon`, `NagaFangRifle`, `BonePistol`
   - allDeployables: drag in `FireTotem`

- [ ] **Step 2: Verify asset creation**

In Unity Editor, select `WeaponPoolConfig` asset. Confirm:
- allWeapons list shows 3 entries (Spirit Cannon, Naga Fang Rifle, Bone Pistol)
- allDeployables list shows 1 entry (Fire Totem)
- Each weapon asset's fields match the values above when selected individually

- [ ] **Step 3: Commit**

```bash
git add Tempest/Assets/ScriptableObjects/
git commit -m "feat(weapons): add starter weapon and deployable SO assets"
```

---

### Task 7: Final Verification and Cleanup

- [ ] **Step 1: Verify folder structure**

Confirm the following files exist:

```
Tempest/Assets/Scripts/Weapons/
├── Enums/
│   └── WeaponEnums.cs
├── Data/
│   ├── WeaponDefinition.cs
│   ├── DeployableDefinition.cs
│   └── WeaponPoolConfig.cs
└── PlayerLoadout.cs

Tempest/Assets/ScriptableObjects/Weapons/
├── SpiritCannon.asset
├── NagaFangRifle.asset
├── BonePistol.asset
├── FireTotem.asset
└── WeaponPoolConfig.asset
```

- [ ] **Step 2: Verify PlayerContext integration**

Open `Tempest/Assets/Scripts/Player/Context/PlayerContext.cs` and confirm it has:
- `using Tempest.Weapons;` at top
- `public PlayerLoadout Loadout { get; set; }` property

- [ ] **Step 3: Verify Unity compiles without errors**

In Unity Editor, check the Console window. There should be zero compile errors. If there are errors, fix them before proceeding.

- [ ] **Step 4: Quick smoke test in Unity**

In Unity Editor:
1. Select `WeaponPoolConfig` asset
2. Verify all 3 weapons and 1 deployable are referenced
3. Enter Play mode briefly — no errors should appear in Console

- [ ] **Step 5: Verify acceptance criteria from issue #1**

Check off against the issue:
- [x] WeaponDefinition SO created with all fields
- [x] At least 2-3 weapon SO assets configured (we have 3 weapons + 1 deployable)
- [x] PlayerLoadout holds weapon assignments
- [x] Weapon data accessible from player context (PlayerContext.Loadout)
