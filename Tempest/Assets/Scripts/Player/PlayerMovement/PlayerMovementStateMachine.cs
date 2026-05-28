using Tempest.Player.Enums;
using UnityEngine;

[RequireComponent(typeof(PlayerMotor))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerStance))]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerMovementStateMachine : StateMachine<PlayerMovementStates, PlayerContext>
{
    protected override void Awake()
    {
        var motor = GetComponent<PlayerMotor>();
        var input = GetComponent<PlayerInput>();
        var stance = GetComponent<PlayerStance>();
        var health = GetComponent<PlayerHealth>();
        Context = new PlayerContext(motor, input, stance, health);
        base.Awake();

        var grounded = new PlayerGroundedState(PlayerMovementStates.Grounded, this);
        var airborne = new PlayerAirborneState(PlayerMovementStates.Airborne, this);
        var death = new PlayerDeathState(PlayerMovementStates.Death, this);

        AddState(grounded);
        AddState(airborne);
        AddState(death);

        grounded.FromThis()
            .To(PlayerMovementStates.Airborne)
            .When(() => !Context.Motor.IsGrounded && !Context.Motor.InCoyoteWindow)
            .Build();

        airborne.FromThis()
            .To(PlayerMovementStates.Grounded)
            .When(() => Context.Motor.IsGrounded || Context.Motor.InCoyoteWindow)
            .Build();

        grounded.FromThis()
            .To(PlayerMovementStates.Death)
            .When(() => Context.Health.IsDown)
            .Build();

        airborne.FromThis()
            .To(PlayerMovementStates.Death)
            .When(() => Context.Health.IsDown)
            .Build();

        death.FromThis()
            .To(PlayerMovementStates.Grounded)
            .When(() => !Context.Health.IsDown)
            .Build();
    }

    private void Start()
    {
        SetInitialState(PlayerMovementStates.Grounded);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Vector3 worldPos = transform.position + Vector3.up * 1.0f;
        var style = new GUIStyle(UnityEditor.EditorStyles.boldLabel)
        {
            normal = { textColor = Color.yellow },
            fontSize = 12
        };
        UnityEditor.Handles.Label(worldPos, DebugStatePath, style);
    }
#endif
}
