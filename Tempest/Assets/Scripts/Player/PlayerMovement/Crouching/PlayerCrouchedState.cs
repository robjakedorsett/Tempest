using Tempest.Core.Extensions;
using Tempest.Player.Enums;

public class PlayerCrouchedState : BaseState<PlayerMovementStates, PlayerContext>
{
    public PlayerCrouchedState(PlayerMovementStates stateKey, StateMachine<PlayerMovementStates, PlayerContext> stateMachine)
        : base(stateKey, stateMachine)
    {
        AddSubState(new PlayerCrouchIdleState(PlayerMovementStates.CrouchIdle, stateMachine));
        AddSubState(new PlayerCrouchWalkState(PlayerMovementStates.CrouchWalk, stateMachine));

        FromThis()
            .To(PlayerMovementStates.CrouchIdle)
            .When(() => !Context.Input.MovementInput.IsMoving())
            .Build();

        FromThis()
            .To(PlayerMovementStates.CrouchWalk)
            .When(() => Context.Input.MovementInput.IsMoving())
            .Build();
    }

    public override void EnterState()
    {
        base.EnterState();
        Context.Stance.SetCrouched();
    }

    public override void ExitState()
    {
        base.ExitState();
        Context.Stance.SetStanding();
    }
}
