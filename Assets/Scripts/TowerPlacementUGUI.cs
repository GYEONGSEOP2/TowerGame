using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>Binds the scene-placed tower-create button to the placement controller.</summary>
    [RequireComponent(typeof(TowerPlacementController))]
    public sealed class TowerPlacementUGUI : MonoBehaviour
    {
        private void Awake()
        {
            var placementController = GetComponent<TowerPlacementController>();
            CreateTowerButtons(placementController);
        }

        private static void CreateTowerButtons(TowerPlacementController placementController)
        {
            var canvasTransform = GameUICanvas.GetOrCreate(placementController.transform);
            var button = canvasTransform?.Find("Create Tower Button")?.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError("TowerPlacementUGUI: Missing Create Tower Button in GameHUD prefab.", placementController);
                return;
            }

            button.onClick.AddListener(placementController.CreateTowerOnRandomEmptyTile);
        }
    }
}
