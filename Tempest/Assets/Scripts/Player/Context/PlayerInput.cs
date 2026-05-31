using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [SerializeField] private float pressBufferTime = 0.2f;

    public Vector3 MovementInput { get; private set; } = Vector3.zero;
    public bool SprintPressed { get; set; }
    public bool CrouchPressed { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool ShootHeld { get; private set; }
    public bool InteractHeld { get; private set; }

    // Buffered edge-triggered inputs
    private float _lastJumpTime = -999f;
    private bool _jumpConsumed;

    private float _lastShootTime = -999f;
    private bool _shootConsumed;

    private float _lastInteractTime = -999f;
    private bool _interactConsumed;

    private float _lastReloadTime = -999f;
    private bool _reloadConsumed;

    private float _lastWeapon1Time = -999f;
    private bool _weapon1Consumed;
    private float _lastWeapon2Time = -999f;
    private bool _weapon2Consumed;
    private float _switchWeaponValue;

    public bool JumpPressed
    {
        get
        {
            if (_jumpConsumed) return false;
            return Time.time - _lastJumpTime <= pressBufferTime;
        }
    }

    public bool ShootPressed
    {
        get
        {
            if (_shootConsumed) return false;
            return Time.time - _lastShootTime <= pressBufferTime;
        }
    }

    public bool InteractPressed
    {
        get
        {
            if (_interactConsumed) return false;
            return Time.time - _lastInteractTime <= pressBufferTime;
        }
    }

    public bool ReloadPressed
    {
        get
        {
            if (_reloadConsumed) return false;
            return Time.time - _lastReloadTime <= pressBufferTime;
        }
    }

    public bool Weapon1Pressed
    {
        get
        {
            if (_weapon1Consumed) return false;
            return Time.time - _lastWeapon1Time <= pressBufferTime;
        }
    }

    public bool Weapon2Pressed
    {
        get
        {
            if (_weapon2Consumed) return false;
            return Time.time - _lastWeapon2Time <= pressBufferTime;
        }
    }

    public float SwitchWeaponValue => _switchWeaponValue;

    public InputSystem_Actions InputActions { get; private set; }

    private void Awake()
    {
        InputActions = new InputSystem_Actions();
        ConfigureInputs();
    }

    private void OnEnable() => InputActions.Enable();
    private void OnDisable() => InputActions.Disable();

    private void ConfigureInputs()
    {
        InputActions.Player.Move.performed += ctx =>
        {
            var val = ctx.ReadValue<Vector2>();
            MovementInput = new Vector3(val.x, 0, val.y);
        };
        InputActions.Player.Move.canceled += _ => MovementInput = Vector3.zero;

        InputActions.Player.Sprint.performed += _ => SprintPressed = true;
        InputActions.Player.Sprint.canceled += _ => SprintPressed = false;

        InputActions.Player.Jump.performed += _ =>
        {
            _jumpConsumed = false;
            _lastJumpTime = Time.time;
            JumpHeld = true;
        };
        InputActions.Player.Jump.canceled += _ => JumpHeld = false;

        InputActions.Player.Crouch.performed += _ => CrouchPressed = true;

        InputActions.Player.Attack.performed += _ =>
        {
            _shootConsumed = false;
            _lastShootTime = Time.time;
            ShootHeld = true;
        };
        InputActions.Player.Attack.canceled += _ => ShootHeld = false;

        InputActions.Player.Interact.performed += _ =>
        {
            _interactConsumed = false;
            _lastInteractTime = Time.time;
            InteractHeld = true;
        };
        InputActions.Player.Interact.canceled += _ => InteractHeld = false;

        InputActions.Player.Reload.performed += _ =>
        {
            _reloadConsumed = false;
            _lastReloadTime = Time.time;
        };

        InputActions.Player.Previous.performed += _ =>
        {
            _weapon1Consumed = false;
            _lastWeapon1Time = Time.time;
        };

        InputActions.Player.Next.performed += _ =>
        {
            _weapon2Consumed = false;
            _lastWeapon2Time = Time.time;
        };

        InputActions.Player.SwitchWeapon.performed += ctx =>
            _switchWeaponValue = ctx.ReadValue<float>();
        InputActions.Player.SwitchWeapon.canceled += _ =>
            _switchWeaponValue = 0f;
    }

    public void ConsumeJump() => _jumpConsumed = true;
    public void ConsumeCrouch() => CrouchPressed = false;
    public void ConsumeShoot() => _shootConsumed = true;
    public void ConsumeInteract() => _interactConsumed = true;
    public void ConsumeReload() => _reloadConsumed = true;
    public void ConsumeWeapon1() => _weapon1Consumed = true;
    public void ConsumeWeapon2() => _weapon2Consumed = true;
    public void ConsumeSwitchWeapon() => _switchWeaponValue = 0f;
}
