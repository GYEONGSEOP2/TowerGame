using Unity.Entities;
using UnityEngine;

namespace Game.DOTS
{
    /// <summary>Marks the static slow-status ring prefab for Entity Graphics baking.</summary>
    public sealed class SlowEffectAuthoring : MonoBehaviour
    {
        private sealed class Baker : Baker<SlowEffectAuthoring>
        {
            public override void Bake(SlowEffectAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new SlowEffect());
            }
        }
    }
}
