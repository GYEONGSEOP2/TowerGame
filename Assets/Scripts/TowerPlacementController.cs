using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    /// <summary>Places, drags, and merges triangle tower instances on map tiles.</summary>
    public sealed class TowerPlacementController : MonoBehaviour
    {
        public Sprite triangleTowerSprite;
        public Sprite squareTowerSprite;
        public Sprite pentagonTowerSprite;
        public Sprite hexagonTowerSprite;
        public Sprite circleTowerSprite;
        public TowerDefinition redTowerDefinition;
        public TowerDefinition blueTowerDefinition;
        public TowerDefinition purpleTowerDefinition;

        private TowerInstance draggingTower;
        private TowerInstance pressedTower;
        private TowerDragPreview dragPreview;
        private TowerDragTileFeedback dragTileFeedback;
        private TowerMergePreviewUGUI mergePreview;
        private TowerEconomy economy;
        private TowerSelectionController selectionController;
        private Vector2 pressScreenPosition;
        private bool hasStartedDrag;

        private const float DragStartDistancePixels = 12f;

        private void Awake()
        {
            GameAudioController.EnsureInstance();

            if (GetComponent<MobileRuntimeSettings>() == null)
                gameObject.AddComponent<MobileRuntimeSettings>();

            economy = GetComponent<TowerEconomy>();
            if (economy == null)
                economy = gameObject.AddComponent<TowerEconomy>();

            if (GetComponent<TowerPlacementUGUI>() == null)
                gameObject.AddComponent<TowerPlacementUGUI>();

            if (GetComponent<EnemyCountUGUI>() == null)
                gameObject.AddComponent<EnemyCountUGUI>();

            if (GetComponent<KillCountUGUI>() == null)
                gameObject.AddComponent<KillCountUGUI>();

            if (GetComponent<MoneyCountUGUI>() == null)
                gameObject.AddComponent<MoneyCountUGUI>();

            if (GetComponent<DifficultyUGUI>() == null)
                gameObject.AddComponent<DifficultyUGUI>();

            if (GetComponent<FrameRateUGUI>() == null)
                gameObject.AddComponent<FrameRateUGUI>();

            if (GetComponent<TowerSellService>() == null)
                gameObject.AddComponent<TowerSellService>();

            selectionController = GetComponent<TowerSelectionController>();
            if (selectionController == null)
                selectionController = gameObject.AddComponent<TowerSelectionController>();

            dragPreview = TowerDragPreview.Create();
            dragTileFeedback = GetComponent<TowerDragTileFeedback>();
            if (dragTileFeedback == null)
                dragTileFeedback = gameObject.AddComponent<TowerDragTileFeedback>();

            mergePreview = GetComponent<TowerMergePreviewUGUI>();
            if (mergePreview == null)
                mergePreview = gameObject.AddComponent<TowerMergePreviewUGUI>();
        }

        private void Update()
        {
            if (!TryReadPointer(
                    out var screenPosition,
                    out var wasPressedThisFrame,
                    out var wasReleasedThisFrame,
                    out var isPressed))
                return;

            if (wasPressedThisFrame)
            {
                pressedTower = GetTowerAtPointer(screenPosition);
                pressScreenPosition = screenPosition;
                hasStartedDrag = false;
            }

            if (pressedTower == null)
                return;

            if (isPressed && !hasStartedDrag &&
                (screenPosition - pressScreenPosition).sqrMagnitude >= DragStartDistancePixels * DragStartDistancePixels)
            {
                BeginDrag(pressedTower);
                hasStartedDrag = true;
            }

            if (draggingTower != null && isPressed)
                FollowPointer(screenPosition);

            if (wasReleasedThisFrame)
            {
                if (draggingTower != null)
                    EndDragAtPointer(screenPosition);
                else
                    selectionController.SelectTower(pressedTower);

                pressedTower = null;
                hasStartedDrag = false;
            }
        }

        public Sprite GetSprite(TowerRank rank)
        {
            return rank switch
            {
                TowerRank.Triangle => triangleTowerSprite,
                TowerRank.Square => squareTowerSprite,
                TowerRank.Pentagon => pentagonTowerSprite,
                TowerRank.Hexagon => hexagonTowerSprite,
                TowerRank.Circle => circleTowerSprite,
                _ => null
            };
        }

        public void CreateTowerOnRandomEmptyTile()
        {
            var definition = GetRandomTowerDefinition();

            CreateTowerOnRandomEmptyTile(definition);
        }

        private TowerDefinition GetRandomTowerDefinition()
        {
            var definitionCount = (redTowerDefinition != null ? 1 : 0) +
                                  (blueTowerDefinition != null ? 1 : 0) +
                                  (purpleTowerDefinition != null ? 1 : 0);
            if (definitionCount == 0)
                return null;

            var selectedIndex = Random.Range(0, definitionCount);
            if (redTowerDefinition != null && selectedIndex-- == 0)
                return redTowerDefinition;
            if (blueTowerDefinition != null && selectedIndex-- == 0)
                return blueTowerDefinition;

            return purpleTowerDefinition;
        }

        private void CreateTowerOnRandomEmptyTile(TowerDefinition definition)
        {
            if (GetSprite(TowerRank.Triangle) == null)
            {
                Debug.LogWarning("TowerPlacementController: Assign the Triangle Tower Sprite.", this);
                return;
            }

            if (definition == null)
            {
                Debug.LogWarning("TowerPlacementController: Assign a Tower Definition.", this);
                return;
            }

            var emptyTiles = new List<TowerTile>();
            foreach (var tile in FindObjectsByType<TowerTile>())
            {
                if (!tile.IsOccupied)
                    emptyTiles.Add(tile);
            }

            if (emptyTiles.Count == 0)
                return;

            if (!economy.TrySpendTowerCost())
            {
                GameAudioController.Play(GameSoundEffect.UiError);
                return;
            }

            var selectedTile = emptyTiles[Random.Range(0, emptyTiles.Count)];
            CreateTower(selectedTile, TowerRank.Triangle, definition, economy.TowerCost);
            GameAudioController.Play(GameSoundEffect.TowerCreate);
        }

        private void CreateTower(TowerTile tile, TowerRank rank, TowerDefinition definition, int investedAmount)
        {
            var tower = new GameObject($"{definition.towerType} {rank} Tower");
            tower.AddComponent<TowerInstance>().Initialize(this, tile, rank, definition, investedAmount);
        }

        private TowerInstance GetTowerAtPointer(Vector2 screenPosition)
        {
            foreach (var hit in Physics2D.OverlapPointAll(GetPointerWorldPosition(screenPosition)))
            {
                var tower = hit.GetComponent<TowerInstance>();
                if (tower == null)
                    continue;

                return tower;
            }

            return null;
        }

        private void BeginDrag(TowerInstance tower)
        {
            draggingTower = tower;
            dragPreview.Show(draggingTower);
            dragTileFeedback.Show(draggingTower);
            mergePreview.Hide();
            selectionController.ClearSelection();
        }

        private void FollowPointer(Vector2 screenPosition)
        {
            var position = GetPointerWorldPosition(screenPosition);
            position.z = -0.1f;
            dragPreview.SetPosition(position);
            var targetTile = GetTileAtPointer(screenPosition);
            dragTileFeedback.SetHoveredTile(targetTile);
            dragPreview.SetDropStatus(dragTileFeedback.GetDropStatus(targetTile));
            if (dragTileFeedback.GetDropStatus(targetTile) == TowerDragDropStatus.Merge)
                mergePreview.Show(targetTile.Occupant, screenPosition);
            else
                mergePreview.Hide();
        }

        private void EndDragAtPointer(Vector2 screenPosition)
        {
            var targetTile = GetTileAtPointer(screenPosition);
            var sourceTile = draggingTower.CurrentTile;

            dragPreview.Hide();
            dragTileFeedback.Clear();
            mergePreview.Hide();

            if (targetTile == null || targetTile == sourceTile)
            {
                draggingTower.MoveToTile(sourceTile);
            }
            else if (!targetTile.IsOccupied)
            {
                draggingTower.MoveToTile(targetTile);
            }
            else if (targetTile.Occupant.Rank == draggingTower.Rank &&
                     targetTile.Occupant.Definition == draggingTower.Definition &&
                     targetTile.Occupant.TryUpgrade(draggingTower.InvestedAmount))
            {
                sourceTile.ClearOccupant(draggingTower);
                Destroy(draggingTower.gameObject);
                GameAudioController.Play(GameSoundEffect.TowerMerge);
            }
            else
            {
                draggingTower.MoveToTile(sourceTile);
            }

            draggingTower = null;
        }

        private void OnDestroy()
        {
            if (dragPreview != null)
                Destroy(dragPreview.gameObject);
            if (dragTileFeedback != null)
                dragTileFeedback.Clear();
            if (mergePreview != null)
                mergePreview.Hide();
        }

        private TowerTile GetTileAtPointer(Vector2 screenPosition)
        {
            foreach (var hit in Physics2D.OverlapPointAll(GetPointerWorldPosition(screenPosition)))
            {
                var tile = hit.GetComponent<TowerTile>();
                if (tile != null)
                    return tile;
            }

            return null;
        }

        private static bool TryReadPointer(
            out Vector2 screenPosition,
            out bool wasPressedThisFrame,
            out bool wasReleasedThisFrame,
            out bool isPressed)
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var touch = touchscreen.primaryTouch;
                if (touch.press.isPressed || touch.press.wasPressedThisFrame || touch.press.wasReleasedThisFrame)
                {
                    screenPosition = touch.position.ReadValue();
                    wasPressedThisFrame = touch.press.wasPressedThisFrame;
                    wasReleasedThisFrame = touch.press.wasReleasedThisFrame;
                    isPressed = touch.press.isPressed;
                    return true;
                }
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                screenPosition = mouse.position.ReadValue();
                wasPressedThisFrame = mouse.leftButton.wasPressedThisFrame;
                wasReleasedThisFrame = mouse.leftButton.wasReleasedThisFrame;
                isPressed = mouse.leftButton.isPressed;
                return true;
            }

            screenPosition = default;
            wasPressedThisFrame = false;
            wasReleasedThisFrame = false;
            isPressed = false;
            return false;
        }

        private static Vector3 GetPointerWorldPosition(Vector2 screenPosition)
        {
            var camera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
            if (camera == null)
                return Vector3.zero;

            var pointerPosition = camera.ScreenToWorldPoint(screenPosition);
            return new Vector3(pointerPosition.x, pointerPosition.y, 0f);
        }
    }
}
