# Elite Enemy AI — Design Spec

**Issue:** [#8 Elite Enemy AI — Patrol, chase, attack patterns](https://github.com/robjakedorsett/Tempest/issues/8)  
**Date:** 2026-05-31

## Overview

An elite enemy type (Naga Warrior) with a 6-state hierarchical lifecycle: Spawn → Patrol → Chase → Attack → Death. Uses binary sphere detection, sticky targeting, and a telegraphed melee cone attack. Ported patrol system from ElevatorOut.

## Architecture

### State Machine (Hierarchical)

```
EliteEnemyStateMachine : StateMachine<EnemyStates, EnemyContext>
├── SpawnState (top-level, reused from Swarm)
├── EliteActiveState (super-state)
│   ├── ElitePatrolState (sub-state, default entry)
│   ├── EliteChaseState (sub-state)
│   └── EliteAttackState (sub-state)
└── DeathState (top-level, reused from Swarm)
```

### Transition Rules

**Managed by EliteActiveState (sub-state transitions):**

| From | To | Condition |
|------|----|-----------|
| Patrol | Chase | SphereSensor detects player within `detectionRadius` |
| Chase | Attack | Distance to target <= `attackRange` |
| Attack | Chase | Attack complete AND target outside `attackRange` |
| Chase | Patrol | Target lost for 5+ seconds (outside detection radius continuously) |

**Managed by EliteEnemyStateMachine (top-level):**

| From | To | Condition |
|------|----|-----------|
| Spawn | Active | Spawn timer (0.5s) complete |
| Any | Death | `Context.Health.IsDead` |

### Why Hierarchical

The existing `BaseState` already supports super/sub state relationships (never used by Swarm). Using a super-state for "alive behaviors" means:
- Death can interrupt from any alive state cleanly
- Patrol/Chase/Attack transition logic is self-contained in `EliteActiveState`
- Matches the proven ElevatorOut guard architecture

## Components

### Detection — SphereSensor (existing)

Binary sphere check using the existing `SphereSensor` from `Core/Sensors/`. No FOV cone, no LOS raycast — prototype-appropriate simplicity.

- Detection radius: 18 units (configurable via `EnemyDefinition.detectionRadius`)
- Checked every frame during Patrol state
- Once aggroed: 5-second "aggro stickiness" timer before deaggroing

### Targeting — Sticky

Unlike Swarm's dynamic `PlayerRegistry.GetBestTarget()` retargeting, the elite locks onto whoever triggered the aggro:
- Stores triggering player as `Context.Target`
- Only retargets if current target dies (fallback to `PlayerRegistry.GetBestTarget()`)
- Feels deliberate and threatening — "it's coming for YOU"

### PatrolPath (ported from ElevatorOut)

Waypoint-based patrol system placed in `Scripts/Core/AI/PatrolPaths/`:

**PatrolPath.cs** — MonoBehaviour attached to a GameObject whose children are waypoints.
- `List<Transform> Waypoints` — auto-populated from child transforms
- `PatrolMode Mode` — Loop, PingPong, or Random

**PatrolMode.cs** — Enum: `Loop`, `PingPong`, `Random`

**PatrolPathEditor.cs** — Custom editor in `Editor/` folder:
- Gizmo visualization: lines between waypoints, directional arrows
- Handles in scene view for waypoint positioning

### ElitePatrolState

- On enter: set agent speed to `moveSpeed * 0.5f`, pick next waypoint from assigned PatrolPath
- Frame update: move toward current waypoint via NaturalAgent; when `IsAtDestination()`, advance to next waypoint per PatrolMode
- Detection: run `SphereSensor.Monitor()` each frame; if hit, store detected player as target, signal transition to Chase
- On exit: (no cleanup needed)

### EliteChaseState

- On enter: set agent speed to full `moveSpeed`
- Frame update: set NaturalAgent destination to `Context.Target.transform.position`
- Lost target logic:
  - Each frame, check if target is outside `detectionRadius`
  - If outside: increment `_lostTimer += Time.deltaTime`
  - If re-enters range: reset `_lostTimer = 0`
  - Transition to Patrol when `_lostTimer >= 5f`
- Transition to Attack: `Vector3.Distance(position, target.position) <= attackRange`
- On exit: if transitioning to Patrol, clear `Context.Target`

### EliteAttackState

Three-phase attack cycle:

**Phase 1 — Windup (0.5s):**
- Stop agent movement
- Rotate to face target (540°/s, same as NaturalAgent turn speed)
- Visual telegraph: material color lerp to red/glow (uses existing MaterialPropertyBlock pattern from EnemyHealth hit flash)

**Phase 2 — Strike (instant):**
- `Physics.OverlapSphere(position, attackRange, playerLayer)`
- Filter results by cone angle: `Vector3.Angle(forward, dirToPlayer) < 30f` (60° cone, half-angle = 30°)
- Deal `Context.Definition.damage` to all valid targets via `IDamageable.TakeDamage()`

**Phase 3 — Cooldown (attackCooldown duration, 1.8s for Naga):**
- Remain stationary, face target
- When cooldown expires: check distance to target
  - If within `attackRange`: loop back to Phase 1 (attack again)
  - If outside `attackRange`: transition to Chase

### EnemyDefinition Changes

Add one field to the existing ScriptableObject:

```csharp
[Header("Detection")]
public float detectionRadius = 10f;
```

Default 10 keeps swarm behavior unchanged. Elite (Naga Warrior) asset uses 18.

### Naga Warrior Asset Values

| Field | Value |
|-------|-------|
| enemyName | "Naga Warrior" |
| tier | EnemyTier.Elite |
| maxHealth | 150 |
| moveSpeed | 4.0 |
| damage | 20 |
| attackRange | 2.5 |
| attackCooldown | 1.8 |
| detectionRadius | 18 |
| xpValue | 50 |

## File Structure

### New Files

```
Scripts/Core/AI/PatrolPaths/
├── PatrolPath.cs
└── PatrolMode.cs

Scripts/Core/AI/PatrolPaths/Editor/
└── PatrolPathEditor.cs

Scripts/Enemies/AI/Elite/
├── EliteEnemyStateMachine.cs
└── States/
    ├── EliteActiveState.cs
    ├── ElitePatrolState.cs
    ├── EliteChaseState.cs
    └── EliteAttackState.cs

ScriptableObjects/Enemies/
└── NagaWarrior.asset
```

### Modified Files

```
Scripts/Enemies/Data/EnemyDefinition.cs  — add detectionRadius field
```

### Reused (no changes)

- `SpawnState.cs` — same 0.5s spawn timer
- `DeathState.cs` — same death trigger
- `EnemyContext.cs` — already has Target, Health, Agent, Definition
- `EnemyHealth.cs` — works as-is (high HP is just a ScriptableObject value)
- `NaturalAgent.cs` — works as-is
- `SphereSensor.cs` — works as-is
- `EnemyStates.cs` — already has `Patrol` enum value defined

## Visual Telegraph

The windup telegraph uses the same `MaterialPropertyBlock` pattern as EnemyHealth's hit flash:
- Lerp `_BaseColor` toward a warning color (red/orange) over 0.5s
- Reset to original color when strike fires
- This is prototype-quality — a proper animation/VFX pass replaces it later

## Edge Cases

- **Target dies mid-chase:** Fallback to `PlayerRegistry.GetBestTarget()`. If no players available (all dead), transition to Patrol (idle at last position).
- **Target dies mid-attack:** Cancel attack, transition to Chase (which will then either find a new target or return to Patrol).
- **Elite spawns with no PatrolPath assigned:** Log warning, stay in Patrol state but remain stationary (acts like Idle).
- **Multiple players in detection sphere:** Lock onto the closest one (first detection). Sticky from there.
- **Elite killed during windup:** Death transition interrupts immediately via top-level state machine check.

## Out of Scope

- Weak points / headshot multiplier (stretch goal per issue, skip for now)
- Animation integration (prototype uses placeholder capsule + material telegraph)
- Sound effects for telegraph/attack
- Networked sync (will be added when Netcode pass happens)
