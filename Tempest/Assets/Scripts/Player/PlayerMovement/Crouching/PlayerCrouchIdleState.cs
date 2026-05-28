using Tempest.Player.Enums;

public class PlayerCrouchIdleState : BaseState<PlayerMovementStates, PlayerContext>
{
    public PlayerCrouchIdleState(PlayerMovementStates stateKey, StateMachine<PlayerMovementStates, PlayerContext> stateMachine)
        : base(stateKey, stateMachine)
    {
    }

    public override void EnterState()
    {
        base.EnterState();
        Context.Motor.StopHorizontalVelocity();
    }
}
