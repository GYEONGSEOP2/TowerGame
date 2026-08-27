using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.DOTS
{
    /// <summary>Add once to a SubScene. Assigns and spawns an enemy prefab on a route.</summary>
    public sealed class EnemySpawnerAuthoring : MonoBehaviour
    {
        public GameObject enemyPrefab;
        public EnemyWaveDefinition waveDefinition;
        [Min(0.01f)] public float spawnInterval = 1f;
        [Header("Difficulty")]
        [Min(1f)] public float stageDuration = 30f;
        [Min(1f)] public float healthMultiplierPerStage = 1.25f;
        [Min(1f)] public float speedMultiplierPerStage = 1.05f;
        [Range(0.1f, 1f)] public float spawnIntervalMultiplierPerStage = 0.9f;
        [Min(0.01f)] public float minimumSpawnInterval = 0.1f;
        [Header("Spatial Grid")]
        [Min(0.1f)] public float gridCellSize = 1f;
        [Tooltip("Creates a rectangular loop automatically; manual waypoints are ignored.")]
        public bool useSquareRoute = true;
        [Min(1f)] public float routeWidth = 14f;
        [Min(1f)] public float routeHeight = 8f;
        public List<Transform> waypoints = new();

        private sealed class Baker : Baker<EnemySpawnerAuthoring>
        {
            public override void Bake(EnemySpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var prefab = authoring.enemyPrefab == null
                    ? Entity.Null
                    : GetEntity(authoring.enemyPrefab, TransformUsageFlags.Dynamic);
                var center = (float3)authoring.transform.position;
                var halfWidth = authoring.routeWidth * 0.5f;
                var halfHeight = authoring.routeHeight * 0.5f;

                AddComponent(entity, new EnemySpawner
                {
                    EnemyPrefab = prefab,
                    SpawnInterval = authoring.spawnInterval,
                    BaseSpawnInterval = authoring.spawnInterval,
                    TimeUntilNextSpawn = 0f,
                    SpawnPosition = authoring.useSquareRoute
                        ? center + new float3(-halfWidth, halfHeight, 0f)
                        : center
                });
                if (authoring.waveDefinition != null && authoring.waveDefinition.waves.Count > 0)
                {
                    AddComponent(entity, new EnemyWaveState
                    {
                        HealthMultiplierPerLoop = authoring.waveDefinition.healthMultiplierPerLoop,
                        SpeedMultiplierPerLoop = authoring.waveDefinition.speedMultiplierPerLoop
                    });
                    var waves = AddBuffer<EnemyWave>(entity);
                    var waveSpawns = AddBuffer<EnemyWaveSpawn>(entity);
                    foreach (var wave in authoring.waveDefinition.waves)
                    {
                        if (wave == null)
                            continue;

                        var firstSpawnIndex = waveSpawns.Length;
                        if (wave.spawns != null && wave.spawns.Count > 0)
                        {
                            foreach (var spawn in wave.spawns)
                            {
                                if (spawn == null)
                                    continue;

                                waveSpawns.Add(new EnemyWaveSpawn
                                {
                                    EnemyPrefab = spawn.enemyPrefab == null
                                        ? prefab
                                        : GetEntity(spawn.enemyPrefab, TransformUsageFlags.Dynamic),
                                    SpawnCount = spawn.spawnCount,
                                    SpawnInterval = spawn.spawnInterval
                                });
                            }
                        }
                        else
                        {
                            waveSpawns.Add(new EnemyWaveSpawn
                            {
                                EnemyPrefab = wave.enemyPrefab == null
                                    ? prefab
                                    : GetEntity(wave.enemyPrefab, TransformUsageFlags.Dynamic),
                                SpawnCount = wave.spawnCount,
                                SpawnInterval = wave.spawnInterval
                            });
                        }

                        var spawnGroupCount = waveSpawns.Length - firstSpawnIndex;
                        if (spawnGroupCount == 0)
                            continue;

                        waves.Add(new EnemyWave
                        {
                            FirstSpawnIndex = firstSpawnIndex,
                            SpawnGroupCount = spawnGroupCount,
                            NextWaveDelay = wave.nextWaveDelay,
                            HealthMultiplier = wave.healthMultiplier,
                            SpeedMultiplier = wave.speedMultiplier,
                            KillRewardMultiplier = wave.killRewardMultiplier
                        });
                    }
                }
                else
                {
                    AddComponent(entity, new EnemyDifficulty
                    {
                        StageDuration = authoring.stageDuration,
                        HealthMultiplierPerStage = authoring.healthMultiplierPerStage,
                        SpeedMultiplierPerStage = authoring.speedMultiplierPerStage,
                        SpawnIntervalMultiplierPerStage = authoring.spawnIntervalMultiplierPerStage,
                        MinimumSpawnInterval = authoring.minimumSpawnInterval,
                        ElapsedTime = 0f,
                        CurrentStage = 0
                    });
                }
                AddComponent(entity, new EnemySpatialGridSettings
                {
                    CellSize = authoring.gridCellSize
                });

                var route = AddBuffer<EnemyWaypoint>(entity);
                if (authoring.useSquareRoute)
                {
                    route.Add(new EnemyWaypoint { Position = center + new float3(-halfWidth, halfHeight, 0f) });
                    route.Add(new EnemyWaypoint { Position = center + new float3(halfWidth, halfHeight, 0f) });
                    route.Add(new EnemyWaypoint { Position = center + new float3(halfWidth, -halfHeight, 0f) });
                    route.Add(new EnemyWaypoint { Position = center + new float3(-halfWidth, -halfHeight, 0f) });
                    return;
                }

                foreach (var waypoint in authoring.waypoints)
                {
                    if (waypoint != null)
                        route.Add(new EnemyWaypoint { Position = waypoint.position });
                }
            }
        }
    }
}
