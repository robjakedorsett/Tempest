using UnityEngine;

public class WeaponBob : MonoBehaviour
{
    [Header("Walk Bob")]
    [SerializeField] private float bobFrequency = 10f;
    [SerializeField] private float bobHorizontalAmplitude = 0.05f;
    [SerializeField] private float bobVerticalAmplitude = 0.03f;
    [SerializeField] private float sprintBobMultiplier = 1.4f;

    [Header("Idle Sway")]
    [SerializeField] private float swayFrequency = 1.5f;
    [SerializeField] private float swayHorizontalAmplitude = 0.003f;
    [SerializeField] private float swayVerticalAmplitude = 0.002f;

    [Header("Jump & Land")]
    [SerializeField] private float jumpKickAmount = 0.05f;
    [SerializeField] private float landDropAmount = 0.08f;
    [SerializeField] private float landMaxVelocity = 15f;
    [SerializeField] private float impactRecoverySpeed = 8f;

    [Header("Settings")]
    [SerializeField] private float resetSmoothing = 5f;
    [SerializeField] private float speedThreshold = 0.1f;

    [Header("References")]
    [SerializeField] private PlayerMotor motor;

    private float _bobTimer;
    private float _swayTimer;
    private Vector3 _restPosition;
    private Vector3 _impactOffset;
    private bool _wasGrounded;
    private float _lastFallSpeed;

    private void Start()
    {
        _restPosition = transform.localPosition;
        _wasGrounded = true;
    }

    private void LateUpdate()
    {
        if (motor == null) return;

        bool grounded = motor.IsGrounded;
        float speed = motor.HorizontalSpeed;

        HandleImpacts(grounded);

        Vector3 bobOffset;

        if (speed > speedThreshold && grounded)
        {
            float sprintFactor = speed > motor.WalkSpeed ? sprintBobMultiplier : 1f;
            _bobTimer += Time.deltaTime * bobFrequency * sprintFactor;
            float xOffset = Mathf.Sin(_bobTimer) * bobHorizontalAmplitude * sprintFactor;
            float yOffset = Mathf.Sin(_bobTimer * 2f) * bobVerticalAmplitude * sprintFactor;
            bobOffset = new Vector3(xOffset, yOffset, 0f);
        }
        else if (grounded)
        {
            _bobTimer = 0f;
            _swayTimer += Time.deltaTime * swayFrequency;
            float xSway = Mathf.Sin(_swayTimer) * swayHorizontalAmplitude;
            float ySway = Mathf.Sin(_swayTimer * 0.7f) * swayVerticalAmplitude;
            bobOffset = new Vector3(xSway, ySway, 0f);
        }
        else
        {
            _bobTimer = 0f;
            bobOffset = Vector3.zero;
        }

        Vector3 target = _restPosition + bobOffset + _impactOffset;
        transform.localPosition = Vector3.Lerp(transform.localPosition, target, Time.deltaTime * resetSmoothing);

        _wasGrounded = grounded;
    }

    private void HandleImpacts(bool grounded)
    {
        if (!grounded)
        {
            float fallSpeed = -motor.Rb.linearVelocity.y;
            if (fallSpeed > 0f)
                _lastFallSpeed = fallSpeed;
        }

        if (!_wasGrounded && grounded)
        {
            float intensity = Mathf.Clamp01(_lastFallSpeed / landMaxVelocity);
            _impactOffset.y = -landDropAmount * intensity;
            _lastFallSpeed = 0f;
        }
        else if (_wasGrounded && !grounded && motor.Rb.linearVelocity.y > 0f)
        {
            _impactOffset.y = -jumpKickAmount;
        }

        _impactOffset = Vector3.Lerp(_impactOffset, Vector3.zero, Time.deltaTime * impactRecoverySpeed);
    }
}
