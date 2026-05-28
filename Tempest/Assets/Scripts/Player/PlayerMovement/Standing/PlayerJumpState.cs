using Tempest.Player.Enums;

public class PlayerJumpState : BaseState<PlayerMovementStates, PlayerContext>
{
    private bool _jumpApplied;

    public PlayerJumpState(PlayerMovementStates stateKey, StateMachine<PlayerMovementStates, PlayerContext> stateMachine)
        : base(stateKey, stateMachine)
    {
    }

    public override bool AllowParentTransitions => false;
    public override bool CanEnterState => Context.Motor.IsGrounded || Context.Motor.InCoyoteWindow;

    public override void EnterState()
    {
        base.EnterState();
        _jumpApplied = false;
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        if (_jumpApplied) return;
        Context.Motor.ApplyJump();
        _jumpApplied = true;
    }
}
