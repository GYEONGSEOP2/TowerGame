using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.DOTS
{
    /// <summary>Applies data-authored wave settings and advances after each spawn quota is met.</summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(EnemySpawnSystem))]
    [BurstCompile]
    public partial struct EnemyWaveSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnemyWaveState>();
            state.RequireForUpdate<EnemySpawner>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var waveState = SystemAPI.GetSingletonRW<EnemyWaveState>();
            var waves = SystemAPI.GetSingletonBuffer<EnemyWave>();
            var waveSpawns = SystemAPI.GetSingletonBuffer<EnemyWaveSpawn>(true);
            var spawner = SystemAPI.GetSingletonRW<EnemySpawner>();
            if (waves.Length == 0)
                return;

            if (!waveState.ValueRO.IsInitialized)
            {
                ApplyCurrentSpawn(ref state, ref waveState.ValueRW, ref spawner.ValueRW, waves, waveSpawns);
                waveState.ValueRW.IsInitialized = true;
                return;
            }

            var currentWave = waves[waveState.ValueRO.CurrentWaveIndex];
            var currentSpawn = waveSpawns[waveState.ValueRO.CurrentSpawnIndex];
            if (waveState.ValueRO.SpawnedInCurrentSpawn < currentSpawn.SpawnCount)
                return;

            var lastSpawnIndex = currentWave.FirstSpawnIndex + currentWave.SpawnGroupCount - 1;
            if (waveState.ValueRO.CurrentSpawnIndex < lastSpawnIndex)
            {
                waveState.ValueRW.CurrentSpawnIndex++;
                waveState.ValueRW.SpawnedInCurrentSpawn = 0;
                ApplyCurrentSpawn(ref state, ref waveState.ValueRW, ref spawner.ValueRW, waves, waveSpawns);
                return;
            }

            if (!waveState.ValueRO.IsWaitingForNextWave)
            {
                waveState.ValueRW.IsWaitingForNextWave = true;
                waveState.ValueRW.TimeUntilNextWave = currentWave.NextWaveDelay;
                return;
            }

            waveState.ValueRW.TimeUntilNextWave -= SystemAPI.Time.DeltaTime;
            if (waveState.ValueRO.TimeUntilNextWave > 0f)
                return;

            waveState.ValueRW.CompletedWaveCount++;
            waveState.ValueRW.CurrentWaveIndex++;
            if (waveState.ValueRO.CurrentWaveIndex >= waves.Length)
            {
                waveState.ValueRW.CurrentWaveIndex = 0;
                waveState.ValueRW.CompletedLoopCount++;
            }

            waveState.ValueRW.SpawnedInWave = 0;
            waveState.ValueRW.SpawnedInCurrentSpawn = 0;
            waveState.ValueRW.IsWaitingForNextWave = false;
            ApplyCurrentSpawn(ref state, ref waveState.ValueRW, ref spawner.ValueRW, waves, waveSpawns);
        }

        private static void ApplyCurrentSpawn(
            ref SystemState state,
            ref EnemyWaveState waveState,
            ref EnemySpawner spawner,
            DynamicBuffer<EnemyWave> waves,
            DynamicBuffer<EnemyWaveSpawn> waveSpawns)
        {
            var wave = waves[waveState.CurrentWaveIndex];
            waveState.CurrentSpawnIndex = math.clamp(
                waveState.CurrentSpawnIndex,
                wave.FirstSpawnIndex,
                wave.FirstSpawnIndex + wave.SpawnGroupCount - 1);
            var spawn = waveSpawns[waveState.CurrentSpawnIndex];
            var prefab = spawn.EnemyPrefab == Entity.Null ? spawner.EnemyPrefab : spawn.EnemyPrefab;
            if (prefab == Entity.Null ||
                !state.EntityManager.HasComponent<Enemy>(prefab) ||
                !state.EntityManager.HasComponent<EnemyBaseStats>(prefab) ||
                !state.EntityManager.HasComponent<EnemyHealth>(prefab))
                return;

            var baseStats = state.EntityManager.GetComponentData<EnemyBaseStats>(prefab);
            var enemy = state.EntityManager.GetComponentData<Enemy>(prefab);
            var health = state.EntityManager.GetComponentData<EnemyHealth>(prefab);
            var healthMultiplier = wave.HealthMultiplier * math.pow(waveState.HealthMultiplierPerLoop, waveState.CompletedLoopCount);
            var speedMultiplier = wave.SpeedMultiplier * math.pow(waveState.SpeedMultiplierPerLoop, waveState.CompletedLoopCount);

            enemy.MoveSpeed = baseStats.MoveSpeed * speedMultiplier;
            enemy.KillReward = math.max(0, (int)math.round(baseStats.KillReward * wave.KillRewardMultiplier));
            health.Max = baseStats.MaxHealth * healthMultiplier;
            health.Current = health.Max;

            state.EntityManager.SetComponentData(prefab, enemy);
            state.EntityManager.SetComponentData(prefab, health);
            spawner.EnemyPrefab = prefab;
            spawner.SpawnInterval = math.max(0.01f, spawn.SpawnInterval);
            spawner.TimeUntilNextSpawn = 0f;
        }
    }
}
