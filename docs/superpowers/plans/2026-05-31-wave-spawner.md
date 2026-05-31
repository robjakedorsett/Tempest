# Wave Spawner Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a DRG-style threat director that creates escalating enemy pressure through dynamically-placed NavMesh-sampled rifts, ambient trickle spawning, and elite thresholds.

**Architecture:** A single `WaveDirector` MonoBehaviour driven by a `WaveConfig` ScriptableObject. The director is a pressure tool — external systems call `StartSpawning()` / `StopSpawning()` / `TriggerFinale()` to control it. Rift positions are generated at runtime via NavMesh sampling (no placed spawn points). Enemy tracking decrements via the existing `GameEventBus.OnEnemyKilled` event.

**Tech Stack:** Unity 6 (URP), C#, NavMeshAgent/NavMesh API, Coroutines for spawn timing.

**Spec:** `docs/superpowers/specs/2026-05-31-wave-spawner-design.md`

---

## File Structure

| File | Responsibility |
|------|---------------|
| `Tempest/Assets/Scripts/Spawning/SpawnEntry.cs` | Serializable struct: prefab + weight + tier |
| `Tempest/Assets/Scripts/Spawning/WaveConfig.cs` | ScriptableObject with all tuning parameters |
| `Tempest/Assets/Scripts/Spawning/WaveDirector.cs` | MonoBehaviour: timing, rift generation, spawning, tracking |
| `Tempest/Assets/Scripts/Core/EventBus/GameEventBus.cs` | (modify) Add wave-related events |
| `Tempest/Assets/Scripts/Core/PlayerRegistry.cs` | (modify) Add `GetMinDistanceFromPlayers()` |

---

## Task 1: Data Types (SpawnEntry + WaveConfig)

**Files:**
- Create: `Tempest/Assets/Scripts/Spawning/SpawnEntry.cs`
- Create: `Tempest/Assets/Scripts/Spawning/WaveConfig.cs`

- [ ] **Step 1: Create SpawnEntry**

```csharp
// Tempest/Assets/Scripts/Spawning/SpawnEntry.cs
using System;
using Tempest.Enemies.Enums;
using UnityEngine;

namespace Tempest.Spawning
{
    [Serializable]
    public struct SpawnEntry
    {
        public GameObject prefab;
        public float weight;
        public EnemyTier tier;
    }
}
```

- [ ] **Step 2: Create WaveConfig**

```csharp
// Tempest/Assets/Scripts/Spawning/WaveConfig.cs
using UnityEngine;

namespace Tempest.Spawning
{
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
}
```

- [ ] **Step 3: Verify compilation**

Open Unity Editor, wait for domain reload. Confirm no compile errors in Console. Confirm "Tempest/Waves/Wave Config" appears in the Create Asset menu (right-click in Project window → Create → Tempest → Waves → Wave Config).

- [ ] **Step 4: Commit**

```bash
git add Tempest/Assets/Scripts/Spawning/SpawnEntry.cs Tempest/Assets/Scripts/Spawning/WaveConfig.cs
git commit -m "feat(spawner): add SpawnEntry and WaveConfig data types"
```

---

## Task 2: Infrastructure Changes (GameEventBus + PlayerRegistry)

**Files:**
- Modify: `Tempest/Assets/Scripts/Core/EventBus/GameEventBus.cs`
- Modify: `Tempest/Assets/Scripts/Core/PlayerRegistry.cs`

- [ ] **Step 1: Add wave events to GameEventBus**

Add after line 8 (`public static event Action<int> OnEnemyKilled;`):

```csharp
public static event Action<int> OnSwarmStarted;
public static event Action OnSwarmEnded;
public static event Action<int> OnActiveEnemyCountChanged;
```

Add after line 16 (`public static void RaiseEnemyKilled(int xpValue) => OnEnemyKilled?.Invoke(xpValue);`):

