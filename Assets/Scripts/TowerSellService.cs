using UnityEngine;

namespace Game
{
    /// <summary>Returns a selected tower's accumulated build investment and removes it from its tile.</summary>
    [RequireComponent(typeof(TowerEconomy))]
    public sealed class TowerSellService : MonoBehaviour
    {
        private TowerEconomy economy;
        private TowerSelectionController selectionController;

        private void Awake()
        {
            economy = GetComponent<TowerEconomy>();
        }

        public int GetSellValue(TowerInstance tower)
        {
            return economy == null ? 0 : economy.GetSellValue(tower);
        }

        public bool TrySellSelectedTower()
        {
            selectionController ??= GetComponent<TowerSelectionController>();
            if (selectionController == null)
                return false;

            var tower = selectionController.SelectedTower;
            if (tower == null || !economy.TrySellTower(tower, out _))
                return false;

            tower.CurrentTile?.ClearOccupant(tower);
            selectionController.ClearSelection();
            Destroy(tower.gameObject);
            GameAudioController.Play(GameSoundEffect.TowerSell);
            return true;
        }
    }
}
