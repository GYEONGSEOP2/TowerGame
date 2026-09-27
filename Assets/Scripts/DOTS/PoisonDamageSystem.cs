using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.DOTS
{
    /// <summary>Applies refreshable poison damage at fixed intervals and grants rewards for poison kills.</summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ProjectileMovementSystem))]
    [UpdateBefore(typeof(EnemyDeathSystem))]
    [BurstCompile]
    public partial struct PoisonDamageSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerCurrency>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deaths = SystemAPI.GetComponentLookup<EnemyDeadTag>();
            var currency = SystemAPI.GetSingletonRW<PlayerCurrency>();
            var deltaTime = SystemAPI.Time.DeltaTime;
            var earnedReward = 0;
            var killCount = 0;

            foreach (var (poison, health, enemy, entity) in SystemAPI
                         .Query<RefRW<EnemyPoison>, RefRW<EnemyHealth>, RefRO<Enemy>>()
                         .WithEntityAccess())
            {
                if (poison.ValueRO.RemainingDuration <= 0f ||
                    deaths.IsComponentEnabled(entity))
                    continue;

                poison.ValueRW.RemainingDuration = math.max(0f, poison.ValueRO.RemainingDuration - deltaTime);
                poison.ValueRW.TimeUntilNextTick -= deltaTime;
                if (poison.ValueRO.TimeUntilNextTick > 0f)
                    continue;

                var interval = math.max(0.01f, poison.ValueRO.TickInterval);
                var tickCount = math.max(1, (int)math.ceil(-poison.ValueRO.TimeUntilNextTick / interval));
                poison.ValueRW.TimeUntilNextTick += tickCount * interval;
                health.ValueRW.Current -= poison.ValueRO.DamagePerTick * tickCount;

                if (health.ValueRO.Current > 0f)
                    continue;

                deaths.SetComponentEnabled(entity, true);
                earnedReward += enemy.ValueRO.KillReward;
                killCount++;
            }

            currency.ValueRW.Amount += earnedReward;
            currency.ValueRW.KillCount += killCount;
        }
    }
}
