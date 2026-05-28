using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCameraController : MonoBehaviour
{
    [Header("Sensitivity")]
    [SerializeField] private float mouseSensitivity = 2f;

    [Header("Pitch Limits")]
    [SerializeField] private float maxPitch = 89f;
    [SerializeField] private float minPitch = -89f;

    [Header("References")]
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private InputActionReference lookAction;

    private float _pitch;
    private float _yaw;

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

        transform.localRotation = Quaternion.Euler(0f, _yaw, 0f);

        if (cameraHolder != null)
            cameraHolder.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }
}
