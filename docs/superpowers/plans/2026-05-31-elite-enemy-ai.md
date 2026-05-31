# Elite Enemy AI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement an elite enemy (Naga Warrior) with patrol, chase, and telegraphed cone attack using a hierarchical state machine.

**Architecture:** Hierarchical state machine — `EliteActiveState` super-state contains Patrol/Chase/Attack sub-states, Death interrupts from any state. PatrolPath system ported from ElevatorOut provides waypoint-based patrol behavior. Binary sphere detection with sticky targeting.

**Tech Stack:** Unity 6, C#, NavMeshAgent, existing StateMachine/BaseState framework, SphereSensor, PlayerRegistry

---

## File Map

| File | Responsibility |
|------|---------------|
| `Scripts/Enemies/Enums/EnemyStates.cs` | Add `Active` enum value |
| `Scripts/Enemies/Data/EnemyDefinition.cs` | Add `detectionRadius` field |
| `Scripts/Core/AI/PatrolPaths/PatrolMode.cs` | Patrol traversal mode enum |
| `Scripts/Core/AI/PatrolPaths/PatrolPath.cs` | Waypoint container MonoBehaviour with gizmos |
| `Scripts/Enemies/AI/Elite/EliteEnemyStateMachine.cs` | Top-level state machine, wires states + transitions |
| `Scripts/Enemies/AI/Elite/States/EliteActiveState.cs` | Super-state managing Patrol/Chase/Attack sub-states |
| `Scripts/Enemies/AI/Elite/States/ElitePatrolState.cs` | Waypoint following + sphere detection |
| `Scripts/Enemies/AI/Elite/States/EliteChaseState.cs` | Pursue sticky target, lost-target timer |
| `Scripts/Enemies/AI/Elite/States/EliteAttackState.cs` | 3-phase telegraphed cone attack |

All paths are relative to `Tempest/Assets/`.

---

### Task 1: Add `Active` to EnemyStates Enum + `detectionRadius` to EnemyDefinition

**Files:**
- Modify: `Tempest/Assets/Scripts/Enemies/Enums/EnemyStates.cs`
- Modify: `Tempest/Assets/Scripts/Enemies/Data/EnemyDefinition.cs`

- [ ] **Step 1: Add `Active` to the enum**

In `Scripts/Enemies/Enums/EnemyStates.cs`, add `Active` after `Retreat`:

```csharp
namespace Tempest.Enemies.Enums
{
    public enum EnemyStates
    {
        Spawn,
        Idle,
        Chase,
        Attack,
        Death,
        Patrol,
        Charge,
        Retreat,
        Active
    }
}
```

- [ ] **Step 2: Add `detectionRadius` to EnemyDefinition**

In `Scripts/Enemies/Data/EnemyDefinition.cs`, add after the `attackCooldown` field:

```csharp
using Tempest.Enemies.Enums;
using UnityEngine;

namespace Tempest.Enemies
{
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "Tempest/Enemies/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string enemyName;
        public EnemyTier tier;

        [Header("Stats")]
        public float maxHealth = 100f;
        public float moveSpeed = 3.5f;
        public float damage = 10f;
        public float attackRange = 2f;
        public float attackCooldown = 1f;
        public float detectionRadius = 10f;

        [Header("Rewards")]
        public int xpValue = 10;

        [Header("Effects")]
        public GameObject deathEffectPrefab;
        public GameObject hitEffectPrefab;
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add Tempest/Assets/Scripts/Enemies/Enums/EnemyStates.cs Tempest/Assets/Scripts/Enemies/Data/EnemyDefinition.cs
git commit -m "feat(enemies): add Active state and detectionRadius to EnemyDefinition"
```

---

### Task 2: PatrolPath System

**Files:**
- Create: `Tempest/Assets/Scripts/Core/AI/PatrolPaths/PatrolMode.cs`
- Create: `Tempest/Assets/Scripts/Core/AI/PatrolPaths/PatrolPath.cs`

