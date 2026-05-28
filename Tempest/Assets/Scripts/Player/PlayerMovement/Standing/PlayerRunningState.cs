using Tempest.Player.Enums;

public class PlayerRunningState : BaseState<PlayerMovementStates, PlayerContext>
{
    public PlayerRunningState(PlayerMovementStates stateKey, StateMachine<PlayerMovementStates, PlayerContext> stateMachine)
        : base(stateKey, stateMachine)
    {
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        Context.Motor.MoveOnGround(Context.Input.MovementInput, Context.Motor.SprintSpeed);
    }
}
