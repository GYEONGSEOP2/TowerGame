using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>Displays the current player currency in the upper-right corner.</summary>
    [RequireComponent(typeof(TowerEconomy))]
    public sealed class MoneyCountUGUI : MonoBehaviour
    {
        private const float RefreshInterval = 0.1f;

        private TowerEconomy economy;
        private Text label;
        private float nextRefreshTime;

        private void Awake()
        {
            economy = GetComponent<TowerEconomy>();
            CreateLabel();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshTime)
                return;

            label.text = $"Money: {economy.CurrentMoney:N0}";
            nextRefreshTime = Time.unscaledTime + RefreshInterval;
        }

        private void CreateLabel()
        {
            var canvasTransform = GameUICanvas.GetOrCreate(transform);
            label = canvasTransform?.Find("Money Count")?.GetComponent<Text>();
            if (label == null)
                Debug.LogError("MoneyCountUGUI: Missing Money Count in GameHUD prefab.", this);
        }
    }
}
