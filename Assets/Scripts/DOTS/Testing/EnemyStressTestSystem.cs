using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.DOTS
{
    /// <summary>Creates a bounded number of test enemies per frame to avoid a single spawn spike.</summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemySpawnSystem))]
    [UpdateBefore(typeof(EnemyMovementSystem))]
    [BurstCompile]
    public partial struct EnemyStressTestSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnemyStressTestSettings>();
            state.RequireForUpdate<EnemySpawner>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var settings = SystemAPI.GetSingletonRW<EnemyStressTestSettings>();
            if (!settings.ValueRO.IsRunning)
                return;

            var remaining = settings.ValueRO.TotalSpawnCount - settings.ValueRO.SpawnedCount;
            if (remaining <= 0)
            {
                settings.ValueRW.IsRunning = false;
                return;
            }

            var spawner = SystemAPI.GetSingleton<EnemySpawner>();
            if (spawner.EnemyPrefab == Entity.Null)
                return;

            var spawnCount = math.min(remaining, math.max(1, settings.ValueRO.SpawnPerFrame));
            var columns = math.max(1, settings.ValueRO.SpawnColumns);
            var spacing = math.max(0f, settings.ValueRO.SpawnSpacing);
            var ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            for (var index = 0; index < spawnCount; index++)
            {
                var spawnIndex = settings.ValueRO.SpawnedCount + index;
                var column = spawnIndex % columns;
                var row = spawnIndex / columns;
                var position = spawner.SpawnPosition;
                position.xy += new float2(
                    (column - (columns - 1) * 0.5f) * spacing,
                    row * spacing);
                position.z = -0.5f;

                var enemy = ecb.Instantiate(spawner.EnemyPrefab);
                ecb.SetComponent(enemy, LocalTransform.FromPosition(position));
                ecb.AddComponent(enemy, new StressTestEnemy());
            }

            settings.ValueRW.SpawnedCount += spawnCount;
            if (settings.ValueRW.SpawnedCount >= settings.ValueRO.TotalSpawnCount)
                settings.ValueRW.IsRunning = false;
        }
    }
}
