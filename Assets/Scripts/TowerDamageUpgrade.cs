using UnityEngine;

namespace Game
{
    /// <summary>Scales a tower's projectile damage from its merged rank.</summary>
    [RequireComponent(typeof(TowerAttack))]
    public sealed class TowerDamageUpgrade : MonoBehaviour
    {
        private TowerAttack towerAttack;
        private TowerInstance towerInstance;

        private void Awake()
        {
            towerAttack = GetComponent<TowerAttack>();
            towerInstance = GetComponent<TowerInstance>();
        }

        public void ApplyRank(TowerRank rank)
        {
            towerAttack.SetRank(rank);
            var definition = towerInstance == null ? null : towerInstance.Definition;
            var multiplierPerRank = definition == null ? 1.75f : definition.rankDamageMultiplier;
            towerAttack.SetRankDamageMultiplier(Mathf.Pow(multiplierPerRank, (int)rank));
        }
    }
}
