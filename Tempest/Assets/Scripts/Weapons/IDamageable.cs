using UnityEngine;

namespace Tempest.Weapons
{
    public interface IDamageable
    {
        bool TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal);
    }
}
