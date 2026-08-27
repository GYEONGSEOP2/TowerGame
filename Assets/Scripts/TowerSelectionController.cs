using System;
using UnityEngine;

namespace Game
{
    /// <summary>Tracks the selected tower and displays its world-space attack range.</summary>
    [RequireComponent(typeof(TowerPlacementController))]
    public sealed class TowerSelectionController : MonoBehaviour
    {
        private const int RangeSegments = 48;

        public TowerInstance SelectedTower { get; private set; }
        public event Action<TowerInstance> SelectionChanged;

        private LineRenderer rangeRenderer;
        private Material rangeMaterial;

        private void Awake()
        {
            CreateRangeIndicator();
            if (GetComponent<TowerSelectionUGUI>() == null)
                gameObject.AddComponent<TowerSelectionUGUI>();
        }

        private void Update()
        {
            if (SelectedTower == null)
            {
                if (rangeRenderer.gameObject.activeSelf)
                    ClearSelection();
                return;
            }

            UpdateRangeIndicator();
        }

        public void SelectTower(TowerInstance tower)
        {
            if (tower == null)
            {
                ClearSelection();
                return;
            }

            SelectedTower = tower;
            UpdateRangeIndicator();
            SelectionChanged?.Invoke(SelectedTower);
        }

        public void ClearSelection()
        {
            if (SelectedTower == null && !rangeRenderer.gameObject.activeSelf)
                return;

            SelectedTower = null;
            rangeRenderer.gameObject.SetActive(false);
            SelectionChanged?.Invoke(null);
        }

        private void CreateRangeIndicator()
        {
            var rangeObject = new GameObject("Tower Attack Range");
            rangeObject.transform.SetParent(transform, false);
            rangeRenderer = rangeObject.AddComponent<LineRenderer>();
            rangeRenderer.useWorldSpace = false;
            rangeRenderer.loop = true;
            rangeRenderer.positionCount = RangeSegments;
            rangeRenderer.widthMultiplier = 0.035f;
            rangeRenderer.numCornerVertices = 2;
            rangeRenderer.numCapVertices = 2;
            rangeRenderer.sortingOrder = 15;

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            rangeMaterial = new Material(shader);
            rangeRenderer.material = rangeMaterial;
            rangeObject.SetActive(false);
        }

        private void UpdateRangeIndicator()
        {
            var attack = SelectedTower.GetComponent<TowerAttack>();
            if (attack == null)
            {
                ClearSelection();
                return;
            }

            var position = SelectedTower.transform.position;
            position.z = -0.12f;
            rangeRenderer.transform.position = position;
            var color = SelectedTower.Definition.displayColor;
            color.a = 0.8f;
            rangeRenderer.startColor = color;
            rangeRenderer.endColor = color;

            for (var index = 0; index < RangeSegments; index++)
            {
                var angle = index * Mathf.PI * 2f / RangeSegments;
                rangeRenderer.SetPosition(index, new Vector3(
                    Mathf.Cos(angle) * attack.attackRange,
                    Mathf.Sin(angle) * attack.attackRange,
                    0f));
            }

            if (!rangeRenderer.gameObject.activeSelf)
                rangeRenderer.gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            if (rangeMaterial != null)
                Destroy(rangeMaterial);
        }
    }
}
