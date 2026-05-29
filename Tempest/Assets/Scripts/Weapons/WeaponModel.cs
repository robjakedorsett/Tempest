using UnityEngine;

namespace Tempest.Weapons
{
    public class WeaponModel : MonoBehaviour
    {
        [SerializeField] private Transform muzzlePoint;

        public Transform MuzzlePoint => muzzlePoint;

        private void Awake()
        {
            if (muzzlePoint == null)
                Debug.LogWarning($"[WeaponModel] MuzzlePoint not assigned on {gameObject.name}.", this);
        }
    }
}
