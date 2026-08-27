using Game.DOTS;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>Displays the number of active enemy entities in a lightweight UGUI label.</summary>
    [RequireComponent(typeof(TowerPlacementController))]
    public sealed class EnemyCountUGUI : MonoBehaviour
    {
        private const float RefreshInterval = 0.1f;

        private EntityQuery enemyQuery;
        private Text label;
        private float nextRefreshTime;
        private bool hasEnemyQuery;

        private void Awake()
        {
            CreateLabel();
        }

        private void Update()
        {
            if (!TryInitializeQuery() || Time.unscaledTime < nextRefreshTime)
                return;

            label.text = $"Enemies: {enemyQuery.CalculateEntityCount():N0}";
            nextRefreshTime = Time.unscaledTime + RefreshInterval;
        }

        private bool TryInitializeQuery()
        {
            if (hasEnemyQuery)
                return true;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            enemyQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<Enemy>());
            hasEnemyQuery = true;
            return true;
        }

        private void CreateLabel()
        {
            var canvasTransform = GameUICanvas.GetOrCreate(transform);
            label = canvasTransform?.Find("Enemy Count")?.GetComponent<Text>();
            if (label == null)
                Debug.LogError("EnemyCountUGUI: Missing Enemy Count in GameHUD prefab.", this);
        }
    }
}