- [ ] **Step 1: Create PatrolMode enum**

Create `Scripts/Core/AI/PatrolPaths/PatrolMode.cs`:

```csharp
namespace Tempest.AI
{
    public enum PatrolMode
    {
        Loop,
        PingPong,
        Random
    }
}
```

- [ ] **Step 2: Create PatrolPath MonoBehaviour**

Create `Scripts/Core/AI/PatrolPaths/PatrolPath.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Tempest.AI
{
    public class PatrolPath : MonoBehaviour
    {
        public List<Transform> Waypoints = new();
        public PatrolMode Mode = PatrolMode.Loop;

        private int _currentIndex;
        private int _direction = 1;

        public Transform CurrentWaypoint =>
            Waypoints.Count > 0 ? Waypoints[_currentIndex] : null;

        public void ResetPath()
        {
            _currentIndex = 0;
            _direction = 1;
        }

        public Transform AdvanceWaypoint()
        {
            if (Waypoints.Count == 0) return null;

            switch (Mode)
            {
                case PatrolMode.Loop:
                    _currentIndex = (_currentIndex + 1) % Waypoints.Count;
                    break;

                case PatrolMode.PingPong:
                    _currentIndex += _direction;
                    if (_currentIndex >= Waypoints.Count - 1 || _currentIndex <= 0)
                        _direction *= -1;
                    _currentIndex = Mathf.Clamp(_currentIndex, 0, Waypoints.Count - 1);
                    break;

                case PatrolMode.Random:
                    int prev = _currentIndex;
                    if (Waypoints.Count > 1)
                    {
                        do { _currentIndex = Random.Range(0, Waypoints.Count); }
                        while (_currentIndex == prev);
                    }
                    break;
            }

            return Waypoints[_currentIndex];
        }

        private void Reset()
        {
            Waypoints.Clear();
            foreach (Transform child in transform)
                Waypoints.Add(child);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Waypoints == null || Waypoints.Count == 0) return;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < Waypoints.Count; i++)
            {
                Transform wp = Waypoints[i];
                if (wp == null) continue;

                Gizmos.DrawSphere(wp.position, 0.25f);

                if (i < Waypoints.Count - 1)
                {
                    Transform next = Waypoints[i + 1];
                    if (next != null)
                        Gizmos.DrawLine(wp.position, next.position);
                }
                else if (Mode == PatrolMode.Loop)
                {
                    Transform first = Waypoints[0];
                    if (first != null)
                        Gizmos.DrawLine(wp.position, first.position);
                }
            }
        }
#endif
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add Tempest/Assets/Scripts/Core/AI/PatrolPaths/
git commit -m "feat(ai): add PatrolPath system with Loop/PingPong/Random modes"
```

---

### Task 3: EliteActiveState (Super-State)

**Files:**
- Create: `Tempest/Assets/Scripts/Enemies/AI/Elite/States/EliteActiveState.cs`

- [ ] **Step 1: Create EliteActiveState**

Create `Scripts/Enemies/AI/Elite/States/EliteActiveState.cs`:

```csharp
using Tempest.Enemies.Enums;
using UnityEngine;

namespace Tempest.Enemies.Elite
{
    public class EliteActiveState : BaseState<EnemyStates, EnemyContext>
    {
        public EliteActiveState(StateMachine<EnemyStates, EnemyContext> stateMachine)
            : base(EnemyStates.Active, stateMachine) { }

        public override void EnterState()
        {
            base.EnterState();
            SetSubState(EnemyStates.Patrol);
        }

        public override void FrameUpdate()
        {
            base.FrameUpdate();
        }
    }
}
```

The transition logic between sub-states (Patrol→Chase, Chase→Attack, etc.) will be wired via the fluent `FromThis()` builder in `EliteEnemyStateMachine` after all states are created, since the conditions reference runtime state from the sub-state instances.

