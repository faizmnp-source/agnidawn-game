using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AGNIDAWN.Core;

namespace AGNIDAWN.UI
{
    /// <summary>
    /// LevelUpUI — Divine boon selection screen.
    ///
    /// Shows 3 boon cards when the player levels up.
    /// Each card shows: deity name, boon name, rarity, description.
    /// Player selects one → EventBus emits OnBoonChosen → LevelUpManager applies it.
    ///
    /// Cards glow based on rarity:
    ///   Common → white    Rare → blue    Epic → purple    Divine → gold
    ///
    /// Linear: FAI-14
    /// </summary>
    public class LevelUpUI : BaseUIPanel
    {
        [Header("Boon Cards")]
        [SerializeField] private List<BoonCardUI> boonCards = new();

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI headerText;
        [SerializeField] private TextMeshProUGUI levelReachedText;

        private List<BoonData> _currentChoices = new();
        private int _levelReached;

        // Rarity colours
        private static readonly Color COLOR_COMMON = new Color(0.85f, 0.85f, 0.85f);
        private static readonly Color COLOR_RARE   = new Color(0.30f, 0.60f, 1.00f);
        private static readonly Color COLOR_EPIC   = new Color(0.75f, 0.30f, 1.00f);
        private static readonly Color COLOR_DIVINE = new Color(1.00f, 0.85f, 0.20f);

        // ──────────────────────────────────────────────────────────────────
        protected override void OnShow()
        {
            EventBus.On<List<BoonData>, int>("OnBoonChoicesReady", OnBoonChoicesReady);
        }

        protected override void OnHide()
        {
            EventBus.Off<List<BoonData>, int>("OnBoonChoicesReady", OnBoonChoicesReady);
        }

        // ──────────────────────────────────────────────────────────────────
        private void OnBoonChoicesReady(List<BoonData> choices, int level)
        {
            _currentChoices = choices;
            _levelReached   = level;

            if (levelReachedText != null)
                levelReachedText.text = $"LEVEL {level}";

            for (int i = 0; i < boonCards.Count; i++)
            {
                bool hasChoice = i < choices.Count;
                boonCards[i].gameObject.SetActive(hasChoice);

                if (hasChoice)
                {
                    var boon = choices[i];
                    boonCards[i].Setup(
                        boon,
                        RarityColor(boon.rarity),
                        () => SelectBoon(boon)
                    );
                }
            }
        }

        private void SelectBoon(BoonData boon)
        {
            EventBus.Emit<BoonData>("OnBoonChosen", boon);
            UIManager.Instance?.Pop();
        }

        // Public static so tests can verify rarity → color mapping without Unity scenes.
        public static Color RarityColor(BoonRarity rarity) => rarity switch
        {
            BoonRarity.Rare   => COLOR_RARE,
            BoonRarity.Epic   => COLOR_EPIC,
            BoonRarity.Divine => COLOR_DIVINE,
            _                 => COLOR_COMMON
        };
    }

    // ── Nested card component ──────────────────────────────────────────────
    /// <summary>Individual boon card — assign to each card prefab.</summary>
    public class BoonCardUI : MonoBehaviour
    {
        [SerializeField] private Image           cardBorder;
        [SerializeField] private Image           deityIcon;
        [SerializeField] private TextMeshProUGUI boonNameText;
        [SerializeField] private TextMeshProUGUI deityNameText;
        [SerializeField] private TextMeshProUGUI rarityText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Button          selectButton;

        private System.Action _onSelect;

        public void Setup(BoonData boon, Color rarityColor, System.Action onSelect)
        {
            _onSelect = onSelect;

            if (boonNameText    != null) boonNameText.text    = boon.displayName;
            if (deityNameText   != null) deityNameText.text   = boon.deity;
            if (rarityText      != null)
            {
                rarityText.text  = boon.rarity.ToString().ToUpper();
                rarityText.color = rarityColor;
            }
            if (descriptionText != null) descriptionText.text = boon.description;
            if (cardBorder      != null) cardBorder.color     = rarityColor;
            if (deityIcon       != null && boon.icon != null) deityIcon.sprite = boon.icon;

            selectButton?.onClick.RemoveAllListeners();
            selectButton?.onClick.AddListener(() => _onSelect?.Invoke());
        }
    }
}
