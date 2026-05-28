using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
public class PlayerStance : MonoBehaviour
{
    public bool IsCrouched { get; private set; }

    [Header("Stance Settings")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchHeight = 1.2f;

    [SerializeField] private Transform cameraRoot;
    [SerializeField] private Vector3 standingCameraOffset = new(0f, 0.8f, 0f);
    [SerializeField] private Vector3 crouchCameraOffset = new(0f, 0.3f, 0f);

    public bool CanCrouch { get; set; } = true;
    public bool CanStand { get; set; } = true;

    private CapsuleCollider _collider;

    private void Awake()
    {
        _collider = GetComponent<CapsuleCollider>();
    }

    public void SetStanding()
    {
        if (!CanStand) return;

        IsCrouched = false;
        _collider.height = standingHeight;
        _collider.center = new Vector3(0f, standingHeight / 2f, 0f);
        if (cameraRoot != null)
            cameraRoot.localPosition = standingCameraOffset;
    }

    public void SetCrouched()
    {
        if (!CanCrouch) return;

        IsCrouched = true;
        _collider.height = crouchHeight;
        _collider.center = new Vector3(0f, crouchHeight / 2f, 0f);
        if (cameraRoot != null)
            cameraRoot.localPosition = crouchCameraOffset;
    }
}
