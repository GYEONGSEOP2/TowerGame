using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.DOTS
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct EnemySpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnemySpawner>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<EnemyStressTestSettings>(out var stressTest) &&
                stressTest.DisableNormalSpawner)
                return;

            var spawner = SystemAPI.GetSingletonRW<EnemySpawner>();
            if (spawner.ValueRO.EnemyPrefab == Entity.Null)
                return;

            var visuals = SystemAPI.GetComponentLookup<EnemyVisual>(true);

            var hasWaveState = SystemAPI.HasSingleton<EnemyWaveState>();
            var waveState = hasWaveState
                ? SystemAPI.GetSingletonRW<EnemyWaveState>()
                : default;
            if (hasWaveState && waveState.ValueRO.IsWaitingForNextWave)
                return;

            if (hasWaveState)
            {
                var waveSpawns = SystemAPI.GetSingletonBuffer<EnemyWaveSpawn>(true);
                if (waveSpawns.Length == 0 || waveState.ValueRO.CurrentSpawnIndex >= waveSpawns.Length ||
                    waveState.ValueRO.SpawnedInCurrentSpawn >= waveSpawns[waveState.ValueRO.CurrentSpawnIndex].SpawnCount)
                    return;
            }

            var remaining = spawner.ValueRO.TimeUntilNextSpawn - SystemAPI.Time.DeltaTime;
            if (remaining > 0f)
            {
                spawner.ValueRW.TimeUntilNextSpawn = remaining;
                return;
            }

            var ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var enemy = ecb.Instantiate(spawner.ValueRO.EnemyPrefab);
            var spawnPosition = spawner.ValueRO.SpawnPosition;
            spawnPosition.z = -0.5f;
            var visualScale = visuals.HasComponent(spawner.ValueRO.EnemyPrefab)
                ? visuals[spawner.ValueRO.EnemyPrefab].Scale
                : 1f;
            ecb.SetComponent(enemy, LocalTransform.FromPositionRotationScale(
                spawnPosition,
                quaternion.identity,
                visualScale));
            spawner.ValueRW.TimeUntilNextSpawn = math.max(0.01f, spawner.ValueRO.SpawnInterval);
            if (hasWaveState)
            {
                waveState.ValueRW.SpawnedInWave++;
                waveState.ValueRW.SpawnedInCurrentSpawn++;
            }
        }
    }
}
