using Tempest.Player.Enums;

public class PlayerSlideState : BaseState<PlayerMovementStates, PlayerContext>
{
    public PlayerSlideState(PlayerMovementStates stateKey, StateMachine<PlayerMovementStates, PlayerContext> stateMachine)
        : base(stateKey, stateMachine)
    {
    }

    public override void EnterState()
    {
        base.EnterState();
        Context.Stance.SetCrouched();
        Context.Motor.StartSlide(Context.Input.MovementInput);
    }

    public override void ExitState()
    {
        base.ExitState();
        Context.Motor.EndSlide();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        Context.Motor.UpdateSlide();
    }
}
