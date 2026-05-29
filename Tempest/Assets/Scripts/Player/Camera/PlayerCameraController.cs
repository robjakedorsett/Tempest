using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCameraController : MonoBehaviour
{
    [Header("Sensitivity")]
    [SerializeField] private float mouseSensitivity = 2f;

    [Header("Pitch Limits")]
    [SerializeField] private float maxPitch = 89f;
    [SerializeField] private float minPitch = -89f;

    [Header("Recoil Recovery")]
    [SerializeField] private float recoilRecoverySpeed = 6f;

    [Header("References")]
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private InputActionReference lookAction;

    private float _pitch;
    private float _yaw;
    private float _recoilPitch;
    private float _recoilYaw;

    public float PitchOffset { get; set; }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _yaw = transform.eulerAngles.y;
    }

    private void OnEnable()
    {
        if (lookAction != null)
            lookAction.action.Enable();
    }

    private void OnDisable()
    {
        if (lookAction != null)
            lookAction.action.Disable();
    }

    private void LateUpdate()
    {
        if (lookAction == null) return;

        Vector2 mouseInput = lookAction.action.ReadValue<Vector2>();

        _yaw += mouseInput.x * mouseSensitivity;
        _pitch -= mouseInput.y * mouseSensitivity;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        _recoilPitch = Mathf.Lerp(_recoilPitch, 0f, Time.deltaTime * recoilRecoverySpeed);
        _recoilYaw = Mathf.Lerp(_recoilYaw, 0f, Time.deltaTime * recoilRecoverySpeed);

        transform.localRotation = Quaternion.Euler(0f, _yaw + _recoilYaw, 0f);

        if (cameraHolder != null)
            cameraHolder.localRotation = Quaternion.Euler(_pitch + _recoilPitch + PitchOffset, 0f, 0f);
    }

    public void ApplyAimKick(float pitchKick, float yawKick)
    {
        _recoilPitch += pitchKick;
        _recoilYaw += yawKick;
    }
}
