using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.DOTS
{
    /// <summary>Follows poisoned enemies with one green status ring and clears its link when poison ends.</summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PoisonDamageSystem))]
    [BurstCompile]
    public partial struct PoisonEffectSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var transforms = SystemAPI.GetComponentLookup<LocalTransform>(true);
            var poisons = SystemAPI.GetComponentLookup<EnemyPoison>(true);
            var visuals = SystemAPI.GetComponentLookup<PoisonEffectVisual>(true);
            var ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (effect, transform, entity) in SystemAPI
                         .Query<RefRW<PoisonEffect>, RefRW<LocalTransform>>()
                         .WithEntityAccess())
            {
                var target = effect.ValueRO.Target;
                if (!state.EntityManager.Exists(target) ||
                    !transforms.HasComponent(target) ||
                    !poisons.HasComponent(target) ||
                    poisons[target].RemainingDuration <= 0f)
                {
                    ecb.DestroyEntity(entity);
                    if (state.EntityManager.Exists(target) &&
                        visuals.HasComponent(target) &&
                        visuals[target].Value == entity)
                    {
                        ecb.SetComponent(target, new PoisonEffectVisual { Value = Entity.Null });
                    }
                    continue;
                }

                effect.ValueRW.Elapsed += deltaTime;
                var position = transforms[target].Position;
                position.z = -0.82f;
                var pulse = 0.6f + math.sin(effect.ValueRO.Elapsed * 9f) * 0.06f;
                transform.ValueRW = LocalTransform.FromPositionRotationScale(position, quaternion.identity, pulse);
            }
        }
    }
}
