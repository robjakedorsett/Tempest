using Tempest.Enemies.Enums;

namespace Tempest.Enemies
{
    public class DeathState : BaseState<EnemyStates, EnemyContext>
    {
        public DeathState(StateMachine<EnemyStates, EnemyContext> stateMachine)
            : base(EnemyStates.Death, stateMachine) { }

        public override void EnterState()
        {
            base.EnterState();
            Context.Health.Die();
        }
    }
}
