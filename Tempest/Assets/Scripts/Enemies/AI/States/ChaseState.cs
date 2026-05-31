using Tempest.Enemies.Enums;
using UnityEngine;

namespace Tempest.Enemies
{
    public class ChaseState : BaseState<EnemyStates, EnemyContext>
    {
        private const float RetargetInterval = 1.5f;
        private float _retargetTimer;

        public ChaseState(StateMachine<EnemyStates, EnemyContext> stateMachine)
            : base(EnemyStates.Chase, stateMachine) { }

        public override void EnterState()
        {
            base.EnterState();

            float speedVariance = Random.Range(0.85f, 1.15f);
            Context.Agent.Initialize(Context.Definition.moveSpeed * speedVariance);

            FindTarget();
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
                _retargetTimer = RetargetInterval;
            }

            if (Context.Target != null)
                Context.Agent.SetDestination(Context.Target.transform.position);
            else
                Context.Agent.Stop();
        }

        public override void ExitState()
        {
            base.ExitState();
            Context.Agent.Stop();
        }

        private void FindTarget()
        {
            Context.Target = PlayerRegistry.GetNearestPlayer(
                StateMachine.transform.position);
        }
    }
}
