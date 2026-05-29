using Tempest.Weapons;
using UnityEngine;

public class WeaponVisualController : MonoBehaviour
{
    private const string WeaponLayerName = "Weapon";

    [SerializeField] private Transform weaponHolder;

    private GameObject _currentInstance;
    private int _weaponLayer;

    public WeaponModel ActiveModel { get; private set; }

    private void Awake()
    {
        _weaponLayer = LayerMask.NameToLayer(WeaponLayerName);
        if (_weaponLayer == -1)
            Debug.LogError($"[WeaponVisualController] Layer '{WeaponLayerName}' not found. Add it in Project Settings > Tags and Layers.", this);
    }

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

        if (_weaponLayer != -1)
            SetLayerRecursive(_currentInstance, _weaponLayer);

        ActiveModel = _currentInstance.GetComponent<WeaponModel>();
        if (ActiveModel == null)
            Debug.LogError($"[WeaponVisualController] Weapon prefab '{weapon.weaponPrefab.name}' is missing a WeaponModel component.", this);
    }

    private static void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
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
