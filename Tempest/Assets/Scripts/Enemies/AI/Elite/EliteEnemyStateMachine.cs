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
