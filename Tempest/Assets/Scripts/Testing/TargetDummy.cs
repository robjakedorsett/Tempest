using System.Collections;
using Tempest.Weapons;
using UnityEngine;

namespace Tempest.Testing
{
    public class TargetDummy : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float respawnDelay = 2f;

        private float _currentHealth;
        private Collider _collider;
        private Renderer _renderer;

        private void Awake()
        {
            _currentHealth = maxHealth;
            _collider = GetComponent<Collider>();
            _renderer = GetComponentInChildren<Renderer>();
        }

        public bool TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (_currentHealth <= 0f) return false;

            _currentHealth -= damage;
            if (_currentHealth <= 0f)
            {
                Die();
                return true;
            }
            return false;
        }

        private void Die()
        {
            _collider.enabled = false;
            _renderer.enabled = false;
            StartCoroutine(RespawnAfterDelay());
        }

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelay);
            _currentHealth = maxHealth;
            _collider.enabled = true;
            _renderer.enabled = true;
        }
    }
}
