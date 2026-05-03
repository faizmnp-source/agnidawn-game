using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AGNIDAWN.Core;

namespace AGNIDAWN.UI
{
    /// <summary>
    /// PauseMenuUI — In-run pause overlay.
    ///
    /// Buttons: Resume, Restart, Main Menu, Settings (placeholder)
    /// Shows current run stats: elapsed time, kills, level.
    /// Linear: FAI-14
    /// </summary>
    public class PauseMenuUI : BaseUIPanel
    {
        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button settingsButton;

        [Header("Run Stats")]
        [SerializeField] private TextMeshProUGUI elapsedTimeText;
        [SerializeField] private TextMeshProUGUI killCountText;
        [SerializeField] private TextMeshProUGUI levelText;

        // ──────────────────────────────────────────────────────────────────
        protected override void OnShow()
        {
            resumeButton?.onClick.AddListener(OnResume);
            restartButton?.onClick.AddListener(OnRestart);
            mainMenuButton?.onClick.AddListener(OnMainMenu);
            settingsButton?.onClick.AddListener(OnSettings);

            RefreshStats();
        }

        protected override void OnHide()
        {
            resumeButton?.onClick.RemoveListener(OnResume);
            restartButton?.onClick.RemoveListener(OnRestart);
            mainMenuButton?.onClick.RemoveListener(OnMainMenu);
            settingsButton?.onClick.RemoveListener(OnSettings);
        }

        // ──────────────────────────────────────────────────────────────────
        private void RefreshStats()
        {
            if (GameManager.Instance == null) return;

            float elapsed = GameManager.Instance.ElapsedTime;
            float min = Mathf.Floor(elapsed / 60f);
            float sec = elapsed % 60f;

            if (elapsedTimeText != null)
                elapsedTimeText.text = $"TIME  {min:00}:{sec:00}";
        }

        private void OnResume()  => GameManager.Instance?.ResumeGame();
        private void OnRestart() => GameManager.Instance?.RestartRun();
        private void OnMainMenu()
        {
            GameManager.Instance?.ReturnToMainMenu();
            UIManager.Instance?.ShowMainMenu();
        }
        private void OnSettings() => EventBus.Emit("OnSettingsMenuRequested");
    }
}