```csharp
public static void RaiseSwarmStarted(int swarmNumber) => OnSwarmStarted?.Invoke(swarmNumber);
public static void RaiseSwarmEnded() => OnSwarmEnded?.Invoke();
public static void RaiseActiveEnemyCountChanged(int count) => OnActiveEnemyCountChanged?.Invoke(count);
```

Final file should look like:

```csharp
using System;

public static class GameEventBus
{
    public static event Action<float> OnPlayerDamaged;
    public static event Action OnPlayerDowned;
    public static event Action OnPlayerRevived;
    public static event Action<int> OnEnemyKilled;
    public static event Action<int> OnSwarmStarted;
    public static event Action OnSwarmEnded;
    public static event Action<int> OnActiveEnemyCountChanged;
    public static event Action OnExtractionStarted;
    public static event Action OnExtractionCompleted;
    public static event Action OnRunFailed;

    public static void RaisePlayerDamaged(float damage) => OnPlayerDamaged?.Invoke(damage);
    public static void RaisePlayerDowned() => OnPlayerDowned?.Invoke();
    public static void RaisePlayerRevived() => OnPlayerRevived?.Invoke();
    public static void RaiseEnemyKilled(int xpValue) => OnEnemyKilled?.Invoke(xpValue);
    public static void RaiseSwarmStarted(int swarmNumber) => OnSwarmStarted?.Invoke(swarmNumber);
    public static void RaiseSwarmEnded() => OnSwarmEnded?.Invoke();
    public static void RaiseActiveEnemyCountChanged(int count) => OnActiveEnemyCountChanged?.Invoke(count);
    public static void RaiseExtractionStarted() => OnExtractionStarted?.Invoke();
    public static void RaiseExtractionCompleted() => OnExtractionCompleted?.Invoke();
    public static void RaiseRunFailed() => OnRunFailed?.Invoke();
}
```

- [ ] **Step 2: Add GetMinDistanceFromPlayers to PlayerRegistry**

Add this method at the end of the `PlayerRegistry` class (before the closing brace on line 99):

```csharp
public static float GetMinDistanceFromPlayers(Vector3 position)
{
    float minDist = float.MaxValue;

    foreach (var player in _players)
    {
        if (player.IsDown) continue;
        float dist = Vector3.Distance(position, player.transform.position);
        if (dist < minDist)
            minDist = dist;
    }

    return minDist;
}
```

- [ ] **Step 3: Verify compilation**

Open Unity Editor, wait for domain reload. Confirm no compile errors.

- [ ] **Step 4: Commit**

```bash
git add Tempest/Assets/Scripts/Core/EventBus/GameEventBus.cs Tempest/Assets/Scripts/Core/PlayerRegistry.cs
git commit -m "feat(core): add wave events to GameEventBus and distance helper to PlayerRegistry"
```

---

## Task 3: WaveDirector — Core Structure + Enemy Tracking + Ambient Trickle

**Files:**
- Create: `Tempest/Assets/Scripts/Spawning/WaveDirector.cs`

This task creates the WaveDirector with: public API, enemy count tracking, ambient trickle spawning, and NavMesh position sampling helpers. Swarm events are added in Task 4.

- [ ] **Step 1: Create WaveDirector with core structure**

