using Tempest.Player.Enums;

public class PlayerWalkingState : BaseState<PlayerMovementStates, PlayerContext>
{
    public PlayerWalkingState(PlayerMovementStates stateKey, StateMachine<PlayerMovementStates, PlayerContext> stateMachine)
        : base(stateKey, stateMachine)
    {
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        Context.Motor.MoveOnGround(Context.Input.MovementInput, Context.Motor.WalkSpeed);
    }
}
