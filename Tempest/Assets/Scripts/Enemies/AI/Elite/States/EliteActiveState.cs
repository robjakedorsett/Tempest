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
