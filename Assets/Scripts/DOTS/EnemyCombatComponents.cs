using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace Game.DOTS
{
    /// <summary>Hit points stored on every enemy entity.</summary>
    public struct EnemyHealth : IComponentData
    {
        public float Current;
        public float Max;
    }

    /// <summary>Supplies the vertical health fill amount to the shared enemy Entity Graphics material.</summary>
    [MaterialProperty("_HealthPercent")]
    public struct EnemyHealthFillProperty : IComponentData
    {
        public float Value;
    }

    /// <summary>Enabled when an enemy is reserved for destruction at the end of the current simulation frame.</summary>
    public struct EnemyDeadTag : IComponentData, IEnableableComponent
    {
    }

    /// <summary>Runtime data for a target-seeking projectile entity.</summary>
    public struct Projectile : IComponentData
    {
        public Entity Target;
        public float3 Position;
        public float Speed;
        public float Damage;
        public float HitRadius;
        public float ExplosionRadius;
        public float ExplosionDamage;
        public float SlowDuration;
        public float SlowMultiplier;
    }

    /// <summary>Short-lived visual feedback for a red tower explosion.</summary>
    public struct Explosion : IComponentData
    {
        public float3 Position;
        public float MaxRadius;
        public float Duration;
        public float Elapsed;
    }

    /// <summary>One-time area damage request carried by an explosion visual entity.</summary>
    public struct ExplosionDamage : IComponentData
    {
        public float3 Position;
        public float Radius;
        public float Damage;
        public bool Applied;
    }

    /// <summary>Provides the preconfigured Entity Graphics projectile prefab.</summary>
    public struct ProjectilePrefab : IComponentData
    {
        public Entity Value;
    }

    /// <summary>Provides the rendered explosion prefab used by red tower projectiles.</summary>
    public struct ExplosionPrefab : IComponentData
    {
        public Entity Value;
    }

    /// <summary>Links a slowed enemy to its single runtime status indicator.</summary>
    public struct SlowEffectVisual : IComponentData
    {
        public Entity Value;
    }

    /// <summary>Runtime visual that follows an enemy while its slow status is active.</summary>
    public struct SlowEffect : IComponentData
    {
        public Entity Target;
        public float Elapsed;
    }

    /// <summary>Provides the rendered blue slow-status indicator prefab.</summary>
    public struct SlowEffectPrefab : IComponentData
    {
        public Entity Value;
    }
}
