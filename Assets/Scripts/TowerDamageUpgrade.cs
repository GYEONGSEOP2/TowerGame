using UnityEngine;

namespace Game
{
    /// <summary>Scales a tower's projectile damage from its merged rank.</summary>
    [RequireComponent(typeof(TowerAttack))]
    public sealed class TowerDamageUpgrade : MonoBehaviour
    {
        [Min(1f)] public float damageMultiplierPerRank = 1.75f;

        private TowerAttack towerAttack;
        private void Awake()
        {
            towerAttack = GetComponent<TowerAttack>();
        }

        public void ApplyRank(TowerRank rank)
        {
            towerAttack.SetRank(rank);
            towerAttack.SetRankDamageMultiplier(Mathf.Pow(damageMultiplierPerRank, (int)rank));
        }
    }
}
