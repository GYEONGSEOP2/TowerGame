using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.DOTS
{
    /// <summary>Follows slowed enemies with one blue status ring and clears its link when the status ends.</summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct SlowEffectSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var transforms = SystemAPI.GetComponentLookup<LocalTransform>(true);
            var enemies = SystemAPI.GetComponentLookup<Enemy>(true);
            var visuals = SystemAPI.GetComponentLookup<SlowEffectVisual>(true);
            var ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (effect, transform, entity) in SystemAPI
                         .Query<RefRW<SlowEffect>, RefRW<LocalTransform>>()
                         .WithEntityAccess())
            {
                var target = effect.ValueRO.Target;
                if (!state.EntityManager.Exists(target) ||
                    !transforms.HasComponent(target) ||
                    !enemies.HasComponent(target) ||
                    enemies[target].SlowRemainingTime <= 0f)
                {
                    ecb.DestroyEntity(entity);
                    if (state.EntityManager.Exists(target) &&
                        visuals.HasComponent(target) &&
                        visuals[target].Value == entity)
                    {
                        ecb.SetComponent(target, new SlowEffectVisual { Value = Entity.Null });
                    }
                    continue;
                }

                effect.ValueRW.Elapsed += deltaTime;
                var position = transforms[target].Position;
                position.z = -0.8f;
                var pulse = 0.52f + math.sin(effect.ValueRO.Elapsed * 8f) * 0.04f;
                transform.ValueRW = LocalTransform.FromPositionRotationScale(position, quaternion.identity, pulse);
            }
        }
    }
}
