using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AGNIDAWN.Core;

namespace AGNIDAWN.UI
{
    /// <summary>
    /// DeathScreenUI — "The darkness claims you" end screen.
    ///
    /// Shows:
    ///   - Sanskrit death poem: "अग्नि बुझ गई" (The flame was extinguished)
    ///   - Survival time, enemies killed, level reached, shards collected
    ///   - New record indicator if best run beaten
    ///   - Retry button, Main Menu button
    ///
    /// Linear: FAI-14
    /// </summary>
    public class DeathScreenUI : BaseUIPanel
    {
        [Header("Flavour")]
        [SerializeField] private TextMeshProUGUI deathPoemText;
        [SerializeField] private TextMeshProUGUI deathSubtitleText;

        [Header("Run Stats")]
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI killsText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI shardsText;
        [SerializeField] private GameObject      newRecordBadge;

        [Header("Buttons")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;

        private static readonly string[] DEATH_POEMS =
        {
            "अग्नि बुझ गई",    // The flame was extinguished
            "अंधकार विजयी हुआ", // Darkness was victorious
            "आहुति अधूरी रही",  // The offering was incomplete
            "देव अप्रसन्न हैं", // The gods are displeased
        };

        // ──────────────────────────────────────────────────────────────────
        protected override void OnShow()
        {
            retryButton?.onClick.AddListener(OnRetry);
            mainMenuButton?.onClick.AddListener(OnMainMenu);

            // Random death poem
            if (deathPoemText != null)
                deathPoemText.text = DEATH_POEMS[Random.Range(0, DEATH_POEMS.Length)];

            PopulateStats();
        }

        protected override void OnHide()
        {
            retryButton?.onClick.RemoveListener(OnRetry);
            mainMenuButton?.onClick.RemoveListener(OnMainMenu);
        }

        // ──────────────────────────────────────────────────────────────────
        private void PopulateStats()
        {
            if (GameManager.Instance == null) return;

            float elapsed = GameManager.Instance.ElapsedTime;
            float min = Mathf.Floor(elapsed / 60f);
            float sec = elapsed % 60f;

            if (timeText  != null) timeText.text  = $"SURVIVED  {min:00}:{sec:00}";
            if (killsText != null) killsText.text = $"ENEMIES SLAIN  {GameManager.Instance.TotalKills}";
            if (levelText != null) levelText.text = $"LEVEL REACHED  {GameManager.Instance.CurrentLevel}";

            // Check if best run
            var save = SaveSystem.Load();
            bool isRecord = elapsed > save.bestRunTimeSeconds;
            if (newRecordBadge != null) newRecordBadge.SetActive(isRecord);
        }

        private void OnRetry()
        {
            GameManager.Instance?.RestartRun();
        }

        private void OnMainMenu()
        {
            GameManager.Instance?.ReturnToMainMenu();
            UIManager.Instance?.ShowMainMenu();
        }
    }
}
