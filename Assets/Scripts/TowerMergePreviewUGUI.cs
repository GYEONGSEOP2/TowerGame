using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>Shows the next rank and changed combat values while a valid tower merge is hovered.</summary>
    [RequireComponent(typeof(TowerPlacementController))]
    public sealed class TowerMergePreviewUGUI : MonoBehaviour
    {
        private RectTransform canvasRect;
        private RectTransform panelRect;
        private Text title;
        private Text details;

        private void Awake()
        {
            CreatePanel();
        }

        public void Show(TowerInstance targetTower, Vector2 screenPosition)
        {
            if (targetTower == null || targetTower.Rank == TowerRank.Circle)
            {
                Hide();
                return;
            }

            var attack = targetTower.GetComponent<TowerAttack>();
            var upgrade = targetTower.GetComponent<TowerDamageUpgrade>();
            if (attack == null || upgrade == null)
            {
                Hide();
                return;
            }

            var nextRank = targetTower.Rank + 1;
            var nextDamage = attack.damage * upgrade.damageMultiplierPerRank;
            title.text = $"{targetTower.Rank} + {targetTower.Rank}  →  {nextRank}";
            title.color = targetTower.Definition.displayColor;
            details.text = targetTower.Definition.towerType switch
            {
                TowerType.Red => $"Damage  {attack.damage:F0}  →  {nextDamage:F0}\nExplosion  {targetTower.Definition.explosionDamage * attack.RankDamageMultiplier:F0}  →  {targetTower.Definition.explosionDamage * attack.RankDamageMultiplier * upgrade.damageMultiplierPerRank:F0}\nSplash Range  {targetTower.Definition.explosionRadius:F1}",
                TowerType.Purple => $"Damage  {attack.damage:F0}  →  {nextDamage:F0}\nTargets  {attack.ProjectileCount}  →  {attack.ProjectileCount + 1}",
                _ => $"Damage  {attack.damage:F0}  →  {nextDamage:F0}"
            };

            SetPosition(screenPosition);
            panelRect.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (panelRect != null)
                panelRect.gameObject.SetActive(false);
        }

        private void CreatePanel()
        {
            canvasRect = GameUICanvas.GetOrCreate(transform) as RectTransform;
            var panelTransform = canvasRect?.Find("Tower Merge Preview");
            if (panelTransform == null)
            {
                Debug.LogError("TowerMergePreviewUGUI: Missing Tower Merge Preview prefab instance.", this);
                return;
            }

            panelRect = panelTransform as RectTransform;
            title = panelTransform.Find("Title")?.GetComponent<Text>();
            details = panelTransform.Find("Details")?.GetComponent<Text>();
            if (title == null || details == null)
            {
                Debug.LogError("TowerMergePreviewUGUI: TowerMergePreview prefab hierarchy is incomplete.", this);
                return;
            }

            panelRect.gameObject.SetActive(false);
        }

        private void SetPosition(Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out var localPosition);
            localPosition += new Vector2(170f, -78f);
            var halfSize = panelRect.sizeDelta * 0.5f;
            var canvasHalfSize = canvasRect.rect.size * 0.5f;
            localPosition.x = Mathf.Clamp(localPosition.x, -canvasHalfSize.x + halfSize.x, canvasHalfSize.x - halfSize.x);
            localPosition.y = Mathf.Clamp(localPosition.y, -canvasHalfSize.y + halfSize.y, canvasHalfSize.y - halfSize.y);
            panelRect.anchoredPosition = localPosition;
        }

    }
}
