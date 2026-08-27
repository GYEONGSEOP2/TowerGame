using Unity.Entities;
using Unity.Mathematics;

namespace Game.DOTS
{
    /// <summary>Runtime data stored on every spawned enemy.</summary>
    public struct Enemy : IComponentData
    {
        public float MoveSpeed;
        public int CurrentWaypoint;
        public int KillReward;
        public float SlowRemainingTime;
        public float SlowMultiplier;
    }

    /// <summary>
    /// Cached data for the current route segment. Direction and distance are recalculated only
    /// when an enemy reaches a waypoint, avoiding a square root for every enemy each frame.
    /// </summary>
    public struct EnemyMovementSegment : IComponentData
    {
        public float3 Direction;
        public float RemainingDistance;
    }

    /// <summary>Singleton configuration for the enemy spawning loop.</summary>
    public struct EnemySpawner : IComponentData
    {
        public Entity EnemyPrefab;
        public float SpawnInterval;
        public float BaseSpawnInterval;
        public float TimeUntilNextSpawn;
        public float3 SpawnPosition;
    }

    /// <summary>One baked wave entry authored by an EnemyWaveDefinition asset.</summary>
    [InternalBufferCapacity(8)]
    public struct EnemyWave : IBufferElementData
    {
        public int FirstSpawnIndex;
        public int SpawnGroupCount;
        public float NextWaveDelay;
        public float HealthMultiplier;
        public float SpeedMultiplier;
        public float KillRewardMultiplier;
    }

    /// <summary>One enemy prefab and quota within a baked wave.</summary>
    [InternalBufferCapacity(16)]
    public struct EnemyWaveSpawn : IBufferElementData
    {
        public Entity EnemyPrefab;
        public int SpawnCount;
        public float SpawnInterval;
    }

    /// <summary>Runtime progress through the baked repeating wave sequence.</summary>
    public struct EnemyWaveState : IComponentData
    {
        public int CurrentWaveIndex;
        public int CurrentSpawnIndex;
        public int SpawnedInCurrentSpawn;
        public int SpawnedInWave;
        public int CompletedWaveCount;
        public int CompletedLoopCount;
        public float TimeUntilNextWave;
        public float HealthMultiplierPerLoop;
        public float SpeedMultiplierPerLoop;
        public bool IsInitialized;
        public bool IsWaitingForNextWave;
    }

    /// <summary>Base values used to scale future enemy spawns by difficulty stage.</summary>
    public struct EnemyBaseStats : IComponentData
    {
        public float MoveSpeed;
        public float MaxHealth;
        public int KillReward;
    }

    /// <summary>Static visual identity baked from an EnemyDefinition.</summary>
    public struct EnemyVisual : IComponentData
    {
        public int MeshIndex;
        public float Scale;
        public float4 TypeColor;
    }

    /// <summary>Shared staged difficulty configuration and runtime state.</summary>
    public struct EnemyDifficulty : IComponentData
    {
        public float StageDuration;
        public float HealthMultiplierPerStage;
        public float SpeedMultiplierPerStage;
        public float SpawnIntervalMultiplierPerStage;
        public float MinimumSpawnInterval;
        public float ElapsedTime;
        public int CurrentStage;
    }

    /// <summary>Configuration for the shared spatial lookup grid used by combat systems.</summary>
    public struct EnemySpatialGridSettings : IComponentData
    {
        public float CellSize;
    }

    /// <summary>A point in the shared enemy route. Points are traversed in buffer order.</summary>
    [InternalBufferCapacity(8)]
    public struct EnemyWaypoint : IBufferElementData
    {
        public float3 Position;
    }
}
