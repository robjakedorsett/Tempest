using Tempest.Player.Enums;

public class PlayerCrouchWalkState : BaseState<PlayerMovementStates, PlayerContext>
{
    public PlayerCrouchWalkState(PlayerMovementStates stateKey, StateMachine<PlayerMovementStates, PlayerContext> stateMachine)
        : base(stateKey, stateMachine)
    {
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        Context.Motor.MoveOnGround(Context.Input.MovementInput, Context.Motor.WalkSpeed * 0.4f);
    }
}
