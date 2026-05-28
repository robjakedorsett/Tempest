using Tempest.Core.Extensions;
using Tempest.Player.Enums;

public class PlayerGroundedState : BaseState<PlayerMovementStates, PlayerContext>
{
    public PlayerGroundedState(PlayerMovementStates stateKey, StateMachine<PlayerMovementStates, PlayerContext> stateMachine)
        : base(stateKey, stateMachine)
    {
        AddSubState(new PlayerStandingState(PlayerMovementStates.Standing, stateMachine));
        AddSubState(new PlayerCrouchedState(PlayerMovementStates.Crouched, stateMachine));
        AddSubState(new PlayerSlideState(PlayerMovementStates.Slide, stateMachine));

        // Sprint + Crouch while moving = slide
        FromThis()
            .To(PlayerMovementStates.Slide)
            .When(() => Context.Input.SprintPressed
                       && Context.Input.MovementInput.IsMoving()
                       && !Context.Stance.IsCrouched
                       && Context.Input.CrouchPressed
                       || Context.Motor.IsSliding)
            .OnTransition(() => Context.Input.ConsumeCrouch())
            .Build();

        // Slide cancel: jump out of slide, keep momentum
        FromThis()
            .To(PlayerMovementStates.Standing)
            .When(() =>
                SubState != null &&
                SubState.StateKey.Equals(PlayerMovementStates.Slide) &&
                Context.Input.JumpPressed)
            .OnTransition(() =>
            {
                Context.Input.ConsumeJump();
                Context.Motor.ApplyJump();
            })
            .Build();

        // Slide finished → crouch
        FromThis()
            .To(PlayerMovementStates.Crouched)
            .When(() =>
                SubState != null &&
                SubState.StateKey.Equals(PlayerMovementStates.Slide) &&
                Context.Motor.IsSlideFinished)
            .Build();

        // Crouch cancel: jump out of crouch
        FromThis()
            .To(PlayerMovementStates.Standing)
            .When(() =>
                SubState != null &&
                SubState.StateKey.Equals(PlayerMovementStates.Crouched) &&
                Context.Input.JumpPressed)
            .OnTransition(() =>
            {
                Context.Input.ConsumeJump();
                Context.Motor.ApplyJump();
            })
            .Build();

        // Toggle crouch on
        FromThis()
            .To(PlayerMovementStates.Crouched)
            .When(() => Context.Input.CrouchPressed
                       && !Context.Stance.IsCrouched
                       && Context.Stance.CanCrouch)
            .OnTransition(() => Context.Input.ConsumeCrouch())
            .Build();

        // Toggle crouch off
        FromThis()
            .To(PlayerMovementStates.Standing)
            .When(() => Context.Input.CrouchPressed
                       && Context.Stance.IsCrouched
                       && Context.Stance.CanStand)
            .OnTransition(() => Context.Input.ConsumeCrouch())
            .Build();
    }

    public override void EnterState()
    {
        base.EnterState();
        SetSubState(PlayerMovementStates.Standing);
    }
}
