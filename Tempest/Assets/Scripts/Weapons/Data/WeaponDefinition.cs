using UnityEngine;

namespace Tempest.Weapons
{
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "Tempest/Weapons/Weapon Definition")]
    public class WeaponDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string weaponName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        public WeaponSlot slot;
        public WeaponType type;

        [Header("Stats")]
        public float damage;
        public float fireRate;
        public int magazineSize;
        public float reloadTime;
        public float range;
        public float spread;
        public float bloomPerShot = 0.5f;
        public float maxSpread = 8f;
        public float bloomDecayRate = 5f;
        public FireMode fireMode;

        [Header("Recoil")]
        [Range(0f, 2f)]
        public float recoil = 1f;
        public float aimKickUp = 0.8f;
        public float aimKickHorizontal = 0.3f;

        [Header("Prefab")]
        public GameObject weaponPrefab;

        [Header("Effects")]
        public GameObject muzzleFlashPrefab;
        public GameObject hitEffectPrefab;
        public GameObject killEffectPrefab;
        public AudioClip fireSound;
        public AudioClip reloadSound;

        [Header("Meta")]
        public bool unlockedByDefault = true;
    }
}
