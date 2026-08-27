using Unity.Collections;
using Unity.Entities;

namespace Game.DOTS
{
    /// <summary>Consumes explosion damage requests through the shared enemy spatial grid.</summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ProjectileMovementSystem))]
    public partial class ExplosionDamageSystem : SystemBase
    {
        private EntityQuery explosionQuery;
        private EntityQuery currencyQuery;
        private EnemySpatialGridSystem enemySpatialGridSystem;

        protected override void OnCreate()
        {
            explosionQuery = GetEntityQuery(ComponentType.ReadWrite<ExplosionDamage>());
            currencyQuery = GetEntityQuery(ComponentType.ReadWrite<PlayerCurrency>());
            enemySpatialGridSystem = World.GetExistingSystemManaged<EnemySpatialGridSystem>();
        }

        protected override void OnUpdate()
        {
            if (enemySpatialGridSystem == null)
                enemySpatialGridSystem = World.GetExistingSystemManaged<EnemySpatialGridSystem>();
            if (enemySpatialGridSystem == null || !enemySpatialGridSystem.IsReady ||
                currencyQuery.IsEmptyIgnoreFilter)
                return;

            var earnedReward = 0;
            var killCount = 0;
            using var explosions = explosionQuery.ToEntityArray(Allocator.Temp);

            foreach (var explosionEntity in explosions)
            {
                var explosion = EntityManager.GetComponentData<ExplosionDamage>(explosionEntity);
                if (explosion.Applied)
                    continue;

                enemySpatialGridSystem.ApplyDamageInRadius(
                    explosion.Position,
                    explosion.Radius,
                    explosion.Damage,
                    out var earned,
                    out var kills);
                earnedReward += earned;
                killCount += kills;
                explosion.Applied = true;
                EntityManager.SetComponentData(explosionEntity, explosion);
            }

            if (earnedReward == 0 && killCount == 0)
                return;

            var currencyEntity = currencyQuery.GetSingletonEntity();
            var currency = EntityManager.GetComponentData<PlayerCurrency>(currencyEntity);
            currency.Amount += earnedReward;
            currency.KillCount += killCount;
            EntityManager.SetComponentData(currencyEntity, currency);
        }
    }
}
