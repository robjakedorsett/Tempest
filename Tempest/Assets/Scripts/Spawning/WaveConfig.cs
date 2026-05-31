using UnityEngine;

namespace Tempest.Spawning
{
    [CreateAssetMenu(menuName = "Tempest/Waves/Wave Config")]
    public class WaveConfig : ScriptableObject
    {
        [Header("Enemy Pool")]
        public SpawnEntry[] enemies;

        [Header("Swarm Timing")]
        public float swarmInterval = 45f;
        public float swarmIntervalDecay = 0.9f;
        public float minSwarmInterval = 20f;

        [Header("Swarm Scaling")]
        public int baseEnemiesPerRift = 8;
        public float enemyCountGrowth = 1.25f;
        public int baseRiftCount = 1;
        public int maxRiftCount = 3;
        public int riftCountEscalationInterval = 3;

        [Header("Rift Spatial")]
        public float spawnRadius = 25f;
        public float riftRadius = 8f;
        public float minPlayerDistance = 12f;
        public float spawnDuration = 40f;

        [Header("Ambient")]
        public float ambientSpawnRate = 0.2f;
        public int ambientMaxActive = 4;

        [Header("Elite")]
        public int eliteSwarmThreshold = 4;
        public float eliteChance = 0.5f;
        public int finaleEliteCount = 2;

        [Header("Effects")]
        public GameObject riftVfxPrefab;
    }
}
