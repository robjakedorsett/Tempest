using Tempest.Enemies.Enums;
using UnityEngine;

namespace Tempest.Enemies
{
    public class ChaseState : BaseState<EnemyStates, EnemyContext>
    {
        private const float RetargetInterval = 1.5f;
        private float _retargetTimer;
        private Vector3 _approachOffset;

        public ChaseState(StateMachine<EnemyStates, EnemyContext> stateMachine)
            : base(EnemyStates.Chase, stateMachine) { }

        public override void EnterState()
        {
            base.EnterState();

            float speedVariance = Random.Range(0.85f, 1.15f);
            Context.Agent.Initialize(Context.Definition.moveSpeed * speedVariance);

            FindTarget();
            GenerateApproachOffset();
            _retargetTimer = RetargetInterval;
        }

        public override void FrameUpdate()
        {
            base.FrameUpdate();
            if (IsExitingState) return;

            _retargetTimer -= Time.deltaTime;
            if (_retargetTimer <= 0f)
            {
                FindTarget();
                GenerateApproachOffset();
                _retargetTimer = RetargetInterval;
            }

            if (Context.Target != null)
                Context.Agent.SetDestination(Context.Target.transform.position + _approachOffset);
            else
                Context.Agent.Stop();
        }

        public override void ExitState()
        {
            PlayerRegistry.ReleaseTarget(Context.Target);
            base.ExitState();
            Context.Agent.Stop();
        }

        private void FindTarget()
        {
            var previous = Context.Target;
            Context.Target = PlayerRegistry.GetBestTarget(
                StateMachine.transform.position);

            if (Context.Target != previous)
            {
                PlayerRegistry.ReleaseTarget(previous);
                PlayerRegistry.AssignTarget(Context.Target);
            }
        }

        private void GenerateApproachOffset()
        {
            float radius = Context.Definition.attackRange * 0.6f;
            Vector2 circle = Random.insideUnitCircle * radius;
            _approachOffset = new Vector3(circle.x, 0f, circle.y);
        }
    }
}
