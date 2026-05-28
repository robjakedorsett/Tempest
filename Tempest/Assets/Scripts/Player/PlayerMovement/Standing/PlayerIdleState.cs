using Tempest.Player.Enums;

public class PlayerIdleState : BaseState<PlayerMovementStates, PlayerContext>
{
    public PlayerIdleState(PlayerMovementStates stateKey, StateMachine<PlayerMovementStates, PlayerContext> stateMachine)
        : base(stateKey, stateMachine)
    {
    }

    public override void EnterState()
    {
        base.EnterState();
        Context.Motor.StopHorizontalVelocity();
    }
}
