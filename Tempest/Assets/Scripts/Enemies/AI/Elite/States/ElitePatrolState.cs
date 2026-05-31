using Tempest.Enemies.Enums;
using UnityEngine;
using UnityEngine.AI;

namespace Tempest.Enemies.Elite
{
    public class ElitePatrolState : BaseState<EnemyStates, EnemyContext>
    {
        private readonly Transform _transform;
        private readonly Vector3 _spawnOrigin;

        private bool _playerDetected;

        public bool PlayerDetected => _playerDetected;

        public ElitePatrolState(StateMachine<EnemyStates, EnemyContext> stateMachine)
            : base(EnemyStates.Patrol, stateMachine)
        {
            _transform = stateMachine.transform;
            _spawnOrigin = _transform.position;
        }

        public override void EnterState()
        {
            base.EnterState();
            _playerDetected = false;

            Context.Health.OnDamageTaken += HandleDamageTaken;

            float patrolSpeed = Context.Definition.moveSpeed * 0.5f;
            Context.Agent.Initialize(patrolSpeed);

            PickRoamDestination();
        }

        public override void ExitState()
        {
            Context.Health.OnDamageTaken -= HandleDamageTaken;
            base.ExitState();
        }

        public override void FrameUpdate()
        {
            base.FrameUpdate();
            if (IsExitingState) return;

            if (!_playerDetected)
                CheckForPlayers();

            if (Context.Agent.IsAtDestination())
                PickRoamDestination();
        }

        private void CheckForPlayers()
        {
            PlayerHealth nearest = PlayerRegistry.GetNearestPlayer(_transform.position);
            if (nearest == null || nearest.IsDown) return;

            float dist = Vector3.Distance(_transform.position, nearest.transform.position);
            if (dist <= Context.Definition.detectionRadius)
            {
                Context.Target = nearest;
                PlayerRegistry.AssignTarget(nearest);
                _playerDetected = true;
            }
        }

        private void HandleDamageTaken()
        {
            if (_playerDetected) return;

            PlayerHealth nearest = PlayerRegistry.GetNearestPlayer(_transform.position);
            if (nearest == null || nearest.IsDown) return;

            Context.Target = nearest;
            PlayerRegistry.AssignTarget(nearest);
            _playerDetected = true;
        }

        private void PickRoamDestination()
        {
            float radius = Context.Definition.roamRadius;

            for (int i = 0; i < 10; i++)
            {
                Vector3 randomDir = Random.insideUnitSphere * radius;
                randomDir.y = 0f;
                Vector3 candidate = _spawnOrigin + randomDir;

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    Context.Agent.SetDestination(hit.position);
                    return;
                }
            }
        }
    }
}
