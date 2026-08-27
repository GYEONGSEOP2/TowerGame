using Unity.Entities;

namespace Game.DOTS
{
    /// <summary>Runtime configuration for the isolated enemy load test scene.</summary>
    public struct EnemyStressTestSettings : IComponentData
    {
        public int TotalSpawnCount;
        public int SpawnPerFrame;
        public int SpawnColumns;
        public float SpawnSpacing;
        public int SpawnedCount;
        public bool IsRunning;
        public bool DisableNormalSpawner;
    }

    /// <summary>Identifies enemies created by the dedicated stress-test spawner.</summary>
    public struct StressTestEnemy : IComponentData
    {
    }
}
