using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AGNIDAWN.Core;

namespace AGNIDAWN.UI
{
    /// <summary>
    /// BossIntroOverlayUI — Full-screen cinematic boss introduction.
    ///
    /// Sequence:
    ///   1. Black overlay fades in (0.4s)
    ///   2. Boss Sanskrit name appears (large, centre-screen)
    ///   3. Boss silhouette rises from bottom
    ///   4. English subtitle fades in beneath
    ///   5. Overlay holds for 2.5s then fades out
    ///
    /// Called by UIManager.OnBossSpawned with the boss ID string.
    /// Linear: FAI-14
    /// </summary>
    public class BossIntroOverlayUI : BaseUIPanel
    {
        [Header("Elements")]
        [SerializeField] private Image           blackOverlay;
        [SerializeField] private TextMeshProUGUI sanskritNameText;
        [SerializeField] private TextMeshProUGUI englishSubtitleText;
        [SerializeField] private Image           bossIconImage;

        [Header("Timing")]
        [SerializeField] private float fadeInTime   = 0.4f;
        [SerializeField] private float holdTime     = 2.5f;
        [SerializeField] private float fadeOutTime  = 0.6f;
        [SerializeField] private float iconRiseDistance = 120f;

        private Coroutine _sequence;

        // Boss data table
        private static readonly System.Collections.Generic.Dictionary<string, (string sanskrit, string subtitle)> BOSS_NAMES
            = new()
        {
            { "Ravana",      ("रावण",       "RAVANA — KING OF LANKA") },
            { "Mahishasura", ("महिषासुर",   "MAHISHASURA — THE DEMON BULL") },
            { "Kali",        ("काली",        "KALI — GODDESS OF DESTRUCTION") },
            { "Vritra",      ("वृत्र",       "VRITRA — THE STORM SERPENT") },
        };

        // ──────────────────────────────────────────────────────────────────
        public void ShowForBoss(string bossId)
        {
            if (_sequence != null) StopCoroutine(_sequence);
            _sequence = StartCoroutine(PlayIntroSequence(bossId));
        }

        private IEnumerator PlayIntroSequence(string bossId)
        {
            ShowImmediate();

            // Set text
            var names = BOSS_NAMES.TryGetValue(bossId, out var n) ? n : (bossId, bossId.ToUpper());
            if (sanskritNameText    != null) { sanskritNameText.text    = names.Item1; sanskritNameText.alpha    = 0f; }
            if (englishSubtitleText != null) { englishSubtitleText.text = names.Item2; englishSubtitleText.alpha = 0f; }

            // Place icon off-screen below
            Vector3 iconStart = bossIconImage != null ? bossIconImage.rectTransform.anchoredPosition3D : Vector3.zero;
            if (bossIconImage != null)
                bossIconImage.rectTransform.anchoredPosition += Vector2.down * iconRiseDistance;

            // Fade in black overlay
            if (blackOverlay != null)
            {
                blackOverlay.color = new Color(0, 0, 0, 0);
                float t = 0;
                while (t < fadeInTime)
                {
                    t += Time.unscaledDeltaTime;
                    blackOverlay.color = new Color(0, 0, 0, Mathf.Clamp01(t / fadeInTime));
                    yield return null;
                }
            }

            // Rise icon + fade Sanskrit name
            float elapsed = 0f;
            while (elapsed < 0.8f)
            {
                elapsed += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(elapsed / 0.8f);

                if (bossIconImage != null)
                    bossIconImage.rectTransform.anchoredPosition = Vector2.Lerp(
                        (Vector2)iconStart + Vector2.down * iconRiseDistance, (Vector2)iconStart, p);

                if (sanskritNameText != null)
                    sanskritNameText.alpha = p;

                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.3f);

            // Fade in subtitle
            elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                if (englishSubtitleText != null)
                    englishSubtitleText.alpha = Mathf.Clamp01(elapsed / 0.5f);
                yield return null;
            }

            yield return new WaitForSecondsRealtime(holdTime);

            // Fade out
            elapsed = 0f;
            while (elapsed < fadeOutTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float a = 1f - Mathf.Clamp01(elapsed / fadeOutTime);
                if (blackOverlay      != null) blackOverlay.color               = new Color(0, 0, 0, a);
                if (sanskritNameText  != null) sanskritNameText.alpha           = a;
                if (englishSubtitleText != null) englishSubtitleText.alpha      = a;
                yield return null;
            }

            HideImmediate();
            _sequence = null;
        }
    }
}
