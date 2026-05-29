using Tempest.Weapons;
using UnityEngine;

public class WeaponVisualController : MonoBehaviour
{
    [SerializeField] private Transform weaponHolder;

    private GameObject _currentInstance;

    public WeaponModel ActiveModel { get; private set; }

    public void SpawnWeapon(WeaponDefinition weapon)
    {
        DespawnWeapon();

        if (weapon == null || weapon.weaponPrefab == null)
            return;

        if (weaponHolder == null)
        {
            Debug.LogError("[WeaponVisualController] weaponHolder not assigned.", this);
            return;
        }

        _currentInstance = Instantiate(weapon.weaponPrefab, weaponHolder);
        _currentInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        ActiveModel = _currentInstance.GetComponent<WeaponModel>();
        if (ActiveModel == null)
            Debug.LogError($"[WeaponVisualController] Weapon prefab '{weapon.weaponPrefab.name}' is missing a WeaponModel component.", this);
    }

    public void DespawnWeapon()
    {
        if (_currentInstance != null)
        {
            Destroy(_currentInstance);
            _currentInstance = null;
        }
        ActiveModel = null;
    }

    private void OnDestroy()
    {
        DespawnWeapon();
    }
}
