using Tempest.Enemies.Enums;
using UnityEngine;

namespace Tempest.Enemies.Elite
{
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(NaturalAgent))]
    public class EliteEnemyStateMachine : StateMachine<EnemyStates, EnemyContext>
    {
        [SerializeField] private EnemyDefinition definition;

        protected override void Awake()
        {
            var health = GetComponent<EnemyHealth>();
            var agent = GetComponent<NaturalAgent>();

            health.Initialize(definition);

            Context = new EnemyContext(health, agent, definition);

            base.Awake();

            var spawn = new SpawnState(this);
            var active = new EliteActiveState(this);
            var patrol = new ElitePatrolState(this);
            var chase = new EliteChaseState(this);
            var attack = new EliteAttackState(this);
            var death = new DeathState(this);

            AddState(spawn);
            AddState(active);
            AddState(death);

            active.AddSubState(patrol);
            active.AddSubState(chase);
            active.AddSubState(attack);

            spawn.FromThis()
                .To(EnemyStates.Active)
                .When(() => spawn.IsComplete)
                .Build();

            spawn.FromThis()
                .To(EnemyStates.Death)
                .When(() => Context.Health.IsDead)
                .Build();

            active.FromThis()
                .To(EnemyStates.Death)
                .When(() => Context.Health.IsDead)
                .Build();

            patrol.FromThis()
                .To(EnemyStates.Chase)
                .When(() => patrol.PlayerDetected)
                .Build();

            chase.FromThis()
                .To(EnemyStates.Attack)
                .When(() => chase.InAttackRange)
                .Build();

            attack.FromThis()
                .To(EnemyStates.Chase)
                .When(() => attack.AttackComplete)
                .Build();

            chase.FromThis()
                .To(EnemyStates.Patrol)
                .When(() => chase.TargetLost)
                .Build();
        }

        private void Start()
        {
            SetInitialState(EnemyStates.Spawn);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (definition == null) return;

            // Detection radius (orange)
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f);
            Gizmos.DrawWireSphere(transform.position, definition.detectionRadius);

            // Roam radius (cyan)
            Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, definition.roamRadius);

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
