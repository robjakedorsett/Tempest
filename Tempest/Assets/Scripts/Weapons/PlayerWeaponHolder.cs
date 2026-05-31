using System;
using Tempest.Weapons;
using UnityEngine;

[RequireComponent(typeof(PlayerWeaponController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerWeaponHolder : MonoBehaviour
{
    private const int SlotCount = 2;

    [Header("Fallback (used when no loadout is set)")]
    [SerializeField] private WeaponDefinition startingPrimary;
    [SerializeField] private WeaponDefinition startingSecondary;

    private PlayerWeaponController _weaponController;
    private WeaponVisualController _visualController;
    private PlayerInput _input;
    private PlayerMovementStateMachine _stateMachine;

    private WeaponRuntimeState[] _slots;
    private int _activeSlotIndex;
    private bool _isEquipping;
    private float _equipEndTime;

    public int ActiveSlotIndex => _activeSlotIndex;
    public bool IsEquipping => _isEquipping;

    public event Action<int> OnWeaponSwitched;

    private void Awake()
    {
        _weaponController = GetComponent<PlayerWeaponController>();
        _visualController = GetComponent<WeaponVisualController>();
        _input = GetComponent<PlayerInput>();
        _stateMachine = GetComponent<PlayerMovementStateMachine>();
        _slots = new WeaponRuntimeState[SlotCount];
    }

    private void Start()
    {
        var loadout = _stateMachine.Context?.Loadout;
        var primary = loadout?.PrimaryWeapon ?? startingPrimary;
        var secondary = loadout?.SecondaryWeapon ?? startingSecondary;

        if (primary != null)
        {
            _slots[0] = new WeaponRuntimeState
            {
                definition = primary,
                currentAmmo = primary.magazineSize,
                currentSpread = primary.spread
            };
        }

        if (secondary != null)
        {
            _slots[1] = new WeaponRuntimeState
            {
                definition = secondary,
                currentAmmo = secondary.magazineSize,
                currentSpread = secondary.spread
            };
        }

        _activeSlotIndex = 0;
        if (_slots[0].definition != null)
            _weaponController.EquipFromState(_slots[0]);
    }

    private void Update()
    {
        if (_isEquipping)
        {
            if (Time.time >= _equipEndTime)
                CompleteEquip();
            return;
        }

        int requestedSlot = GetRequestedSlot();
        if (requestedSlot >= 0 && requestedSlot != _activeSlotIndex)
            BeginSwitch(requestedSlot);
    }

    private int GetRequestedSlot()
    {
        if (_input.Weapon1Pressed)
        {
            _input.ConsumeWeapon1();
            return 0;
        }

        if (_input.Weapon2Pressed)
        {
            _input.ConsumeWeapon2();
            return 1;
        }

        float scroll = _input.SwitchWeaponValue;
        if (Mathf.Abs(scroll) > 0.1f)
        {
            _input.ConsumeSwitchWeapon();
            int next = _activeSlotIndex + (scroll > 0f ? 1 : -1);
            next = ((next % SlotCount) + SlotCount) % SlotCount;
            return next;
        }

        return -1;
    }

    private void BeginSwitch(int targetSlot)
    {
        if (_slots[targetSlot].definition == null) return;

        _slots[_activeSlotIndex] = _weaponController.SaveState();
        _visualController.DespawnWeapon();

        _activeSlotIndex = targetSlot;
        _isEquipping = true;
        _weaponController.IsEquipping = true;
        _equipEndTime = Time.time + _slots[targetSlot].definition.equipTime;
    }

    private void CompleteEquip()
    {
        _isEquipping = false;
        _weaponController.IsEquipping = false;
        _weaponController.EquipFromState(_slots[_activeSlotIndex]);
        OnWeaponSwitched?.Invoke(_activeSlotIndex);
    }
}
