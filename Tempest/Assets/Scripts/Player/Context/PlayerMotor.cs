using Tempest.Player.Helpers;
using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerMotor : MonoBehaviour
{
    [Header("Movement Settings")]
    public float WalkSpeed = 5f;
    public float SprintSpeed = 8f;
    public float AirControlSpeed = 2.5f;
    public float JumpForce = 7f;
    public float GroundAcceleration = 10f;
    public float AirAcceleration = 5f;

    [Header("Slide Settings")]
    public float MinSlideSpeed = 5f;
    public float SlideFriction = 8f;
    public float MaxSlideDuration = 0.75f;

    [Header("Ground Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] public float CoyoteTime = 0.2f;

    public Rigidbody Rb { get; private set; }
    public CapsuleCollider Collider { get; private set; }

    private GroundSensor _groundSensor;
    private float _slideTimer;

    public bool IsSliding;
    public bool IsGrounded => _groundSensor.IsGrounded;
    public bool InCoyoteWindow => _groundSensor.InCoyoteWindow;
    public bool IsFalling => !_groundSensor.IsGrounded && Rb.linearVelocity.y < 0;
    public float RelativeHeight => _groundSensor.RelativeHeight;

    public float CurrentSpeed = 0f;
    public float HorizontalSpeed =>
        new Vector3(Rb.linearVelocity.x, 0f, Rb.linearVelocity.z).magnitude;
    public bool IsSlideFinished =>
        !IsSliding ||
        _slideTimer >= MaxSlideDuration ||
        HorizontalSpeed <= MinSlideSpeed * 0.5f;

    private void Awake()
    {
        Collider = GetComponent<CapsuleCollider>();
        Rb = GetComponent<Rigidbody>();
        Rb.freezeRotation = true;

        _groundSensor = new GroundSensor(
            groundLayer,
            groundCheckRadius,
            Collider,
            transform,
            CoyoteTime
        );
    }

    private void FixedUpdate()
    {
        _groundSensor.Monitor();

        if (!_groundSensor.IsGrounded)
        {
            Rb.AddForce(Vector3.down * 20f, ForceMode.Acceleration);
        }
    }

    public void MoveOnGround(Vector3 movementInput, float targetSpeed)
    {
        CurrentSpeed = targetSpeed;
        Vector3 desiredMoveDirection = MovementHelpers.GetMovementDirectionForTransform(
            transform,
            movementInput
        );
        MovementHelpers.MoveRigidbody(Rb, desiredMoveDirection, CurrentSpeed, GroundAcceleration);
    }

    public void MoveInAir(Vector3 movementInput)
    {
        Vector3 desiredMoveDirection = MovementHelpers.GetMovementDirectionForTransform(
            transform,
            movementInput
        );
        MovementHelpers.MoveRigidbody(Rb, desiredMoveDirection, AirControlSpeed, AirAcceleration);
    }

    public void ApplyJump()
    {
        Rb.linearVelocity = new Vector3(Rb.linearVelocity.x, 0f, Rb.linearVelocity.z);
        Rb.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
    }

    public void StopHorizontalVelocity()
    {
        Rb.linearVelocity = new Vector3(0, Rb.linearVelocity.y, 0);
    }

    public void StartSlide(Vector3 movementInput)
    {
        IsSliding = true;
        _slideTimer = 0f;

        Vector3 slideDir;
        if (movementInput.sqrMagnitude > 0.01f)
        {
            slideDir = MovementHelpers
                .GetMovementDirectionForTransform(transform, movementInput)
                .normalized;
        }
        else
        {
            var horizVel = new Vector3(Rb.linearVelocity.x, 0f, Rb.linearVelocity.z);
            slideDir = horizVel.sqrMagnitude > 0.01f ? horizVel.normalized : transform.forward;
        }

        float baseSpeed = new Vector3(Rb.linearVelocity.x, 0f, Rb.linearVelocity.z).magnitude;
        float startSpeed = Mathf.Max(baseSpeed, MinSlideSpeed);

        Vector3 newHorizVel = slideDir * startSpeed;
        Rb.linearVelocity = new Vector3(newHorizVel.x, Rb.linearVelocity.y, newHorizVel.z);
    }

    public void UpdateSlide()
    {
        if (!IsSliding) return;

        _slideTimer += Time.fixedDeltaTime;

        var v = Rb.linearVelocity;
        var horizontal = new Vector3(v.x, 0, v.z);
        horizontal = Vector3.MoveTowards(horizontal, Vector3.zero, SlideFriction * Time.fixedDeltaTime);
        Rb.linearVelocity = new Vector3(horizontal.x, v.y, horizontal.z);
    }

    public void EndSlide()
    {
        IsSliding = false;
    }

    private void OnDrawGizmos()
    {
        _groundSensor?.DrawGizmos();
    }
}