```csharp
// Tempest/Assets/Scripts/Spawning/WaveDirector.cs
using System.Collections;
using Tempest.Enemies.Enums;
using UnityEngine;
using UnityEngine.AI;

namespace Tempest.Spawning
{
    public class WaveDirector : MonoBehaviour
    {
        [SerializeField] private WaveConfig config;

        private int _activeEnemyCount;
        private int _ambientActiveCount;
        private bool _isSpawning;
        private Coroutine _ambientCoroutine;
        private Coroutine _swarmCoroutine;

        public int ActiveEnemyCount => _activeEnemyCount;
        public bool IsSpawning => _isSpawning;

        private void OnEnable()
        {
            GameEventBus.OnEnemyKilled += HandleEnemyKilled;
        }

        private void OnDisable()
        {
            GameEventBus.OnEnemyKilled -= HandleEnemyKilled;
        }

        public void StartSpawning()
        {
            if (_isSpawning) return;
            _isSpawning = true;
            _ambientCoroutine = StartCoroutine(AmbientTrickle());
            _swarmCoroutine = StartCoroutine(SwarmLoop());
        }

        public void StopSpawning()
        {
            _isSpawning = false;
            if (_ambientCoroutine != null) StopCoroutine(_ambientCoroutine);
            if (_swarmCoroutine != null) StopCoroutine(_swarmCoroutine);
        }

        public void TriggerFinale()
        {
            if (!_isSpawning) return;
            StopSpawning();
            StartCoroutine(FinaleSequence());
        }

        private void HandleEnemyKilled(int xpValue)
        {
            _activeEnemyCount--;
            if (_activeEnemyCount < 0) _activeEnemyCount = 0;
            GameEventBus.RaiseActiveEnemyCountChanged(_activeEnemyCount);
        }

        private void TrackSpawn(bool isAmbient)
        {
            _activeEnemyCount++;
            if (isAmbient) _ambientActiveCount++;
            GameEventBus.RaiseActiveEnemyCountChanged(_activeEnemyCount);
        }

        private IEnumerator AmbientTrickle()
        {
            while (_isSpawning)
            {
                if (_ambientActiveCount < config.ambientMaxActive)
                {
                    Vector3 pos;
                    if (TryGetSpawnPosition(config.spawnRadius, config.minPlayerDistance, out pos))
                    {
                        SpawnEnemy(EnemyTier.Swarm, pos);
                        TrackSpawn(true);
                    }
                }

                float delay = 1f / config.ambientSpawnRate;
                yield return new WaitForSeconds(delay);
            }
        }

        private IEnumerator SwarmLoop()
        {
            // Implemented in Task 4
            yield break;
        }

        private IEnumerator FinaleSequence()
        {
            // Implemented in Task 5
            yield break;
        }

        private void SpawnEnemy(EnemyTier tier, Vector3 position)
        {
            GameObject prefab = ChooseEnemyByTier(tier);
            if (prefab == null) return;
            Instantiate(prefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        }

        private GameObject ChooseEnemyByTier(EnemyTier tier)
        {
            float totalWeight = 0f;
            foreach (var entry in config.enemies)
            {
                if (entry.tier == tier)
                    totalWeight += entry.weight;
            }

            if (totalWeight <= 0f) return null;

            float roll = Random.value * totalWeight;
            foreach (var entry in config.enemies)
            {
                if (entry.tier != tier) continue;
                if (roll < entry.weight) return entry.prefab;
                roll -= entry.weight;
            }

            // Fallback: return first matching tier
            foreach (var entry in config.enemies)
            {
                if (entry.tier == tier) return entry.prefab;
            }

            return null;
        }

        private bool TryGetSpawnPosition(float radius, float minPlayerDist, out Vector3 result)
        {
            result = Vector3.zero;
            float bestDist = 0f;
            Vector3 bestCandidate = Vector3.zero;
            bool foundAny = false;

            for (int i = 0; i < 10; i++)
            {
                Vector3 randomPoint = transform.position + Random.insideUnitSphere * radius;
                randomPoint.y = transform.position.y;

                NavMeshHit hit;
                if (!NavMesh.SamplePosition(randomPoint, out hit, radius * 0.5f, NavMesh.AllAreas))
                    continue;

                float playerDist = PlayerRegistry.GetMinDistanceFromPlayers(hit.position);

                if (playerDist >= minPlayerDist)
                {
                    result = hit.position;
                    return true;
                }

                if (playerDist > bestDist)
                {
                    bestDist = playerDist;
                    bestCandidate = hit.position;
                    foundAny = true;
                }
            }

            if (foundAny)
            {
                result = bestCandidate;
                return true;
            }

            return false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (config == null) return;

            Gizmos.color = new Color(1f, 0.3f, 0f, 0.15f);
            Gizmos.DrawWireSphere(transform.position, config.spawnRadius);

            Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, config.minPlayerDistance);
        }
#endif
    }
}
```

