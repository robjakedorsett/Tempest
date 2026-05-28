using Tempest.Player.Enums;

public class PlayerAirborneState : BaseState<PlayerMovementStates, PlayerContext>
{
    public PlayerAirborneState(PlayerMovementStates stateKey, StateMachine<PlayerMovementStates, PlayerContext> stateMachine)
        : base(stateKey, stateMachine)
    {
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        Context.Motor.MoveInAir(Context.Input.MovementInput);
    }
}
