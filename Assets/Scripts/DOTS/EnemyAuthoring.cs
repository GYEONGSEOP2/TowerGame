using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.DOTS
{
    /// <summary>Add to the enemy prefab that will be instantiated by an EnemySpawnerAuthoring.</summary>
    public sealed class EnemyAuthoring : MonoBehaviour
    {
        [Tooltip("Base stats used by this enemy prefab. When unassigned, the fields below are used for compatibility.")]
        public EnemyDefinition definition;
        [Min(0.01f)] public float moveSpeed = 2f;
        [Min(1f)] public float maxHealth = 100f;
        [Min(0)] public int killReward = 1;

        private sealed class Baker : Baker<EnemyAuthoring>
        {
            public override void Bake(EnemyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var moveSpeed = authoring.definition == null ? authoring.moveSpeed : authoring.definition.moveSpeed;
                var maxHealth = authoring.definition == null ? authoring.maxHealth : authoring.definition.maxHealth;
                var killReward = authoring.definition == null ? authoring.killReward : authoring.definition.killReward;
                var shape = authoring.definition == null ? EnemyShape.Circle : authoring.definition.shape;
                var visualScale = authoring.definition == null ? 1f : authoring.definition.visualScale;
                var typeColor = authoring.definition == null ? Color.white : authoring.definition.typeColor.linear;
                AddComponent(entity, new Enemy
                {
                    MoveSpeed = moveSpeed,
                    CurrentWaypoint = 0,
                    KillReward = killReward,
                    SlowRemainingTime = 0f,
                    SlowMultiplier = 1f
                });
                AddComponent(entity, new EnemyBaseStats
                {
                    MoveSpeed = moveSpeed,
                    MaxHealth = maxHealth,
                    KillReward = killReward
                });
                AddComponent(entity, new EnemyVisual
                {
                    MeshIndex = (int)shape,
                    Scale = visualScale,
                    TypeColor = new float4(typeColor.r, typeColor.g, typeColor.b, typeColor.a)
                });
                AddComponent(entity, new EnemyMovementSegment());
                AddComponent(entity, new EnemyHealth
                {
                    Current = maxHealth,
                    Max = maxHealth
                });
                AddComponent<EnemyDeadTag>(entity);
                SetComponentEnabled<EnemyDeadTag>(entity, false);
                AddComponent(entity, new SlowEffectVisual
                {
                    Value = Entity.Null
                });
            }
        }
    }
}