- [ ] **Step 2: Verify compilation**

Open Unity Editor, wait for domain reload. Confirm no compile errors. The `SwarmLoop` and `FinaleSequence` are stubs that will be filled in Tasks 4 and 5.

- [ ] **Step 3: Quick smoke test**

1. Create a new empty GameObject in the scene named "WaveDirector"
2. Add the `WaveDirector` component
3. Create a WaveConfig asset (Create → Tempest → Waves → Wave Config), assign it
4. Add BasicEnemy prefab to the enemies array (tier = Swarm, weight = 1)
5. Add a temporary script or use `[ContextMenu("Start")]` to call `StartSpawning()` — verify ambient enemies appear on the NavMesh away from the player

- [ ] **Step 4: Commit**

```bash
git add Tempest/Assets/Scripts/Spawning/WaveDirector.cs
git commit -m "feat(spawner): WaveDirector core with ambient trickle and NavMesh sampling"
```

---

## Task 4: WaveDirector — Swarm Events + Rift Generation

**Files:**
- Modify: `Tempest/Assets/Scripts/Spawning/WaveDirector.cs`

Replace the `SwarmLoop` stub with full swarm event logic: decaying timer, rift position generation, clustered spawning over duration, escalating rift count and enemy count.

- [ ] **Step 1: Add swarm state fields**

Add these fields to the class (after the existing field declarations):

```csharp
private int _swarmCount;
private int _currentSwarmActiveCount;
```

- [ ] **Step 2: Replace SwarmLoop with full implementation**

Replace the `SwarmLoop` stub method with:

```csharp
private IEnumerator SwarmLoop()
{
    float currentInterval = config.swarmInterval;

    while (_isSpawning)
    {
        yield return new WaitForSeconds(currentInterval);
        if (!_isSpawning) yield break;

        _swarmCount++;
        _currentSwarmActiveCount = 0;
        GameEventBus.RaiseSwarmStarted(_swarmCount);

        yield return StartCoroutine(ExecuteSwarm(_swarmCount, false));

        currentInterval = Mathf.Max(
            config.minSwarmInterval,
            currentInterval * config.swarmIntervalDecay);
    }
}
```

- [ ] **Step 3: Add ExecuteSwarm method**

Add this method to the class:

```csharp
private IEnumerator ExecuteSwarm(int swarmNumber, bool isFinale)
{
    int riftCount = CalculateRiftCount(swarmNumber, isFinale);
    int enemiesPerRift = CalculateEnemiesPerRift(swarmNumber, isFinale);
    bool includeElite = isFinale || ShouldIncludeElite(swarmNumber);

    for (int r = 0; r < riftCount; r++)
    {
        Vector3 riftCenter;
        if (!TryGetSpawnPosition(config.spawnRadius, config.minPlayerDistance, out riftCenter))
            continue;

        int elitesForThisRift = 0;
        if (includeElite && r == 0)
            elitesForThisRift = isFinale ? config.finaleEliteCount : 1;

        yield return StartCoroutine(SpawnFromRift(riftCenter, enemiesPerRift, elitesForThisRift));
    }

    // Wait for this swarm's enemies to die
    while (_currentSwarmActiveCount > 0)
        yield return null;

    GameEventBus.RaiseSwarmEnded();
}
```

- [ ] **Step 4: Add SpawnFromRift method**

Add this method to the class:

