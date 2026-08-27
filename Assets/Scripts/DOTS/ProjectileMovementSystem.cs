using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.DOTS
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct ProjectileMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerCurrency>();
            state.RequireForUpdate<ExplosionPrefab>();
            state.RequireForUpdate<SlowEffectPrefab>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var targetTransforms = SystemAPI.GetComponentLookup<LocalTransform>(true);
            var targetHealth = SystemAPI.GetComponentLookup<EnemyHealth>();
            var targetEnemies = SystemAPI.GetComponentLookup<Enemy>();
            var targetDeaths = SystemAPI.GetComponentLookup<EnemyDeadTag>();
            var targetSlowEffects = SystemAPI.GetComponentLookup<SlowEffectVisual>();
            var ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var projectileCount = SystemAPI.QueryBuilder().WithAll<Projectile>().Build().CalculateEntityCount();
            var processedTargetCapacity = math.max(1, projectileCount);
            var slowEffectsAdded = new NativeParallelHashSet<Entity>(processedTargetCapacity, Allocator.Temp);
            var currency = SystemAPI.GetSingletonRW<PlayerCurrency>();
            var explosionPrefab = SystemAPI.GetSingleton<ExplosionPrefab>().Value;
            var slowEffectPrefab = SystemAPI.GetSingleton<SlowEffectPrefab>().Value;
            var earnedReward = 0;
            var killCount = 0;
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (projectile, projectileEntity) in SystemAPI.Query<RefRW<Projectile>>().WithEntityAccess())
            {
                var target = projectile.ValueRO.Target;
                if (!state.EntityManager.Exists(target) ||
                    !targetTransforms.HasComponent(target) ||
                    !targetHealth.HasComponent(target) ||
                    !targetEnemies.HasComponent(target) ||
                    !targetDeaths.HasComponent(target))
                {
                    ecb.DestroyEntity(projectileEntity);
                    continue;
                }

                if (targetDeaths.IsComponentEnabled(target))
                {
                    ecb.DestroyEntity(projectileEntity);
                    continue;
                }

                var start = projectile.ValueRO.Position;
                var targetPosition = targetTransforms[target].Position;
                targetPosition.z = start.z;
                var direction = math.normalizesafe(targetPosition - start);
                var end = start + direction * projectile.ValueRO.Speed * deltaTime;
                var segment = end - start;
                var segmentLengthSq = math.lengthsq(segment);
                var hitT = segmentLengthSq > 0.000001f
                    ? math.saturate(math.dot(targetPosition - start, segment) / segmentLengthSq)
                    : 0f;
                var closestPoint = start + segment * hitT;
                var hitRadiusSq = projectile.ValueRO.HitRadius * projectile.ValueRO.HitRadius;

                if (math.distancesq(closestPoint, targetPosition) > hitRadiusSq)
                {
                    projectile.ValueRW.Position = end;
                    state.EntityManager.SetComponentData(
                        projectileEntity,
                        LocalTransform.FromPosition(end));
                    continue;
                }

                var health = targetHealth[target];
                health.Current -= projectile.ValueRO.Damage;
                targetHealth[target] = health;

                if (projectile.ValueRO.SlowDuration > 0f)
                {
                    var enemy = targetEnemies[target];
                    enemy.SlowRemainingTime = math.max(enemy.SlowRemainingTime, projectile.ValueRO.SlowDuration);
                    enemy.SlowMultiplier = math.min(enemy.SlowMultiplier, projectile.ValueRO.SlowMultiplier);
                    targetEnemies[target] = enemy;

                    if (targetSlowEffects.HasComponent(target) &&
                        targetSlowEffects[target].Value == Entity.Null &&
                        slowEffectsAdded.Add(target))
                    {
                        var slowEffect = ecb.Instantiate(slowEffectPrefab);
                        ecb.SetComponent(slowEffect, new SlowEffect { Target = target, Elapsed = 0f });
                        ecb.SetComponent(slowEffect, LocalTransform.FromPositionRotationScale(
                            targetPosition,
                            quaternion.identity,
                            0.52f));
                        ecb.SetComponent(target, new SlowEffectVisual { Value = slowEffect });
                    }
                }

                if (health.Current <= 0f)
                {
                    targetDeaths.SetComponentEnabled(target, true);
                    earnedReward += targetEnemies[target].KillReward;
                    killCount++;
                }

                if (projectile.ValueRO.ExplosionRadius > 0f && projectile.ValueRO.ExplosionDamage > 0f)
                {
                    var position = targetPosition;
                    position.z = -0.8f;
                    var explosion = ecb.Instantiate(explosionPrefab);
                    ecb.SetComponent(explosion, new Explosion
                    {
                        Position = position,
                        MaxRadius = projectile.ValueRO.ExplosionRadius,
                        Duration = 0.18f,
                        Elapsed = 0f
                    });
                    ecb.SetComponent(explosion, new ExplosionDamage
                    {
                        Position = targetPosition,
                        Radius = projectile.ValueRO.ExplosionRadius,
                        Damage = projectile.ValueRO.ExplosionDamage,
                        Applied = false
                    });
                    ecb.SetComponent(explosion, LocalTransform.FromPositionRotationScale(
                        position,
                        quaternion.identity,
                        0f));
                }

                ecb.DestroyEntity(projectileEntity);
            }

            currency.ValueRW.Amount += earnedReward;
            currency.ValueRW.KillCount += killCount;

            slowEffectsAdded.Dispose();
        }
    }
}
