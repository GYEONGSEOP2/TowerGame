using UnityEngine;

namespace Game
{
    /// <summary>
    /// Draws a simple top-down square-loop map using Unity's built-in white sprite.
    /// Attach this to a regular GameObject in the main scene at the same position as the DOTS spawner.
    /// </summary>
    [ExecuteAlways]
    public sealed class SimpleSquareMapRenderer : MonoBehaviour
    {
        [Min(1f)] public float routeWidth = 14f;
        [Min(1f)] public float routeHeight = 8f;
        [Min(0.1f)] public float pathThickness = 0.8f;
        [Min(1)] public int towerGridColumns = 6;
        [Min(1)] public int towerGridRows = 3;
        [Min(0f)] public float towerAreaMargin = 0.3f;
        [Range(0f, 0.2f)] public float towerTileGap = 0.08f;
        public Color groundColor = new(0.12f, 0.25f, 0.16f);
        public Color pathColor = new(0.72f, 0.62f, 0.38f);
        public Color towerAreaColor = new(0.10f, 0.16f, 0.22f);
        public Color towerTileColor = new(0.32f, 0.36f, 0.42f);

        private const string GeneratedNamePrefix = "__Map_";
        private const int CurrentLayoutVersion = 1;
        [SerializeField] private int layoutVersion;

        private void OnEnable()
        {
            ApplyLayoutDefaults();
            Rebuild();
        }

        private void OnValidate()
        {
            ApplyLayoutDefaults();
            if (isActiveAndEnabled)
                Rebuild();
        }

        private void ApplyLayoutDefaults()
        {
            if (layoutVersion >= CurrentLayoutVersion)
                return;

            towerGridColumns = 6;
            towerGridRows = 3;
            towerAreaMargin = 0.3f;
            layoutVersion = CurrentLayoutVersion;
        }

        private void OnDestroy()
        {
            ClearGenerated();
        }

        private void Rebuild()
        {
            ClearGenerated();
            CreateRectangle("Ground", Vector2.zero, new Vector2(routeWidth + 3f, routeHeight + 3f), groundColor, -1);
            CreateRectangle("Top", new Vector2(0f, routeHeight * 0.5f), new Vector2(routeWidth + pathThickness, pathThickness), pathColor, 0);
            CreateRectangle("Right", new Vector2(routeWidth * 0.5f, 0f), new Vector2(pathThickness, routeHeight + pathThickness), pathColor, 0);
            CreateRectangle("Bottom", new Vector2(0f, -routeHeight * 0.5f), new Vector2(routeWidth + pathThickness, pathThickness), pathColor, 0);
            CreateRectangle("Left", new Vector2(-routeWidth * 0.5f, 0f), new Vector2(pathThickness, routeHeight + pathThickness), pathColor, 0);
            CreateTowerGrid();
        }

        private void CreateTowerGrid()
        {
            var availableWidth = routeWidth - pathThickness * 2f - towerAreaMargin * 2f;
            var availableHeight = routeHeight - pathThickness * 2f - towerAreaMargin * 2f;
            var tileSize = Mathf.Max(0.1f, Mathf.Min(
                (availableWidth - (towerGridColumns + 1) * towerTileGap) / towerGridColumns,
                (availableHeight - (towerGridRows + 1) * towerTileGap) / towerGridRows));

            var paddedWidth = towerGridColumns * tileSize + (towerGridColumns + 1) * towerTileGap;
            var paddedHeight = towerGridRows * tileSize + (towerGridRows + 1) * towerTileGap;
            CreateRectangle("TowerArea", Vector2.zero, new Vector2(paddedWidth, paddedHeight), towerAreaColor, 1);

            var startX = -(towerGridColumns - 1) * (tileSize + towerTileGap) * 0.5f;
            var startY = -(towerGridRows - 1) * (tileSize + towerTileGap) * 0.5f;
            for (var row = 0; row < towerGridRows; row++)
            {
                for (var column = 0; column < towerGridColumns; column++)
                {
                    var position = new Vector2(
                        startX + column * (tileSize + towerTileGap),
                        startY + row * (tileSize + towerTileGap));
                    var tile = CreateRectangle($"TowerTile_{row}_{column}", position, Vector2.one * tileSize, towerTileColor, 2);
                    tile.AddComponent<TowerTile>().Initialize(new Vector2Int(column, row));
                    tile.AddComponent<BoxCollider2D>().isTrigger = true;
                }
            }
        }

        private GameObject CreateRectangle(string label, Vector2 localPosition, Vector2 size, Color color, int sortingOrder)
        {
            var tile = new GameObject(GeneratedNamePrefix + label);
            tile.transform.SetParent(transform, false);
            tile.transform.localPosition = localPosition;
            tile.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = tile.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return tile;
        }

        private void ClearGenerated()
        {
            for (var index = transform.childCount - 1; index >= 0; index--)
            {
                var child = transform.GetChild(index);
                if (child.name.StartsWith(GeneratedNamePrefix))
                {
                    if (Application.isPlaying)
                        Destroy(child.gameObject);
                    else
                        DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}
