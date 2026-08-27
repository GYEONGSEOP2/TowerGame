using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>Displays the total number of enemies defeated by the player.</summary>
    [RequireComponent(typeof(TowerEconomy))]
    public sealed class KillCountUGUI : MonoBehaviour
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

            label.text = $"Kills: {economy.KillCount:N0}";
            nextRefreshTime = Time.unscaledTime + RefreshInterval;
        }

        private void CreateLabel()
        {
            var canvasTransform = GameUICanvas.GetOrCreate(transform);
            label = canvasTransform?.Find("Kill Count")?.GetComponent<Text>();
            if (label == null)
                Debug.LogError("KillCountUGUI: Missing Kill Count in GameHUD prefab.", this);
        }
    }
}
