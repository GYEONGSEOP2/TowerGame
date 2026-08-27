using UnityEngine;

namespace Game.DOTS
{
    public enum EnemyShape
    {
        Triangle,
        Square,
        Pentagon,
        Hexagon,
        Circle
    }

    /// <summary>Defines the base combat stats shared by one enemy archetype.</summary>
    [CreateAssetMenu(menuName = "Game/Enemies/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [Min(0.01f)] public float moveSpeed = 2f;
        [Min(1f)] public float maxHealth = 100f;
        [Min(0)] public int killReward = 1;
        public EnemyShape shape = EnemyShape.Circle;
        [Min(0.1f)] public float visualScale = 1f;
        public Color typeColor = Color.white;
    }
}
