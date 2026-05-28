using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tempest.Weapons
{
    [CreateAssetMenu(fileName = "WeaponPoolConfig", menuName = "Tempest/Weapons/Weapon Pool Config")]
    public class WeaponPoolConfig : ScriptableObject
    {
        public List<WeaponDefinition> allWeapons = new();
        public List<DeployableDefinition> allDeployables = new();

        public List<WeaponDefinition> GetAvailableWeapons(WeaponSlot slot)
        {
            return allWeapons
                .Where(w => w != null && w.slot == slot && w.unlockedByDefault)
                .ToList();
        }

        public List<DeployableDefinition> GetAvailableDeployables()
        {
            return allDeployables
                .Where(d => d != null && d.unlockedByDefault)
                .ToList();
        }
    }
}
