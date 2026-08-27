using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.DOTS
{
    /// <summary>Authoring data for a repeating sequence of enemy waves.</summary>
    [CreateAssetMenu(menuName = "Game/Enemies/Wave Definition")]
    public sealed class EnemyWaveDefinition : ScriptableObject
    {
        [Min(1f)] public float healthMultiplierPerLoop = 1.25f;
        [Min(1f)] public float speedMultiplierPerLoop = 1.05f;
        public List<EnemyWaveDefinitionEntry> waves = new();
    }

    [Serializable]
    public sealed class EnemyWaveDefinitionEntry
    {
        [Header("Legacy Single Spawn")]
        [Tooltip("Used only when Spawns is empty. Existing wave assets remain compatible.")]
        public GameObject enemyPrefab;
        [Min(1)] public int spawnCount = 10;
        [Min(0.01f)] public float spawnInterval = 0.75f;
        [Header("Spawn Groups")]
        public List<EnemyWaveSpawnDefinition> spawns = new();
        [Min(0f)] public float nextWaveDelay = 3f;
        [Min(0.01f)] public float healthMultiplier = 1f;
        [Min(0.01f)] public float speedMultiplier = 1f;
        [Min(0f)] public float killRewardMultiplier = 1f;
    }

    [Serializable]
    public sealed class EnemyWaveSpawnDefinition
    {
        public GameObject enemyPrefab;
        [Min(1)] public int spawnCount = 10;
        [Min(0.01f)] public float spawnInterval = 0.75f;
    }
}
