using UnityEngine;

namespace Game
{
    /// <summary>Shows a temporary visual while the placed tower remains on its source tile.</summary>
    public sealed class TowerDragPreview : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Color baseColor;

        public static TowerDragPreview Create()
        {
            var previewObject = new GameObject("Tower Drag Preview");
            var preview = previewObject.AddComponent<TowerDragPreview>();
            preview.spriteRenderer = previewObject.AddComponent<SpriteRenderer>();
            previewObject.SetActive(false);
            return preview;
        }

        public void Show(TowerInstance source)
        {
            var sourceRenderer = source.GetComponent<SpriteRenderer>();
            if (sourceRenderer == null)
                return;

            spriteRenderer.sprite = sourceRenderer.sprite;
            var sourceColor = sourceRenderer.color;
            baseColor = new Color(sourceColor.r, sourceColor.g, sourceColor.b, 0.65f);
            spriteRenderer.color = baseColor;
            spriteRenderer.sortingOrder = sourceRenderer.sortingOrder + 10;
            transform.localScale = source.transform.lossyScale;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }

        public void SetDropStatus(TowerDragDropStatus status)
        {
            spriteRenderer.color = baseColor;
        }
    }
}