- [ ] **Step 2: Commit**

```bash
git add Tempest/Assets/Scripts/Enemies/AI/Elite/States/EliteActiveState.cs
git commit -m "feat(enemies): add EliteActiveState super-state"
```

---

### Task 4: ElitePatrolState

**Files:**
- Create: `Tempest/Assets/Scripts/Enemies/AI/Elite/States/ElitePatrolState.cs`

- [ ] **Step 1: Create ElitePatrolState**

Create `Scripts/Enemies/AI/Elite/States/ElitePatrolState.cs`:

```csharp
using Tempest.AI;
using Tempest.Enemies.Enums;
using UnityEngine;

namespace Tempest.Enemies.Elite
{
    public class ElitePatrolState : BaseState<EnemyStates, EnemyContext>
    {
        private readonly PatrolPath _patrolPath;
        private readonly SphereSensor _detectionSensor;
        private readonly Transform _transform;

        private bool _playerDetected;

        public bool PlayerDetected => _playerDetected;

        public ElitePatrolState(
            StateMachine<EnemyStates, EnemyContext> stateMachine,
            PatrolPath patrolPath,
            SphereSensor detectionSensor)
            : base(EnemyStates.Patrol, stateMachine)
        {
            _patrolPath = patrolPath;
            _detectionSensor = detectionSensor;
            _transform = stateMachine.transform;
        }

        public override void EnterState()
        {
            base.EnterState();
            _playerDetected = false;

            float patrolSpeed = Context.Definition.moveSpeed * 0.5f;
            Context.Agent.Initialize(patrolSpeed);

            if (_patrolPath != null && _patrolPath.CurrentWaypoint != null)
                Context.Agent.SetDestination(_patrolPath.CurrentWaypoint.position);
        }

        public override void FrameUpdate()
        {
            base.FrameUpdate();
            if (IsExitingState) return;

            _detectionSensor.Monitor();
            if (_detectionSensor.Hit)
            {
                AcquireTarget();
                if (Context.Target != null)
                {
                    _playerDetected = true;
                    return;
                }
            }

            if (_patrolPath == null) return;

            if (Context.Agent.IsAtDestination())
            {
                Transform next = _patrolPath.AdvanceWaypoint();
                if (next != null)
                    Context.Agent.SetDestination(next.position);
            }
        }

        private void AcquireTarget()
        {
            PlayerHealth nearest = PlayerRegistry.GetNearestPlayer(_transform.position);
            if (nearest == null || nearest.IsDown) return;

            float dist = Vector3.Distance(_transform.position, nearest.transform.position);
            if (dist <= Context.Definition.detectionRadius)
            {
                Context.Target = nearest;
                PlayerRegistry.AssignTarget(nearest);
            }
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Tempest/Assets/Scripts/Enemies/AI/Elite/States/ElitePatrolState.cs
git commit -m "feat(enemies): add ElitePatrolState with waypoint following and sphere detection"
```

---

### Task 5: EliteChaseState

**Files:**
- Create: `Tempest/Assets/Scripts/Enemies/AI/Elite/States/EliteChaseState.cs`

- [ ] **Step 1: Create EliteChaseState**

Create `Scripts/Enemies/AI/Elite/States/EliteChaseState.cs`:

