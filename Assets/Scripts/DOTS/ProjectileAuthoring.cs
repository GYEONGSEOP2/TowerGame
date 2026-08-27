using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace Game.DOTS
{
    /// <summary>Marks the static projectile visual prefab for Entity Graphics baking.</summary>
    public sealed class ProjectileAuthoring : MonoBehaviour
    {
        private sealed class Baker : Baker<ProjectileAuthoring>
        {
            public override void Bake(ProjectileAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Projectile());
                AddComponent(entity, new URPMaterialPropertyBaseColor
                {
                    Value = new float4(1f, 0.82f, 0.16f, 1f)
                });
            }
        }
    }
}