```csharp
private IEnumerator SpawnFromRift(Vector3 riftCenter, int enemyCount, int eliteCount)
{
    int spawned = 0;
    int elitesSpawned = 0;
    float elapsed = 0f;

    // Pre-generate randomized spawn times within the duration
    float[] spawnTimes = new float[enemyCount];
    for (int i = 0; i < enemyCount; i++)
        spawnTimes[i] = Random.Range(0f, config.spawnDuration);
    System.Array.Sort(spawnTimes);

    for (int i = 0; i < enemyCount; i++)
    {
        float waitUntil = spawnTimes[i];
        if (waitUntil > elapsed)
        {
            yield return new WaitForSeconds(waitUntil - elapsed);
            elapsed = waitUntil;
        }

        if (!_isSpawning) yield break;

        Vector3 spawnPos;
        if (!TryGetClusterPosition(riftCenter, config.riftRadius, out spawnPos))
            continue;

        bool spawnElite = elitesSpawned < eliteCount && (enemyCount - i) <= (eliteCount - elitesSpawned);
        if (!spawnElite && elitesSpawned < eliteCount && Random.value < 0.3f)
            spawnElite = true;

        if (spawnElite)
        {
            SpawnEnemy(EnemyTier.Elite, spawnPos);
            elitesSpawned++;
        }
        else
        {
            SpawnEnemy(EnemyTier.Swarm, spawnPos);
        }

        TrackSpawn(false);
        _currentSwarmActiveCount++;
        spawned++;
    }
}
```

- [ ] **Step 5: Add TryGetClusterPosition helper**

Add this method after `TryGetSpawnPosition`:

```csharp
private bool TryGetClusterPosition(Vector3 center, float radius, out Vector3 result)
{
    result = Vector3.zero;

    for (int i = 0; i < 5; i++)
    {
        Vector3 randomPoint = center + Random.insideUnitSphere * radius;
        randomPoint.y = center.y;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, radius, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }
    }

    result = center;
    return true;
}
```

- [ ] **Step 6: Add escalation calculation methods**

Add these methods:

```csharp
private int CalculateRiftCount(int swarmNumber, bool isFinale)
{
    if (isFinale) return config.maxRiftCount;
    int extra = (swarmNumber - 1) / config.riftCountEscalationInterval;
    return Mathf.Min(config.baseRiftCount + extra, config.maxRiftCount);
}

private int CalculateEnemiesPerRift(int swarmNumber, bool isFinale)
{
    if (isFinale) return Mathf.RoundToInt(config.baseEnemiesPerRift * Mathf.Pow(config.enemyCountGrowth, 6));
    return Mathf.RoundToInt(config.baseEnemiesPerRift * Mathf.Pow(config.enemyCountGrowth, swarmNumber - 1));
}

private bool ShouldIncludeElite(int swarmNumber)
{
    if (swarmNumber < config.eliteSwarmThreshold) return false;
    return Random.value < config.eliteChance;
}
```

- [ ] **Step 7: Update HandleEnemyKilled to track swarm count**

Replace the existing `HandleEnemyKilled` method:

```csharp
private void HandleEnemyKilled(int xpValue)
{
    _activeEnemyCount--;
    if (_activeEnemyCount < 0) _activeEnemyCount = 0;

    _currentSwarmActiveCount--;
    if (_currentSwarmActiveCount < 0) _currentSwarmActiveCount = 0;

    _ambientActiveCount--;
    if (_ambientActiveCount < 0) _ambientActiveCount = 0;

    GameEventBus.RaiseActiveEnemyCountChanged(_activeEnemyCount);
}
```

- [ ] **Step 8: Verify compilation + play test**

1. Open Unity Editor, confirm no compile errors
2. Enter play mode with the WaveDirector set up from Task 3
3. Call `StartSpawning()` — verify ambient enemies appear immediately, then after ~45s the first swarm event fires (multiple enemies spawn from a cluster location)
4. Verify swarm enemies spawn away from the player
5. Verify escalation: second swarm should have more enemies

- [ ] **Step 9: Commit**

```bash
git add Tempest/Assets/Scripts/Spawning/WaveDirector.cs
git commit -m "feat(spawner): swarm events with rift generation and escalation"
```

---

## Task 5: WaveDirector — Finale Sequence

**Files:**
- Modify: `Tempest/Assets/Scripts/Spawning/WaveDirector.cs`

Replace the `FinaleSequence` stub with the real implementation.

- [ ] **Step 1: Replace FinaleSequence stub**

Replace the `FinaleSequence` method with:

