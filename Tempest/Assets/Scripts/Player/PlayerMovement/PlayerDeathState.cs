using Tempest.Player.Enums;
using UnityEngine;

public class PlayerDeathState : BaseState<PlayerMovementStates, PlayerContext>
{
    private PlayerCameraController _cameraController;

    public PlayerDeathState(PlayerMovementStates stateKey, StateMachine<PlayerMovementStates, PlayerContext> stateMachine)
        : base(stateKey, stateMachine)
    {
        _cameraController = stateMachine.GetComponent<PlayerCameraController>();
    }

    public override void EnterState()
    {
        base.EnterState();
        Context.Motor.StopHorizontalVelocity();

        if (_cameraController != null)
        {
            _cameraController.enabled = false;
            _cameraController.PitchOffset = 30f;
        }
    }

    public override void ExitState()
    {
        base.ExitState();

        if (_cameraController != null)
        {
            _cameraController.PitchOffset = 0f;
            _cameraController.enabled = true;
        }
    }
}
