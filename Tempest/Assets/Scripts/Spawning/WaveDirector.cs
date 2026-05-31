using System.Collections;
using Tempest.Enemies.Enums;
using UnityEngine;
using UnityEngine.AI;

namespace Tempest.Spawning
{
    public class WaveDirector : MonoBehaviour
    {
        [SerializeField] private WaveConfig config;

        private int _activeEnemyCount;
        private int _ambientActiveCount;
        private bool _isSpawning;
        private Coroutine _ambientCoroutine;
        private Coroutine _swarmCoroutine;

        public int ActiveEnemyCount => _activeEnemyCount;
        public bool IsSpawning => _isSpawning;

        private void OnEnable()
        {
            GameEventBus.OnEnemyKilled += HandleEnemyKilled;
        }

        private void OnDisable()
        {
            GameEventBus.OnEnemyKilled -= HandleEnemyKilled;
        }

        public void StartSpawning()
        {
            if (_isSpawning) return;
            _isSpawning = true;
            _ambientCoroutine = StartCoroutine(AmbientTrickle());
            _swarmCoroutine = StartCoroutine(SwarmLoop());
        }

        public void StopSpawning()
        {
            _isSpawning = false;
            if (_ambientCoroutine != null) StopCoroutine(_ambientCoroutine);
            if (_swarmCoroutine != null) StopCoroutine(_swarmCoroutine);
        }

        public void TriggerFinale()
        {
            if (!_isSpawning) return;
            StopSpawning();
            StartCoroutine(FinaleSequence());
        }

        private void HandleEnemyKilled(int xpValue)
        {
            _activeEnemyCount--;
            if (_activeEnemyCount < 0) _activeEnemyCount = 0;
            GameEventBus.RaiseActiveEnemyCountChanged(_activeEnemyCount);
        }

        private void TrackSpawn(bool isAmbient)
        {
            _activeEnemyCount++;
            if (isAmbient) _ambientActiveCount++;
            GameEventBus.RaiseActiveEnemyCountChanged(_activeEnemyCount);
        }

        private IEnumerator AmbientTrickle()
        {
            while (_isSpawning)
            {
                if (_ambientActiveCount < config.ambientMaxActive)
                {
                    Vector3 pos;
                    if (TryGetSpawnPosition(config.spawnRadius, config.minPlayerDistance, out pos))
                    {
                        SpawnEnemy(EnemyTier.Swarm, pos);
                        TrackSpawn(true);
                    }
                }

                float delay = 1f / config.ambientSpawnRate;
                yield return new WaitForSeconds(delay);
            }
        }

        private IEnumerator SwarmLoop()
        {
            // Implemented in Task 4
            yield break;
        }

        private IEnumerator FinaleSequence()
        {
            // Implemented in Task 5
            yield break;
        }

        private void SpawnEnemy(EnemyTier tier, Vector3 position)
        {
            GameObject prefab = ChooseEnemyByTier(tier);
            if (prefab == null) return;
            Instantiate(prefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        }

        private GameObject ChooseEnemyByTier(EnemyTier tier)
        {
            float totalWeight = 0f;
            foreach (var entry in config.enemies)
            {
                if (entry.tier == tier)
                    totalWeight += entry.weight;
            }

            if (totalWeight <= 0f) return null;

            float roll = Random.value * totalWeight;
            foreach (var entry in config.enemies)
            {
                if (entry.tier != tier) continue;
                if (roll < entry.weight) return entry.prefab;
                roll -= entry.weight;
            }

            // Fallback: return first matching tier
            foreach (var entry in config.enemies)
            {
                if (entry.tier == tier) return entry.prefab;
            }

            return null;
        }

        private bool TryGetSpawnPosition(float radius, float minPlayerDist, out Vector3 result)
        {
            result = Vector3.zero;
            float bestDist = 0f;
            Vector3 bestCandidate = Vector3.zero;
            bool foundAny = false;

            for (int i = 0; i < 10; i++)
            {
                Vector3 randomPoint = transform.position + Random.insideUnitSphere * radius;
                randomPoint.y = transform.position.y;

                NavMeshHit hit;
                if (!NavMesh.SamplePosition(randomPoint, out hit, radius * 0.5f, NavMesh.AllAreas))
                    continue;

                float playerDist = PlayerRegistry.GetMinDistanceFromPlayers(hit.position);

                if (playerDist >= minPlayerDist)
                {
                    result = hit.position;
                    return true;
                }

                if (playerDist > bestDist)
                {
                    bestDist = playerDist;
                    bestCandidate = hit.position;
                    foundAny = true;
                }
            }

            if (foundAny)
            {
                result = bestCandidate;
                return true;
            }

            return false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (config == null) return;

            Gizmos.color = new Color(1f, 0.3f, 0f, 0.15f);
            Gizmos.DrawWireSphere(transform.position, config.spawnRadius);

            Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, config.minPlayerDistance);
        }
#endif
    }
}
