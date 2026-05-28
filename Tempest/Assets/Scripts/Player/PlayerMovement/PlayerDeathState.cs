using Tempest.Player.Enums;

public class PlayerDeathState : BaseState<PlayerMovementStates, PlayerContext>
{
    public PlayerDeathState(PlayerMovementStates stateKey, StateMachine<PlayerMovementStates, PlayerContext> stateMachine)
        : base(stateKey, stateMachine)
    {
    }

    public override void EnterState()
    {
        base.EnterState();
        Context.Motor.StopHorizontalVelocity();
    }
}
