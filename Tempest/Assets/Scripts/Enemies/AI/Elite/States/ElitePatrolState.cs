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