```csharp
using Tempest.Enemies.Enums;
using UnityEngine;

namespace Tempest.Enemies.Elite
{
    public class EliteChaseState : BaseState<EnemyStates, EnemyContext>
    {
        private const float LostTargetDuration = 5f;

        private readonly Transform _transform;
        private float _lostTimer;
        private bool _targetLost;

        public bool TargetLost => _targetLost;
        public bool InAttackRange { get; private set; }

        public EliteChaseState(StateMachine<EnemyStates, EnemyContext> stateMachine)
            : base(EnemyStates.Chase, stateMachine)
        {
            _transform = stateMachine.transform;
        }

        public override void EnterState()
        {
            base.EnterState();
            _lostTimer = 0f;
            _targetLost = false;
            InAttackRange = false;

            Context.Agent.Initialize(Context.Definition.moveSpeed);
        }

        public override void FrameUpdate()
        {
            base.FrameUpdate();
            if (IsExitingState) return;

            if (Context.Target == null || Context.Target.IsDown)
            {
                RetargetOrLose();
                return;
            }

            Vector3 targetPos = Context.Target.transform.position;
            Context.Agent.SetDestination(targetPos);

            float dist = Vector3.Distance(_transform.position, targetPos);
            InAttackRange = dist <= Context.Definition.attackRange;

            if (dist > Context.Definition.detectionRadius)
            {
                _lostTimer += Time.deltaTime;
                if (_lostTimer >= LostTargetDuration)
                {
                    _targetLost = true;
                    PlayerRegistry.ReleaseTarget(Context.Target);
                    Context.Target = null;
                }
            }
            else
            {
                _lostTimer = 0f;
            }
        }

        public override void ExitState()
        {
            base.ExitState();
            Context.Agent.Stop();
        }

        private void RetargetOrLose()
        {
            PlayerHealth newTarget = PlayerRegistry.GetBestTarget(_transform.position);
            if (newTarget != null)
            {
                Context.Target = newTarget;
                PlayerRegistry.AssignTarget(newTarget);
                _lostTimer = 0f;
            }
            else
            {
                _targetLost = true;
                Context.Target = null;
            }
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Tempest/Assets/Scripts/Enemies/AI/Elite/States/EliteChaseState.cs
git commit -m "feat(enemies): add EliteChaseState with sticky targeting and lost-target timer"
```

---

### Task 6: EliteAttackState

**Files:**
- Create: `Tempest/Assets/Scripts/Enemies/AI/Elite/States/EliteAttackState.cs`

- [ ] **Step 1: Create EliteAttackState**

Create `Scripts/Enemies/AI/Elite/States/EliteAttackState.cs`:

