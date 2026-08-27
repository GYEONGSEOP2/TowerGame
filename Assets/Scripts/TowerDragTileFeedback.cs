using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public enum TowerDragDropStatus
    {
        None,
        Move,
        Merge,
        Invalid
    }

    /// <summary>Temporarily colors build tiles to show move, merge, and invalid drop targets while dragging.</summary>
    public sealed class TowerDragTileFeedback : MonoBehaviour
    {
        private static readonly Color MoveColor = new(0.22f, 0.7f, 0.88f, 1f);
        private static readonly Color MergeColor = new(0.28f, 0.9f, 0.4f, 1f);
        private static readonly Color InvalidColor = new(0.78f, 0.25f, 0.28f, 1f);
        private static readonly Color HoverMultiplier = new(1.2f, 1.2f, 1.2f, 1f);

        private readonly Dictionary<TowerTile, Color> originalColors = new();
        private readonly Dictionary<TowerTile, Color> targetColors = new();
        private TowerTile hoveredTile;
        private TowerInstance draggedTower;

        public void Show(TowerInstance draggedTower)
        {
            Clear();
            if (draggedTower == null)
                return;

            this.draggedTower = draggedTower;

            foreach (var tile in FindObjectsByType<TowerTile>())
            {
                var renderer = tile.GetComponent<SpriteRenderer>();
                if (renderer == null)
                    continue;

                originalColors.Add(tile, renderer.color);
                if (tile == draggedTower.CurrentTile)
                    continue;

                var targetColor = GetColor(GetDropStatus(tile));
                targetColors.Add(tile, targetColor);
                renderer.color = targetColor;
            }
        }

        public void SetHoveredTile(TowerTile tile)
        {
            if (hoveredTile == tile)
                return;

            RestoreTargetColor(hoveredTile);
            hoveredTile = tile;
            if (hoveredTile != null && targetColors.TryGetValue(hoveredTile, out var color))
                SetColor(hoveredTile, color * HoverMultiplier);
        }

        public void Clear()
        {
            foreach (var (tile, color) in originalColors)
                SetColor(tile, color);

            originalColors.Clear();
            targetColors.Clear();
            hoveredTile = null;
            draggedTower = null;
        }

        public TowerDragDropStatus GetDropStatus(TowerTile tile)
        {
            if (draggedTower == null || tile == null || tile == draggedTower.CurrentTile)
                return TowerDragDropStatus.None;
            if (!tile.IsOccupied)
                return TowerDragDropStatus.Move;

            return CanMerge(draggedTower, tile.Occupant)
                ? TowerDragDropStatus.Merge
                : TowerDragDropStatus.Invalid;
        }

        private static bool CanMerge(TowerInstance draggedTower, TowerInstance targetTower)
        {
            return targetTower != null &&
                   targetTower.Rank == draggedTower.Rank &&
                   targetTower.Rank != TowerRank.Circle &&
                   targetTower.Definition == draggedTower.Definition;
        }

        private static Color GetColor(TowerDragDropStatus status)
        {
            return status switch
            {
                TowerDragDropStatus.Move => MoveColor,
                TowerDragDropStatus.Merge => MergeColor,
                TowerDragDropStatus.Invalid => InvalidColor,
                _ => Color.clear
            };
        }

        private void RestoreTargetColor(TowerTile tile)
        {
            if (tile != null && targetColors.TryGetValue(tile, out var color))
                SetColor(tile, color);
        }

        private static void SetColor(TowerTile tile, Color color)
        {
            if (tile != null && tile.TryGetComponent<SpriteRenderer>(out var renderer))
                renderer.color = color;
        }
    }
}
