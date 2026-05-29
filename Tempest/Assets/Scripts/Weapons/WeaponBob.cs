using UnityEngine;

public class WeaponBob : MonoBehaviour
{
    [Header("Bob Settings")]
    [SerializeField] private float bobFrequency = 10f;
    [SerializeField] private float bobHorizontalAmplitude = 0.05f;
    [SerializeField] private float bobVerticalAmplitude = 0.03f;

    [Header("Reset")]
    [SerializeField] private float resetSmoothing = 5f;
    [SerializeField] private float speedThreshold = 0.1f;

    [Header("References")]
    [SerializeField] private PlayerMotor motor;

    private float _bobTimer;
    private Vector3 _restPosition;

    private void Start()
    {
        _restPosition = transform.localPosition;
    }

    private void LateUpdate()
    {
        if (motor == null) return;

        float speed = motor.HorizontalSpeed;

        if (speed > speedThreshold)
        {
            _bobTimer += Time.deltaTime * bobFrequency;

            float xOffset = Mathf.Sin(_bobTimer) * bobHorizontalAmplitude;
            float yOffset = Mathf.Sin(_bobTimer * 2f) * bobVerticalAmplitude;

            transform.localPosition = _restPosition + new Vector3(xOffset, yOffset, 0f);
        }
        else
        {
            _bobTimer = 0f;
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                _restPosition,
                Time.deltaTime * resetSmoothing
            );
        }
    }
}
