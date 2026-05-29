using UnityEngine;

namespace Tempest.Core.Utility
{
    public class SelfDestruct : MonoBehaviour
    {
        [SerializeField] private float lifetime = 5f;

        private void Start()
        {
            Destroy(gameObject, lifetime);
        }
    }
}
