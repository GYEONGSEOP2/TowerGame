using Unity.Entities;
using UnityEngine;

namespace Game.DOTS
{
    /// <summary>Provides baked combat visual prefabs for towers inside the GamePlay SubScene.</summary>
    public sealed class TowerCombatVisualsAuthoring : MonoBehaviour
    {
        public GameObject projectilePrefab;
        public GameObject explosionPrefab;
        public GameObject slowEffectPrefab;

        private sealed class Baker : Baker<TowerCombatVisualsAuthoring>
        {
            public override void Bake(TowerCombatVisualsAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddPrefabReference(entity, authoring.projectilePrefab, AddProjectilePrefab);
                AddPrefabReference(entity, authoring.explosionPrefab, AddExplosionPrefab);
                AddPrefabReference(entity, authoring.slowEffectPrefab, AddSlowEffectPrefab);
            }

            private void AddPrefabReference(Entity entity, GameObject prefab, System.Action<Entity, Entity> addComponent)
            {
                if (prefab == null)
                    return;

                addComponent(entity, GetEntity(prefab, TransformUsageFlags.Dynamic));
            }

            private void AddProjectilePrefab(Entity entity, Entity prefab) => AddComponent(entity, new ProjectilePrefab { Value = prefab });
            private void AddExplosionPrefab(Entity entity, Entity prefab) => AddComponent(entity, new ExplosionPrefab { Value = prefab });
            private void AddSlowEffectPrefab(Entity entity, Entity prefab) => AddComponent(entity, new SlowEffectPrefab { Value = prefab });
        }
    }
}
