using UnityEngine;

namespace Game
{
    public enum TowerType
    {
        Red,
        Blue,
        Purple
    }

    /// <summary>Configures a color tower's combat identity independently from its shape rank.</summary>
    [CreateAssetMenu(menuName = "Game/Towers/Tower Definition")]
    public sealed class TowerDefinition : ScriptableObject
    {
        public TowerType towerType;
        public Color displayColor = Color.white;

        [Header("Base Combat")]
        [Min(0f)] public float baseDamage = 20f;
        [Min(0.01f)] public float baseFireInterval = 0.5f;
        [Min(0.1f)] public float baseAttackRange = 3f;
        [Min(0.1f)] public float projectileSpeed = 12f;
        [Min(0.01f)] public float projectileHitRadius = 0.2f;

        [Header("Type Modifiers")]
        [Min(0.01f)] public float damageMultiplier = 1f;
        [Min(0.01f)] public float attackSpeedMultiplier = 1f;
        [Min(0.01f)] public float rangeMultiplier = 1f;
        [Min(1)] public int baseProjectileCount = 1;
        [Min(1f)] public float rankDamageMultiplier = 1.75f;

        [Header("Special Effects")]
        [Min(0f)] public float explosionRadius;
        [Min(0f)] public float explosionDamage;
        [Min(0f)] public float slowDuration;
        [Range(0.05f, 1f)] public float slowMultiplier = 1f;
    }
}
