using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>Displays combat stats for the currently selected tower in one shared UGUI panel.</summary>
    [RequireComponent(typeof(TowerSelectionController))]
    public sealed class TowerSelectionUGUI : MonoBehaviour
    {
        private TowerSelectionController selectionController;
        private GameObject panel;
        private Image icon;
        private Text title;
        private Text stats;
        private Text special;
        private Text sellValue;
        private Button sellButton;
        private TowerSellService sellService;

        private void Awake()
        {
            selectionController = GetComponent<TowerSelectionController>();
            sellService = GetComponent<TowerSellService>();
            CreatePanel();
            selectionController.SelectionChanged += Refresh;
            Refresh(selectionController.SelectedTower);
        }

        private void OnDestroy()
        {
            if (selectionController != null)
                selectionController.SelectionChanged -= Refresh;
        }

        private void CreatePanel()
        {
            var canvasTransform = GameUICanvas.GetOrCreate(transform);
            var panelTransform = canvasTransform?.Find("Tower Selection Panel");
            if (panelTransform == null)
            {
                Debug.LogError("TowerSelectionUGUI: Missing Tower Selection Panel prefab instance.", this);
                return;
            }

            panel = panelTransform.gameObject;
            icon = panelTransform.Find("Icon")?.GetComponent<Image>();
            title = panelTransform.Find("Title")?.GetComponent<Text>();
            stats = panelTransform.Find("Stats")?.GetComponent<Text>();
            special = panelTransform.Find("Special")?.GetComponent<Text>();
            sellValue = panelTransform.Find("Sell Value")?.GetComponent<Text>();
            sellButton = panelTransform.Find("Sell Button")?.GetComponent<Button>();
            if (icon == null || title == null || stats == null || special == null || sellValue == null || sellButton == null)
            {
                Debug.LogError("TowerSelectionUGUI: TowerSelectionPanel prefab hierarchy is incomplete.", this);
                return;
            }

            sellButton.onClick.AddListener(() => sellService?.TrySellSelectedTower());
        }

        private void Refresh(TowerInstance tower)
        {
            var visible = tower != null;
            panel.SetActive(visible);
            if (!visible)
                return;

            var attack = tower.GetComponent<TowerAttack>();
            var definition = tower.Definition;
            var color = definition.displayColor;
            title.text = $"{definition.towerType} {tower.Rank} Tower";
            title.color = color;
            icon.sprite = tower.Icon;
            icon.color = color;
            stats.text = $"Damage      {attack.damage:F0}\nAttack SPD  {1f / attack.fireInterval:F1} / sec\nRange       {attack.attackRange:F1}\nTargets     {attack.ProjectileCount}";
            special.text = definition.towerType switch
            {
                TowerType.Red => $"Explosion  {definition.explosionDamage * attack.RankDamageMultiplier:F0} dmg / {definition.explosionRadius:F1} range",
                TowerType.Blue => $"Slow  {(1f - definition.slowMultiplier) * 100f:F0}% / {definition.slowDuration:F1} sec",
                TowerType.Purple => "Nearest enemies receive one projectile each.",
                _ => string.Empty
            };
            special.color = color;
            sellValue.text = $"Sell Value  {sellService.GetSellValue(tower)}";
            sellValue.color = new Color(0.75f, 1f, 0.55f);
            sellButton.interactable = sellService != null;
        }

    }
}
