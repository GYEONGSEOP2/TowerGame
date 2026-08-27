using Unity.Entities;
using UnityEngine;

namespace Game.DOTS
{
    /// <summary>Marks the static explosion ring prefab for Entity Graphics baking.</summary>
    public sealed class ExplosionAuthoring : MonoBehaviour
    {
        private sealed class Baker : Baker<ExplosionAuthoring>
        {
            public override void Bake(ExplosionAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Explosion());
                AddComponent(entity, new ExplosionDamage());
            }
        }
    }
}
