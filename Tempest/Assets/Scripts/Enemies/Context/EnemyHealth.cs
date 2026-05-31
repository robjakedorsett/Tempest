using System.Collections;
using Tempest.Weapons;
using UnityEngine;

namespace Tempest.Enemies
{
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        private float _maxHealth;
        private float _currentHealth;
        private int _xpValue;
        private GameObject _deathEffectPrefab;
        private GameObject _hitEffectPrefab;
        private bool _isDead;

        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;
        private Color _originalColor;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public bool IsDead => _isDead;

        public void Initialize(EnemyDefinition definition)
        {
            _maxHealth = definition.maxHealth;
            _currentHealth = _maxHealth;
            _xpValue = definition.xpValue;
            _deathEffectPrefab = definition.deathEffectPrefab;
            _hitEffectPrefab = definition.hitEffectPrefab;

            _renderer = GetComponentInChildren<Renderer>();
            _propBlock = new MaterialPropertyBlock();

            if (_renderer != null)
            {
                _renderer.GetPropertyBlock(_propBlock);
                _originalColor = _propBlock.GetColor("_BaseColor");
                if (_originalColor == default)
                    _originalColor = _renderer.sharedMaterial.GetColor("_BaseColor");
            }
        }

        public bool TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (_isDead) return false;

            _currentHealth -= damage;

            SpawnHitEffect(hitPoint, hitNormal);
            StartCoroutine(HitFlash());

            if (_currentHealth <= 0f)
            {
                Die();
                return true;
            }

            return false;
        }

        private void Die()
        {
            _isDead = true;
            GameEventBus.RaiseEnemyKilled(_xpValue);

            if (_deathEffectPrefab != null)
                Instantiate(_deathEffectPrefab, transform.position, transform.rotation);

            Destroy(gameObject);
        }

        private void SpawnHitEffect(Vector3 hitPoint, Vector3 hitNormal)
        {
            if (_hitEffectPrefab != null)
                Instantiate(_hitEffectPrefab, hitPoint, Quaternion.LookRotation(hitNormal));
        }

        private IEnumerator HitFlash()
        {
            if (_renderer == null) yield break;

            _propBlock.SetColor("_BaseColor", Color.white);
            _renderer.SetPropertyBlock(_propBlock);

            float duration = 0.1f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                Color current = Color.Lerp(Color.white, _originalColor, t);
                _propBlock.SetColor("_BaseColor", current);
                _renderer.SetPropertyBlock(_propBlock);
                yield return null;
            }

            _propBlock.SetColor("_BaseColor", _originalColor);
            _renderer.SetPropertyBlock(_propBlock);
        }
    }
}