```csharp
private IEnumerator FinaleSequence()
{
    _isSpawning = true;
    _swarmCount++;
    _currentSwarmActiveCount = 0;
    GameEventBus.RaiseSwarmStarted(_swarmCount);

    yield return StartCoroutine(ExecuteSwarm(_swarmCount, true));

    _isSpawning = false;
}
```

- [ ] **Step 2: Verify compilation + test TriggerFinale**

1. Open Unity Editor, confirm no compile errors
2. Enter play mode, call `StartSpawning()`, wait for 1-2 swarms
3. Call `TriggerFinale()` — verify max rifts spawn with guaranteed elites
4. Verify spawning stops after finale completes

- [ ] **Step 3: Commit**

```bash
git add Tempest/Assets/Scripts/Spawning/WaveDirector.cs
git commit -m "feat(spawner): add TriggerFinale sequence"
```

---

## Task 6: Scene Integration + Verification

**Files:**
- No new code files. Scene setup and play-testing.

- [ ] **Step 1: Create PrototypeWaveConfig asset**

In Unity Editor:
1. Right-click in `Tempest/Assets/ScriptableObjects/` → Create folder "Waves"
2. Right-click in Waves → Create → Tempest → Waves → Wave Config
3. Name it "PrototypeWaveConfig"
4. Configure:
   - Enemies: 2 entries
     - Entry 0: BasicEnemy prefab, weight 1, tier Swarm
     - Entry 1: NagaWarrior prefab, weight 1, tier Elite
   - Swarm Timing: interval 45, decay 0.9, min 20
   - Swarm Scaling: base 8, growth 1.25, base rifts 1, max rifts 3, escalation interval 3
   - Rift Spatial: spawn radius 25, rift radius 8, min player dist 12, duration 40
   - Ambient: rate 0.2, max active 4
   - Elite: threshold 4, chance 0.5, finale count 2

- [ ] **Step 2: Set up WaveDirector in scene**

1. Create empty GameObject at room center named "WaveDirector"
2. Add `WaveDirector` component
3. Assign PrototypeWaveConfig to the config field
4. Add a simple trigger script to call `StartSpawning()` on game start (or use `[ContextMenu]` for manual testing)

- [ ] **Step 3: Full play-test verification**

Enter play mode and verify:
- [ ] Ambient enemies trickle in immediately (1 every ~5s, max 4 alive)
- [ ] First swarm fires at ~45s — cluster of 8 enemies from a rift position
- [ ] Enemies spawn away from player (at least 12 units)
- [ ] Second swarm fires faster (interval * 0.9 = ~40s)
- [ ] Second swarm has more enemies (~10)
- [ ] After swarm 3, a second rift starts appearing
- [ ] After swarm 4, elites may appear
- [ ] `TriggerFinale()` produces max intensity with guaranteed elites
- [ ] `StopSpawning()` halts all spawning, existing enemies persist
- [ ] `GameEventBus` events fire (add temporary Debug.Log listeners to verify)

- [ ] **Step 4: Commit scene changes**

```bash
git add Tempest/Assets/ScriptableObjects/Waves/
git commit -m "feat(spawner): add PrototypeWaveConfig asset"
```

---

## Summary

| Task | What it produces | Commit message |
|------|-----------------|----------------|
| 1 | SpawnEntry + WaveConfig data types | `feat(spawner): add SpawnEntry and WaveConfig data types` |
| 2 | GameEventBus wave events + PlayerRegistry helper | `feat(core): add wave events to GameEventBus and distance helper to PlayerRegistry` |
| 3 | WaveDirector core: ambient trickle, tracking, NavMesh sampling | `feat(spawner): WaveDirector core with ambient trickle and NavMesh sampling` |
| 4 | Swarm events: rift generation, clustered spawning, escalation | `feat(spawner): swarm events with rift generation and escalation` |
| 5 | Finale sequence | `feat(spawner): add TriggerFinale sequence` |
| 6 | Scene integration + play verification | `feat(spawner): add PrototypeWaveConfig asset` |
