using Tempest.Enemies.Enums;
using UnityEngine;

namespace Tempest.Enemies
{
    public class SpawnState : BaseState<EnemyStates, EnemyContext>
    {
        private const float SpawnDuration = 0.5f;
        private float _timer;

        public bool IsComplete => _timer <= 0f;

        public SpawnState(StateMachine<EnemyStates, EnemyContext> stateMachine)
            : base(EnemyStates.Spawn, stateMachine) { }

        public override void EnterState()
        {
            base.EnterState();
            _timer = SpawnDuration;
            Context.Agent.Stop();
        }

        public override void FrameUpdate()
        {
            base.FrameUpdate();
            if (IsExitingState) return;

            _timer -= Time.deltaTime;
        }
    }
}
