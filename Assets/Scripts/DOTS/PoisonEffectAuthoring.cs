using Unity.Entities;
using UnityEngine;

namespace Game.DOTS
{
    /// <summary>Marks the static poison-status ring prefab for Entity Graphics baking.</summary>
    public sealed class PoisonEffectAuthoring : MonoBehaviour
    {
        private sealed class Baker : Baker<PoisonEffectAuthoring>
        {
            public override void Bake(PoisonEffectAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new PoisonEffect());
            }
        }
    }
}
