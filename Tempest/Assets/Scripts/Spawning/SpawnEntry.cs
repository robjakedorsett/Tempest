using System;
using Tempest.Enemies.Enums;
using UnityEngine;

namespace Tempest.Spawning
{
    [Serializable]
    public struct SpawnEntry
    {
        public GameObject prefab;
        public float weight;
        public EnemyTier tier;
    }
}
