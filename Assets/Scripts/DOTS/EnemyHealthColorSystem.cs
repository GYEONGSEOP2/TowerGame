using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace Game.DOTS
{
    /// <summary>Updates an enemy's Entity Graphics color when its health changes.</summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ProjectileMovementSystem))]
    [BurstCompile]
    public partial struct EnemyHealthColorSystem : ISystem
    {
        private const float BaseRenderZ = -0.5f;
        private const float LowHealthRenderZ = -0.55f;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (health, visual, baseColor, healthFill, transform) in SystemAPI
                         .Query<RefRO<EnemyHealth>, RefRO<EnemyVisual>, RefRW<URPMaterialPropertyBaseColor>, RefRW<EnemyHealthFillProperty>, RefRW<LocalTransform>>()
                         .WithChangeFilter<EnemyHealth>())
            {
                var healthPercent = health.ValueRO.Max <= 0f
                    ? 0f
                    : math.saturate(health.ValueRO.Current / health.ValueRO.Max);
                baseColor.ValueRW.Value = math.lerp(GetHealthColor(healthPercent), visual.ValueRO.TypeColor, 0.38f);
                healthFill.ValueRW.Value = healthPercent;
                var position = transform.ValueRO.Position;
                position.z = math.lerp(BaseRenderZ, LowHealthRenderZ, 1f - healthPercent);
                transform.ValueRW.Position = position;
            }
        }

        private static float4 GetHealthColor(float healthPercent)
        {
            if (healthPercent >= 0.7f)
                return new float4(0f, 0f, 1f, 1f);
            if (healthPercent >= 0.4f)
                return new float4(0f, 1f, 0f, 1f);
            if (healthPercent >= 0.15f)
                return new float4(1f, 0.21404114f, 0f, 1f);

            return new float4(1f, 0f, 0f, 1f);
        }
    }
}
