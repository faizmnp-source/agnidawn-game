using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AGNIDAWN.Core;

namespace AGNIDAWN.UI
{
    /// <summary>
    /// VictoryScreenUI — "Dawn has broken" victory screen.
    ///
    /// Reveals with sunrise animation sequence:
    ///   1. Screen brightens from night → dawn
    ///   2. Sanskrit victory text rises: "जय अग्नि" (Victory to the Fire)
    ///   3. Stats count up
    ///   4. Divine shard reward revealed
    ///
    /// Linear: FAI-14
    /// </summary>
    public class VictoryScreenUI : BaseUIPanel
    {
        [Header("Cinematic")]
        [SerializeField] private Image           sunriseOverlay;
        [SerializeField] private Gradient        sunriseGradient;
        [SerializeField] private float           sunriseDuration = 3f;

        [Header("Victory Text")]
        [SerializeField] private TextMeshProUGUI victoryPoemText;
        [SerializeField] private TextMeshProUGUI victorySubtitleText;

        [Header("Run Stats")]
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI killsText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI shardsRewardText;
        [SerializeField] private GameObject      newRecordBadge;

        [Header("Buttons")]
        [SerializeField] private Button continueButton;

        // ──────────────────────────────────────────────────────────────────
        protected override void OnShow()
        {
            continueButton?.onClick.AddListener(OnContinue);
            StartCoroutine(PlayVictorySequence());
        }

        protected override void OnHide()
        {
            continueButton?.onClick.RemoveListener(OnContinue);
            StopAllCoroutines();
        }

        // ──────────────────────────────────────────────────────────────────
        private IEnumerator PlayVictorySequence()
        {
            if (victoryPoemText     != null) victoryPoemText.alpha     = 0f;
            if (victorySubtitleText != null) victorySubtitleText.alpha = 0f;

            // Sunrise sweep
            if (sunriseOverlay != null)
            {
                float t = 0f;
                while (t < sunriseDuration)
                {
                    t += Time.unscaledDeltaTime;
                    float p = t / sunriseDuration;
                    sunriseOverlay.color = sunriseGradient.Evaluate(p);
                    yield return null;
                }
            }

            // Victory poem rise
            if (victoryPoemText != null)
            {
                victoryPoemText.text = "जय अग्नि";  // Victory to the Fire
                float t = 0f;
                while (t < 1f)
                {
                    t += Time.unscaledDeltaTime * 1.5f;
                    victoryPoemText.alpha = Mathf.Clamp01(t);
                    yield return null;
                }
            }

            yield return new WaitForSecondsRealtime(0.5f);

            if (victorySubtitleText != null)
            {
                victorySubtitleText.text  = "DAWN HAS BROKEN — THE AGNI KUND ENDURES";
                float t = 0f;
                while (t < 1f)
                {
                    t += Time.unscaledDeltaTime * 2f;
                    victorySubtitleText.alpha = Mathf.Clamp01(t);
                    yield return null;
                }
            }

            yield return new WaitForSecondsRealtime(0.5f);
            PopulateStats();
        }

        private void PopulateStats()
        {
            if (GameManager.Instance == null) return;

            float elapsed = GameManager.Instance.ElapsedTime;
            float min = Mathf.Floor(elapsed / 60f);
            float sec = elapsed % 60f;

            if (timeText  != null) timeText.text  = $"SURVIVED  {min:00}:{sec:00}";
            if (killsText != null) killsText.text = $"ENEMIES SLAIN  {GameManager.Instance.TotalKills}";
            if (levelText != null) levelText.text = $"LEVEL REACHED  {GameManager.Instance.CurrentLevel}";

            var save    = SaveSystem.Load();
            bool record = elapsed > save.bestRunTimeSeconds;
            if (newRecordBadge != null) newRecordBadge.SetActive(record);

            int shardsEarned = Mathf.RoundToInt(GameManager.Instance.TotalKills * 0.5f + 200);
            if (shardsRewardText != null)
                shardsRewardText.text = $"✦ {shardsEarned} DIVINE SHARDS AWARDED ✦";
        }

        private void OnContinue()
        {
            GameManager.Instance?.ReturnToMainMenu();
            UIManager.Instance?.ShowMainMenu();
        }
    }
}
