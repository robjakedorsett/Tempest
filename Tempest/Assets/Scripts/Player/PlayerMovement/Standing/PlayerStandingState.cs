using Tempest.Core.Extensions;
using Tempest.Player.Enums;

public class PlayerStandingState : BaseState<PlayerMovementStates, PlayerContext>
{
    public PlayerStandingState(PlayerMovementStates stateKey, StateMachine<PlayerMovementStates, PlayerContext> stateMachine)
        : base(stateKey, stateMachine)
    {
        AddSubState(new PlayerIdleState(PlayerMovementStates.Idle, stateMachine));
        AddSubState(new PlayerWalkingState(PlayerMovementStates.Walk, stateMachine));
        AddSubState(new PlayerRunningState(PlayerMovementStates.Run, stateMachine));
        AddSubState(new PlayerJumpState(PlayerMovementStates.Jump, stateMachine));

        FromThis()
            .To(PlayerMovementStates.Jump)
            .When(() => Context.Input.JumpPressed && (Context.Motor.IsGrounded || Context.Motor.InCoyoteWindow))
            .OnTransition(() => Context.Input.ConsumeJump())
            .Build();

        FromThis()
            .To(PlayerMovementStates.Idle)
            .When(() => !Context.Input.MovementInput.IsMoving())
            .Build();

        FromThis()
            .To(PlayerMovementStates.Walk)
            .When(() => Context.Input.MovementInput.IsMoving() && !Context.Input.SprintPressed)
            .Build();

        FromThis()
            .To(PlayerMovementStates.Run)
            .When(() => Context.Input.MovementInput.IsMoving() && Context.Input.SprintPressed)
            .Build();
    }

    public override void EnterState()
    {
        base.EnterState();
        Context.Stance.SetStanding();
    }
}
