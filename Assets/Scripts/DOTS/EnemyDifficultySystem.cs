using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.DOTS
{
    /// <summary>Applies staged difficulty to the enemy prefab before future enemies are spawned.</summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(EnemySpawnSystem))]
    [BurstCompile]
    public partial struct EnemyDifficultySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnemyDifficulty>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var difficulty = SystemAPI.GetSingletonRW<EnemyDifficulty>();
            difficulty.ValueRW.ElapsedTime += SystemAPI.Time.DeltaTime;

            var stageDuration = math.max(0.01f, difficulty.ValueRO.StageDuration);
            var nextStage = (int)math.floor(difficulty.ValueRO.ElapsedTime / stageDuration);
            if (nextStage <= difficulty.ValueRO.CurrentStage)
                return;

            var spawner = SystemAPI.GetSingletonRW<EnemySpawner>();
            var enemyPrefab = spawner.ValueRO.EnemyPrefab;
            if (enemyPrefab == Entity.Null ||
                !state.EntityManager.HasComponent<Enemy>(enemyPrefab) ||
                !state.EntityManager.HasComponent<EnemyBaseStats>(enemyPrefab) ||
                !state.EntityManager.HasComponent<EnemyHealth>(enemyPrefab))
                return;

            var baseStats = state.EntityManager.GetComponentData<EnemyBaseStats>(enemyPrefab);
            var enemy = state.EntityManager.GetComponentData<Enemy>(enemyPrefab);
            var health = state.EntityManager.GetComponentData<EnemyHealth>(enemyPrefab);

            enemy.MoveSpeed = baseStats.MoveSpeed * math.pow(difficulty.ValueRO.SpeedMultiplierPerStage, nextStage);
            health.Max = baseStats.MaxHealth * math.pow(difficulty.ValueRO.HealthMultiplierPerStage, nextStage);
            health.Current = health.Max;

            state.EntityManager.SetComponentData(enemyPrefab, enemy);
            state.EntityManager.SetComponentData(enemyPrefab, health);
            spawner.ValueRW.SpawnInterval = math.max(
                difficulty.ValueRO.MinimumSpawnInterval,
                spawner.ValueRO.BaseSpawnInterval * math.pow(difficulty.ValueRO.SpawnIntervalMultiplierPerStage, nextStage));
            difficulty.ValueRW.CurrentStage = nextStage;
        }
    }
}
