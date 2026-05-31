using Tempest.Enemies.Enums;
using UnityEngine;

namespace Tempest.Enemies
{
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "Tempest/Enemies/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string enemyName;
        public EnemyTier tier;

        [Header("Stats")]
        public float maxHealth = 100f;
        public float moveSpeed = 3.5f;
        public float damage = 10f;
        public float attackRange = 2f;
        public float attackCooldown = 1f;
        public float detectionRadius = 10f;
        public float roamRadius = 8f;

        [Header("Rewards")]
        public int xpValue = 10;

        [Header("Effects")]
        public GameObject deathEffectPrefab;
        public GameObject hitEffectPrefab;
    }
}
