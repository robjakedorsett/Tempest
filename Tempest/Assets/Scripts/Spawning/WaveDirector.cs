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
        private int _swarmCount;
        private int _currentSwarmActiveCount;

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

            _currentSwarmActiveCount--;
            if (_currentSwarmActiveCount < 0) _currentSwarmActiveCount = 0;

            _ambientActiveCount--;
            if (_ambientActiveCount < 0) _ambientActiveCount = 0;

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
            float currentInterval = config.swarmInterval;

            while (_isSpawning)
            {
                yield return new WaitForSeconds(currentInterval);
                if (!_isSpawning) yield break;

                _swarmCount++;
                _currentSwarmActiveCount = 0;
                GameEventBus.RaiseSwarmStarted(_swarmCount);

                yield return StartCoroutine(ExecuteSwarm(_swarmCount, false));

                currentInterval = Mathf.Max(
                    config.minSwarmInterval,
                    currentInterval * config.swarmIntervalDecay);
            }
        }

        private IEnumerator ExecuteSwarm(int swarmNumber, bool isFinale)
        {
            int riftCount = CalculateRiftCount(swarmNumber, isFinale);
            int enemiesPerRift = CalculateEnemiesPerRift(swarmNumber, isFinale);
            bool includeElite = isFinale || ShouldIncludeElite(swarmNumber);

            for (int r = 0; r < riftCount; r++)
            {
                Vector3 riftCenter;
                if (!TryGetSpawnPosition(config.spawnRadius, config.minPlayerDistance, out riftCenter))
                    continue;

                int elitesForThisRift = 0;
                if (includeElite && r == 0)
                    elitesForThisRift = isFinale ? config.finaleEliteCount : 1;

                yield return StartCoroutine(SpawnFromRift(riftCenter, enemiesPerRift, elitesForThisRift));
            }

            // Wait for this swarm's enemies to die
            while (_currentSwarmActiveCount > 0)
                yield return null;

            GameEventBus.RaiseSwarmEnded();
        }

        private IEnumerator SpawnFromRift(Vector3 riftCenter, int enemyCount, int eliteCount)
        {
            int elitesSpawned = 0;
            float elapsed = 0f;

            float[] spawnTimes = new float[enemyCount];
            for (int i = 0; i < enemyCount; i++)
                spawnTimes[i] = Random.Range(0f, config.spawnDuration);
            System.Array.Sort(spawnTimes);

            for (int i = 0; i < enemyCount; i++)
            {
                float waitUntil = spawnTimes[i];
                if (waitUntil > elapsed)
                {
                    yield return new WaitForSeconds(waitUntil - elapsed);
                    elapsed = waitUntil;
                }

                if (!_isSpawning) yield break;

                Vector3 spawnPos;
                if (!TryGetClusterPosition(riftCenter, config.riftRadius, out spawnPos))
                    continue;

                bool spawnElite = elitesSpawned < eliteCount && (enemyCount - i) <= (eliteCount - elitesSpawned);
                if (!spawnElite && elitesSpawned < eliteCount && Random.value < 0.3f)
                    spawnElite = true;

                if (spawnElite)
                {
                    SpawnEnemy(EnemyTier.Elite, spawnPos);
                    elitesSpawned++;
                }
                else
                {
                    SpawnEnemy(EnemyTier.Swarm, spawnPos);
                }

                TrackSpawn(false);
                _currentSwarmActiveCount++;
            }
        }

        private IEnumerator FinaleSequence()
        {
            _isSpawning = true;
            _swarmCount++;
            _currentSwarmActiveCount = 0;
            GameEventBus.RaiseSwarmStarted(_swarmCount);

            yield return StartCoroutine(ExecuteSwarm(_swarmCount, true));

            _isSpawning = false;
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

        private bool TryGetClusterPosition(Vector3 center, float radius, out Vector3 result)
        {
            result = Vector3.zero;

            for (int i = 0; i < 5; i++)
            {
                Vector3 randomPoint = center + Random.insideUnitSphere * radius;
                randomPoint.y = center.y;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomPoint, out hit, radius, NavMesh.AllAreas))
                {
                    result = hit.position;
                    return true;
                }
            }

            result = center;
            return true;
        }

        private int CalculateRiftCount(int swarmNumber, bool isFinale)
        {
            if (isFinale) return config.maxRiftCount;
            int extra = (swarmNumber - 1) / config.riftCountEscalationInterval;
            return Mathf.Min(config.baseRiftCount + extra, config.maxRiftCount);
        }

        private int CalculateEnemiesPerRift(int swarmNumber, bool isFinale)
        {
            if (isFinale) return Mathf.RoundToInt(config.baseEnemiesPerRift * Mathf.Pow(config.enemyCountGrowth, 6));
            return Mathf.RoundToInt(config.baseEnemiesPerRift * Mathf.Pow(config.enemyCountGrowth, swarmNumber - 1));
        }

        private bool ShouldIncludeElite(int swarmNumber)
        {
            if (swarmNumber < config.eliteSwarmThreshold) return false;
            return Random.value < config.eliteChance;
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
