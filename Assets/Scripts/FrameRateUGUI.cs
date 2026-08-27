using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>Displays a low-overhead average FPS readout for runtime performance checks.</summary>
    [RequireComponent(typeof(TowerPlacementController))]
    public sealed class FrameRateUGUI : MonoBehaviour
    {
        private const float RefreshInterval = 0.5f;

        private Text label;
        private float elapsed;
        private int frameCount;

        private void Awake()
        {
            CreateLabel();
        }

        private void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            frameCount++;
            if (elapsed < RefreshInterval)
                return;

            var framesPerSecond = Mathf.RoundToInt(frameCount / elapsed);
            label.text = $"FPS: {framesPerSecond}";
            label.color = framesPerSecond switch
            {
                >= 60 => new Color(0.35f, 1f, 0.45f),
                >= 30 => new Color(1f, 0.82f, 0.2f),
                _ => new Color(1f, 0.35f, 0.3f)
            };

            elapsed = 0f;
            frameCount = 0;
        }

        private void CreateLabel()
        {
            var canvasTransform = GameUICanvas.GetOrCreate(transform);
            label = canvasTransform?.Find("Frame Rate")?.GetComponent<Text>();
            if (label == null)
                Debug.LogError("FrameRateUGUI: Missing Frame Rate in GameHUD prefab.", this);
        }
    }
}
