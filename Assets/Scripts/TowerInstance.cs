using UnityEngine;

namespace Game
{
    public enum TowerRank
    {
        Triangle,
        Square,
        Pentagon,
        Hexagon,
        Circle
    }

    /// <summary>State and visual for a draggable tower placed on a TowerTile.</summary>
    public sealed class TowerInstance : MonoBehaviour
    {
        public TowerRank Rank { get; private set; }
        public TowerDefinition Definition { get; private set; }
        public TowerTile CurrentTile { get; private set; }
        public Sprite Icon => spriteRenderer == null ? null : spriteRenderer.sprite;
        public int InvestedAmount { get; private set; }

        private TowerPlacementController controller;
        private SpriteRenderer spriteRenderer;
        private BoxCollider2D hitbox;
        private TowerDamageUpgrade damageUpgrade;

        public void Initialize(
            TowerPlacementController placementController,
            TowerTile tile,
            TowerRank rank,
            TowerDefinition towerDefinition,
            int investedAmount)
        {
            controller = placementController;
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.color = towerDefinition.displayColor;
            spriteRenderer.sortingOrder = 10;
            hitbox = gameObject.AddComponent<BoxCollider2D>();
            var towerAttack = gameObject.AddComponent<TowerAttack>();
            damageUpgrade = gameObject.AddComponent<TowerDamageUpgrade>();
            Rank = rank;
            Definition = towerDefinition;
            InvestedAmount = Mathf.Max(0, investedAmount);
            towerAttack.Configure(Definition);
            damageUpgrade.ApplyRank(Rank);
            SetSprite(controller.GetSprite(rank));
            MoveToTile(tile);
        }

        public bool TryUpgrade(int additionalInvestment)
        {
            if (Rank == TowerRank.Circle)
                return false;

            var nextRank = Rank + 1;
            var nextSprite = controller.GetSprite(nextRank);
            if (nextSprite == null)
            {
                Debug.LogWarning($"TowerPlacementController: {nextRank} Sprite를 지정하세요.", controller);
                return false;
            }

            Rank = nextRank;
            InvestedAmount += Mathf.Max(0, additionalInvestment);
            damageUpgrade.ApplyRank(Rank);
            SetSprite(nextSprite);
            return true;
        }

        public void MoveToTile(TowerTile tile)
        {
            if (CurrentTile != null)
                CurrentTile.ClearOccupant(this);

            CurrentTile = tile;
            CurrentTile.SetOccupant(this);
            transform.SetParent(tile.transform, false);
            transform.localPosition = new Vector3(0f, 0f, -0.1f);
            FitToTile();
        }

        private void SetSprite(Sprite sprite)
        {
            spriteRenderer.sprite = sprite;
            hitbox.size = sprite.bounds.size;

            FitToTile();
        }

        private void FitToTile()
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null)
                return;

            var tileSprite = CurrentTile == null ? null : CurrentTile.GetComponent<SpriteRenderer>().sprite;
            if (tileSprite != null)
            {
                var scale = tileSprite.bounds.size.x * 0.68f / spriteRenderer.sprite.bounds.size.x;
                transform.localScale = Vector3.one * scale;
            }
        }
    }
}
