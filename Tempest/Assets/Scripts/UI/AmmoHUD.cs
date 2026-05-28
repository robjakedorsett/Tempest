using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class AmmoHUD : MonoBehaviour
{
    private PlayerWeaponController _weaponController;
    private Label _ammoLabel;
    private Label _reloadLabel;

    private void Awake()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _ammoLabel = root.Q<Label>("ammo-label");
        _reloadLabel = root.Q<Label>("reload-label");
    }

    private void OnEnable()
    {
        _weaponController = FindFirstObjectByType<PlayerWeaponController>();
        if (_weaponController == null) return;

        _weaponController.OnAmmoChanged += HandleAmmoChanged;
        _weaponController.OnReloadStateChanged += HandleReloadStateChanged;

        if (_weaponController.HasWeapon)
            HandleAmmoChanged(_weaponController.CurrentAmmo, _weaponController.MaxAmmo);
    }

    private void OnDisable()
    {
        if (_weaponController == null) return;

        _weaponController.OnAmmoChanged -= HandleAmmoChanged;
        _weaponController.OnReloadStateChanged -= HandleReloadStateChanged;
    }

    private void HandleAmmoChanged(int current, int max)
    {
        _ammoLabel.text = $"{current} / {max}";

        bool isLow = max > 0 && current <= max / 4;
        _ammoLabel.EnableInClassList("ammo-label--low", isLow);
    }

    private void HandleReloadStateChanged(bool isReloading)
    {
        _reloadLabel.EnableInClassList("reload-label--visible", isReloading);
    }
}
