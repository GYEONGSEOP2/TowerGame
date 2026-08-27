using Unity.Entities;
using UnityEngine;

namespace Game.DOTS
{
    /// <summary>Place only in the stress-test SubScene to configure batched enemy creation.</summary>
    public sealed class EnemyStressTestAuthoring : MonoBehaviour
    {
        [Min(1)] public int totalSpawnCount = 10000;
        [Min(1)] public int spawnPerFrame = 250;
        [Min(1)] public int spawnColumns = 128;
        [Min(0f)] public float spawnSpacing = 0.04f;
        public bool disableNormalSpawner = true;

        private sealed class Baker : Baker<EnemyStressTestAuthoring>
        {
            public override void Bake(EnemyStressTestAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new EnemyStressTestSettings
                {
                    TotalSpawnCount = authoring.totalSpawnCount,
                    SpawnPerFrame = authoring.spawnPerFrame,
                    SpawnColumns = authoring.spawnColumns,
                    SpawnSpacing = authoring.spawnSpacing,
                    SpawnedCount = 0,
                    IsRunning = true,
                    DisableNormalSpawner = authoring.disableNormalSpawner
                });
            }
        }
    }
}