```csharp
using Tempest.Enemies.Enums;
using Tempest.Weapons;
using UnityEngine;

namespace Tempest.Enemies.Elite
{
    public class EliteAttackState : BaseState<EnemyStates, EnemyContext>
    {
        private enum Phase { Windup, Strike, Cooldown }

        private const float WindupDuration = 0.5f;
        private const float ConeHalfAngle = 30f;
        private const float TurnSpeed = 540f;

        private readonly Transform _transform;
        private readonly LayerMask _playerLayer;
        private readonly Renderer _renderer;
        private readonly MaterialPropertyBlock _propBlock;
        private readonly Color _originalColor;
        private readonly Color _telegraphColor = new(1f, 0.3f, 0f, 1f);

        private Phase _phase;
        private float _timer;
        private bool _attackComplete;

        public bool AttackComplete => _attackComplete;

        public EliteAttackState(StateMachine<EnemyStates, EnemyContext> stateMachine)
            : base(EnemyStates.Attack, stateMachine)
        {
            _transform = stateMachine.transform;
            _playerLayer = LayerConstants.Player;

            _renderer = stateMachine.GetComponentInChildren<Renderer>();
            _propBlock = new MaterialPropertyBlock();
            if (_renderer != null)
            {
                _renderer.GetPropertyBlock(_propBlock);
                _originalColor = _propBlock.GetColor("_BaseColor");
                if (_originalColor == default)
                    _originalColor = _renderer.sharedMaterial.GetColor("_BaseColor");
            }
        }

        public override void EnterState()
        {
            base.EnterState();
            _attackComplete = false;
            Context.Agent.Stop();
            BeginWindup();
        }

        public override void FrameUpdate()
        {
            base.FrameUpdate();
            if (IsExitingState) return;

            if (Context.Target == null || Context.Target.IsDown)
            {
                _attackComplete = true;
                ResetTelegraph();
                return;
            }

            FaceTarget();

            _timer -= Time.deltaTime;

            switch (_phase)
            {
                case Phase.Windup:
                    UpdateTelegraph();
                    if (_timer <= 0f)
                        ExecuteStrike();
                    break;

                case Phase.Cooldown:
                    if (_timer <= 0f)
                        _attackComplete = true;
                    break;
            }
        }

        public override void ExitState()
        {
            ResetTelegraph();
            base.ExitState();
        }

        private void BeginWindup()
        {
            _phase = Phase.Windup;
            _timer = WindupDuration;
        }

        private void UpdateTelegraph()
        {
            if (_renderer == null) return;

            float t = 1f - (_timer / WindupDuration);
            Color current = Color.Lerp(_originalColor, _telegraphColor, t);
            _propBlock.SetColor("_BaseColor", current);
            _renderer.SetPropertyBlock(_propBlock);
        }

        private void ExecuteStrike()
        {
            ResetTelegraph();

            float attackRange = Context.Definition.attackRange;
            Collider[] hits = Physics.OverlapSphere(
                _transform.position, attackRange, _playerLayer);

            foreach (Collider hit in hits)
            {
                Vector3 dirToTarget = (hit.transform.position - _transform.position).normalized;
                float angle = Vector3.Angle(_transform.forward, dirToTarget);

                if (angle <= ConeHalfAngle)
                {
                    IDamageable damageable = hit.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        Vector3 hitPoint = hit.ClosestPoint(_transform.position);
                        Vector3 hitNormal = (_transform.position - hit.transform.position).normalized;
                        damageable.TakeDamage(Context.Definition.damage, hitPoint, hitNormal);
                    }
                }
            }

            _phase = Phase.Cooldown;
            _timer = Context.Definition.attackCooldown;
        }

        private void ResetTelegraph()
        {
            if (_renderer == null) return;
            _propBlock.SetColor("_BaseColor", _originalColor);
            _renderer.SetPropertyBlock(_propBlock);
        }

        private void FaceTarget()
        {
            Vector3 direction = Context.Target.transform.position - _transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                _transform.rotation = Quaternion.RotateTowards(
                    _transform.rotation, targetRot, TurnSpeed * Time.deltaTime);
            }
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Tempest/Assets/Scripts/Enemies/AI/Elite/States/EliteAttackState.cs
git commit -m "feat(enemies): add EliteAttackState with telegraphed cone attack"
```

---

### Task 7: EliteEnemyStateMachine (Wires Everything Together)

**Files:**
- Create: `Tempest/Assets/Scripts/Enemies/AI/Elite/EliteEnemyStateMachine.cs`

- [ ] **Step 1: Create EliteEnemyStateMachine**

Create `Scripts/Enemies/AI/Elite/EliteEnemyStateMachine.cs`:

