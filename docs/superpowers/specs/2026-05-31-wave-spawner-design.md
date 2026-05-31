# Wave Spawner — Escalating Enemy Waves (DRG-style Threat Director)

**Issue:** #9
**Date:** 2026-05-31

## Overview

A time-driven wave spawner that creates escalating enemy pressure through dynamically-placed "rifts" — NavMesh-sampled spawn clusters that appear in different locations each swarm event. Inspired by Deep Rock Galactic's organic ebb-and-flow rather than rigid wave-clear gating.

The spawner is a **pressure tool** controlled by external systems (objectives, extraction, room encounters). It doesn't own its end condition — it escalates until told to stop.

## Architecture

Two new types, no scene-placed spawn points:

- **`WaveDirector`** (MonoBehaviour) — the brain. Manages elapsed time, fires swarm events on a decaying timer, maintains ambient trickle, generates rift positions, instantiates enemies, tracks active count.
- **`WaveConfig`** (ScriptableObject) — all tuning. Escalation rates, enemy pools, rift parameters, ambient config, elite thresholds.

### Flow

```
WaveDirector.StartSpawning() called
  → ambient trickle begins (low-rate spawns, capped)
  → swarm timer starts

Timer fires → swarm event:
  1. Pick 1-N rift positions (NavMesh sample, min distance from players)
  2. For each rift: sample spawn point cluster within radius
  3. Spawn enemies from cluster over spawnDuration (randomized intervals)
  4. Optional: instantiate rift VFX at position

Escalation over time:
  - Swarm interval decreases (decaying timer)
  - Enemies per rift increases (growth multiplier)
  - More rifts per swarm (added every N swarms)
  - Elites appear after threshold swarm count

External call → TriggerFinale()
  → max rifts, guaranteed elites, max intensity
  → auto-stops after finale spawns complete
```

## WaveConfig ScriptableObject

```csharp
[CreateAssetMenu(menuName = "Tempest/Waves/Wave Config")]
public class WaveConfig : ScriptableObject
{
    [Header("Enemy Pool")]
    public SpawnEntry[] enemies;

    [Header("Swarm Timing")]
    public float swarmInterval = 45f;
    public float swarmIntervalDecay = 0.9f;
    public float minSwarmInterval = 20f;

    [Header("Swarm Scaling")]
    public int baseEnemiesPerRift = 8;
    public float enemyCountGrowth = 1.25f;
    public int baseRiftCount = 1;
    public int maxRiftCount = 3;
    public int riftCountEscalationInterval = 3;

    [Header("Rift Spatial")]
    public float spawnRadius = 25f;
    public float riftRadius = 8f;
    public float minPlayerDistance = 12f;
    public float spawnDuration = 40f;

    [Header("Ambient")]
    public float ambientSpawnRate = 0.2f;
    public int ambientMaxActive = 4;

    [Header("Elite")]
    public int eliteSwarmThreshold = 4;
    public float eliteChance = 0.5f;
    public int finaleEliteCount = 2;
}
```

## SpawnEntry

```csharp
[Serializable]
public struct SpawnEntry
{
    public GameObject prefab;
    public float weight;
    public EnemyTier tier;
}
```

The director filters by tier at runtime: ambient/swarm draws from `Swarm` entries, elite slots draw from `Elite` entries. Weighted random selection.

## WaveDirector

### Public API

```csharp
public void StartSpawning()     // begins encounter — ambient + swarm timer
public void StopSpawning()      // halts all spawning (alive enemies persist)
public void TriggerFinale()     // max rifts, guaranteed elites, then auto-stops
public int ActiveEnemyCount { get; }
```

### Rift Position Generation

The director samples rift positions within a configurable `spawnRadius` from its own transform position (center of the room). For prototype, place the director GameObject at the room center.

1. Determine rift count for current swarm (baseRiftCount + escalation)
2. For each rift: pick random point within `spawnRadius` of director, then `NavMesh.SamplePosition()` to snap to walkable surface
3. Validate: NavMesh hit succeeded AND position is at least `minPlayerDistance` from all living players
4. Retry up to 10 times, fall back to best-distance candidate found
5. Optionally instantiate rift VFX prefab at position

### Spawn Point Clustering

- For each rift center, sample N positions within `riftRadius` via NavMesh
- No player-distance check needed (rift center already validated)
- Enemies instantiated at these positions, staggered over `spawnDuration`
- Spawn intervals randomized within the duration window (not fixed cadence)

### Active Enemy Tracking

- Increment counter on each instantiation
- Subscribe to `GameEventBus.OnEnemyKilled` to decrement
- Fire `GameEventBus.RaiseActiveEnemyCountChanged()` on change
- Swarm-specific tracking: fire `OnSwarmEnded` when a swarm's enemies are all dead

### Ambient Trickle

- Between swarm events, spawn at `ambientSpawnRate` (enemies/second)
- Only from `Swarm` tier entries
- Capped at `ambientMaxActive` concurrent ambient enemies
- Uses same NavMesh sampling but single positions (no rift cluster)

### Elite Spawning

- After `eliteSwarmThreshold` swarms, each subsequent swarm has `eliteChance` probability of including an elite
- Elite spawns as one of the rift's enemies (replaces a swarm slot)
- `TriggerFinale()` guarantees `finaleEliteCount` elites
- Elite = the "boss" for prototype purposes (EnemyTier.Elite)

## GameEventBus Additions

```csharp
public static event Action<int> OnSwarmStarted;           // swarm number
public static event Action OnSwarmEnded;                  // swarm's enemies all dead
public static event Action<int> OnActiveEnemyCountChanged; // current alive count

public static void RaiseSwarmStarted(int swarmNumber) => OnSwarmStarted?.Invoke(swarmNumber);
public static void RaiseSwarmEnded() => OnSwarmEnded?.Invoke();
public static void RaiseActiveEnemyCountChanged(int count) => OnActiveEnemyCountChanged?.Invoke(count);
```

## PlayerRegistry Addition

```csharp
public static float GetMinDistanceFromPlayers(Vector3 position)
```

Returns distance to the closest living player. Used for rift position validation.

## File Plan

### New Files

| Path | Type |
|------|------|
| `Scripts/Spawning/WaveDirector.cs` | MonoBehaviour |
| `Scripts/Spawning/WaveConfig.cs` | ScriptableObject |
| `Scripts/Spawning/SpawnEntry.cs` | Serializable struct |

### Modified Files

| Path | Change |
|------|--------|
| `Scripts/Core/EventBus/GameEventBus.cs` | Add wave events |
| `Scripts/Core/PlayerRegistry.cs` | Add `GetMinDistanceFromPlayers()` |

### Assets (created in editor)

| Path | Type |
|------|------|
| `ScriptableObjects/Waves/PrototypeWaveConfig.asset` | WaveConfig instance |

## Prototype Scope

- WaveDirector with full escalation logic
- NavMesh-sampled rift positions (no hand-placed origins)
- 2 enemy types in pool: BasicEnemy (swarm), NagaWarrior (elite)
- Ambient trickle between swarms
- Elites appear after swarm 4
- TriggerFinale() support for external systems
- Events for HUD integration
- No rift VFX (placeholder for later)
- No networking (single-player host for now)

## Dependencies

- #7 (Swarm Enemy AI) — completed, BasicEnemy exists
- #8 (Elite Enemy AI) — completed, NagaWarrior exists
- NavMesh baked on prototype room floor
