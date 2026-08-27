using Unity.Burst;
using Unity.Entities;

namespace Game.DOTS
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ExplosionDamageSystem))]
    [BurstCompile]
    public partial struct EnemyDeathSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (_, enemy) in SystemAPI.Query<RefRO<EnemyDeadTag>>().WithEntityAccess())
                ecb.DestroyEntity(enemy);
        }
    }
}