```csharp
using Tempest.AI;
using Tempest.Enemies.Enums;
using UnityEngine;

namespace Tempest.Enemies.Elite
{
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(NaturalAgent))]
    public class EliteEnemyStateMachine : StateMachine<EnemyStates, EnemyContext>
    {
        [SerializeField] private EnemyDefinition definition;
        [SerializeField] private PatrolPath patrolPath;

        private SphereSensor _detectionSensor;

        protected override void Awake()
        {
            var health = GetComponent<EnemyHealth>();
            var agent = GetComponent<NaturalAgent>();

            health.Initialize(definition);

            Context = new EnemyContext(health, agent, definition);

            _detectionSensor = new SphereSensor(
                LayerConstants.Player,
                definition.detectionRadius,
                transform,
                Vector3.zero);

            base.Awake();

            var spawn = new SpawnState(this);
            var active = new EliteActiveState(this);
            var patrol = new ElitePatrolState(this, patrolPath, _detectionSensor);
            var chase = new EliteChaseState(this);
            var attack = new EliteAttackState(this);
            var death = new DeathState(this);

            AddState(spawn);
            AddState(active);
            AddState(death);

            active.AddSubState(patrol);
            active.AddSubState(chase);
            active.AddSubState(attack);

            // Top-level: Spawn → Active
            spawn.FromThis()
                .To(EnemyStates.Active)
                .When(() => spawn.IsComplete)
                .Build();

            // Top-level: Any → Death
            spawn.FromThis()
                .To(EnemyStates.Death)
                .When(() => Context.Health.IsDead)
                .Build();

            active.FromThis()
                .To(EnemyStates.Death)
                .When(() => Context.Health.IsDead)
                .Build();

            // Sub-state: Patrol → Chase (player detected)
            patrol.FromThis()
                .To(EnemyStates.Chase)
                .When(() => patrol.PlayerDetected)
                .Build();

            // Sub-state: Chase → Attack (in range)
            chase.FromThis()
                .To(EnemyStates.Attack)
                .When(() => chase.InAttackRange)
                .Build();

            // Sub-state: Attack → Chase (attack complete)
            attack.FromThis()
                .To(EnemyStates.Chase)
                .When(() => attack.AttackComplete)
                .Build();

            // Sub-state: Chase → Patrol (target lost)
            chase.FromThis()
                .To(EnemyStates.Patrol)
                .When(() => chase.TargetLost)
                .Build();
        }

        private void Start()
        {
            if (patrolPath == null)
                Debug.LogWarning($"[{name}] No PatrolPath assigned — elite will idle in place.");

            SetInitialState(EnemyStates.Spawn);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (definition == null) return;

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f);
            Gizmos.DrawWireSphere(transform.position, definition.detectionRadius);

            if (Application.isPlaying)
            {
                Vector3 worldPos = transform.position + Vector3.up * 2.5f;
                var style = new GUIStyle(UnityEditor.EditorStyles.boldLabel)
                {
                    normal = { textColor = Color.magenta },
                    fontSize = 12
                };
                UnityEditor.Handles.Label(worldPos, DebugStatePath, style);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (definition == null) return;

            Gizmos.color = Color.red;
            Vector3 forward = transform.forward;
            float range = definition.attackRange;

            Quaternion leftRot = Quaternion.AngleAxis(-30f, Vector3.up);
            Quaternion rightRot = Quaternion.AngleAxis(30f, Vector3.up);

            Vector3 leftDir = leftRot * forward * range;
            Vector3 rightDir = rightRot * forward * range;

            Gizmos.DrawLine(transform.position, transform.position + leftDir);
            Gizmos.DrawLine(transform.position, transform.position + rightDir);
            Gizmos.DrawLine(transform.position + leftDir, transform.position + rightDir);
        }
#endif
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Tempest/Assets/Scripts/Enemies/AI/Elite/EliteEnemyStateMachine.cs
git commit -m "feat(enemies): add EliteEnemyStateMachine wiring hierarchical states and transitions"
```

---

### Task 8: Create NagaWarrior ScriptableObject Asset

**Files:**
- Create: `Tempest/Assets/ScriptableObjects/Enemies/NagaWarrior.asset`

- [ ] **Step 1: Create the asset file**

Create `ScriptableObjects/Enemies/NagaWarrior.asset`:

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: GUID_OF_ENEMY_DEFINITION, type: 3}
  m_Name: NagaWarrior
  m_EditorClassIdentifier:
  enemyName: Naga Warrior
  tier: 1
  maxHealth: 150
  moveSpeed: 4
  damage: 20
  attackRange: 2.5
  attackCooldown: 1.8
  detectionRadius: 18
  xpValue: 50
  deathEffectPrefab: {fileID: 0}
  hitEffectPrefab: {fileID: 0}
