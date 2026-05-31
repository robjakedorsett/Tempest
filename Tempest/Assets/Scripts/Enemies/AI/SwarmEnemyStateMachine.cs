using Tempest.Enemies.Enums;
using UnityEngine;

namespace Tempest.Enemies
{
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(NaturalAgent))]
    public class SwarmEnemyStateMachine : StateMachine<EnemyStates, EnemyContext>
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
            var chase = new ChaseState(this);
            var attack = new AttackState(this);
            var death = new DeathState(this);

            AddState(spawn);
            AddState(chase);
            AddState(attack);
            AddState(death);

            // Spawn → Chase when spawn timer expires
            spawn.FromThis()
                .To(EnemyStates.Chase)
                .When(() => spawn.IsComplete)
                .Build();

            // Chase → Attack when target is in attack range
            chase.FromThis()
                .To(EnemyStates.Attack)
                .When(() => Context.Target != null &&
                    Vector3.Distance(
                        transform.position,
                        Context.Target.transform.position) <= definition.attackRange)
                .Build();

            // Attack → Chase when target moves out of range (with hysteresis)
            attack.FromThis()
                .To(EnemyStates.Chase)
                .When(() => Context.Target == null ||
                    Context.Target.IsDown ||
                    Vector3.Distance(
                        transform.position,
                        Context.Target.transform.position) > definition.attackRange * 1.2f)
                .Build();

            // Any state → Death when health depleted
            spawn.FromThis()
                .To(EnemyStates.Death)
                .When(() => Context.Health.IsDead)
                .Build();

            chase.FromThis()
                .To(EnemyStates.Death)
                .When(() => Context.Health.IsDead)
                .Build();

            attack.FromThis()
                .To(EnemyStates.Death)
                .When(() => Context.Health.IsDead)
                .Build();
        }

        private void Start()
        {
            SetInitialState(EnemyStates.Spawn);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            Vector3 worldPos = transform.position + Vector3.up * 2.5f;
            var style = new GUIStyle(UnityEditor.EditorStyles.boldLabel)
            {
                normal = { textColor = Color.red },
                fontSize = 12
            };
            UnityEditor.Handles.Label(worldPos, DebugStatePath, style);
        }
#endif
    }
}
