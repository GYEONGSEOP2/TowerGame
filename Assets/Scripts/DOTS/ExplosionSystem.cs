using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.DOTS
{
    /// <summary>Expands and removes short-lived explosion indicator entities.</summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct ExplosionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (explosion, transform, entity) in SystemAPI
                         .Query<RefRW<Explosion>, RefRW<LocalTransform>>()
                         .WithEntityAccess())
            {
                explosion.ValueRW.Elapsed += deltaTime;
                var progress = explosion.ValueRO.Duration <= 0f
                    ? 1f
                    : explosion.ValueRO.Elapsed / explosion.ValueRO.Duration;
                transform.ValueRW = LocalTransform.FromPositionRotationScale(
                    explosion.ValueRO.Position,
                    quaternion.identity,
                    explosion.ValueRO.MaxRadius * progress);

                if (progress >= 1f)
                    ecb.DestroyEntity(entity);
            }
        }
    }
}