```

**Note:** The GUID for `m_Script` must reference `EnemyDefinition.cs`. To create this properly in Unity:

1. Open Unity Editor
2. Right-click `Assets/ScriptableObjects/Enemies/` → Create → Tempest → Enemies → Enemy Definition
3. Name it "NagaWarrior"
4. Set values in Inspector:
   - enemyName: "Naga Warrior"
   - tier: Elite
   - maxHealth: 150
   - moveSpeed: 4
   - damage: 20
   - attackRange: 2.5
   - attackCooldown: 1.8
   - detectionRadius: 18
   - xpValue: 50

- [ ] **Step 2: Commit** (after Unity generates the .asset and .meta files)

```bash
git add Tempest/Assets/ScriptableObjects/Enemies/NagaWarrior.asset Tempest/Assets/ScriptableObjects/Enemies/NagaWarrior.asset.meta
git commit -m "feat(enemies): add NagaWarrior ScriptableObject definition"
```

---

### Task 9: Scene Setup and Smoke Test

**Files:**
- No new files — scene setup in Unity Editor

- [ ] **Step 1: Create Elite enemy prefab**

In Unity Editor:
1. Create empty GameObject, name it "NagaWarrior_Elite"
2. Add a Capsule child (scale Y to 1.5 for larger silhouette — elite is bigger)
3. Add components to root:
   - `EliteEnemyStateMachine`
   - `EnemyHealth` (auto-added via RequireComponent)
   - `NaturalAgent` (auto-added via RequireComponent)
   - `NavMeshAgent`
4. Assign `NagaWarrior` ScriptableObject to the `definition` field
5. Save as prefab in `Assets/Prefabs/Enemies/NagaWarrior.prefab`

- [ ] **Step 2: Create PatrolPath in scene**

1. Create empty GameObject, name it "ElitePatrolPath"
2. Add `PatrolPath` component
3. Create 3-4 child GameObjects as waypoints, position them around the play area
4. Set Mode to Loop
5. Verify gizmo lines appear in Scene view

- [ ] **Step 3: Place and test**

1. Drag `NagaWarrior` prefab into scene
2. Assign the `ElitePatrolPath` to the elite's `patrolPath` field
3. Ensure NavMesh is baked in the scene
4. Enter Play mode

- [ ] **Step 4: Verify acceptance criteria**

| Criteria | How to verify |
|----------|--------------|
| Patrols waypoints | Watch the elite move between waypoints at half speed |
| Detects player | Walk within 18 units — elite should switch to Chase |
| Chases player | Elite sprints toward player at full speed |
| Telegraphed attack | Elite glows orange for 0.5s when in melee range |
| Deals significant damage | Player health drops by 20 on hit |
| Takes many shots to kill | Elite has 150 HP — requires sustained fire |
| Returns to patrol | Run 18+ units away, wait 5s — elite returns to patrol |
| Distinct from swarm | Slower, more deliberate, hits harder, patrols |

- [ ] **Step 5: Commit prefab and scene changes**

```bash
git add Tempest/Assets/Prefabs/Enemies/ Tempest/Assets/Scenes/
git commit -m "feat(enemies): add NagaWarrior prefab and scene setup"
```

---

## Dependency Order

```
Task 1 (enum + definition changes)
  ↓
Task 2 (PatrolPath system) — independent of Task 1 but ordered for clarity
  ↓
Task 3 (EliteActiveState)
  ↓
Task 4 (ElitePatrolState) — depends on PatrolPath + SphereSensor
  ↓
Task 5 (EliteChaseState)
  ↓
Task 6 (EliteAttackState)
  ↓
Task 7 (EliteEnemyStateMachine) — wires all states together
  ↓
Task 8 (NagaWarrior asset)
  ↓
Task 9 (Scene setup + smoke test)
```

Tasks 1 and 2 can be done in parallel. Tasks 3-6 can be done in parallel (they don't import each other). Task 7 depends on all of 3-6. Tasks 8-9 are sequential at the end.
