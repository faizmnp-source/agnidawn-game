using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using AGNIDAWN.Core;

namespace AGNIDAWN.Visuals
{
    /// <summary>
    /// Full-screen colour flash overlay driven by AGNIDAWN EventBus events.
    ///
    /// Each event type has its own flash colour and meaning:
    ///   • Orange  — Agni Kund took significant damage
    ///   • White   — Boss entered a new phase (jarring, urgent)
    ///   • Gold    — Agni Kund tier upgraded (positive)
    ///   • Dark Red — Game Over (holds then fades to near-black)
    ///   • Bright Gold — Victory (celebratory pulse)
    ///
    /// SCENE SETUP:
    ///   1. Add a Canvas (Screen Space – Overlay, Sort Order: 99+).
    ///   2. Add a full-screen Image child (stretch anchors, Raycast Target: OFF).
    ///   3. Set the Image's initial colour to clear (alpha = 0).
    ///   4. Drag the Image into the <see cref="flashImage"/> Inspector field.
    ///   5. Attach this script to any persistent GameObject in the scene.
    ///
    /// Linear: FAI-15 (Phase 10)
    /// </summary>
    public class ScreenFlashController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [SerializeField] private Image flashImage;

        [Header("Flash Colours  (RGBA — alpha controls peak opacity)")]
        [SerializeField] private Color agniDamageColor  = new Color(1f,   0.4f, 0f,   0.28f);
        [SerializeField] private Color bossPhaseColor   = new Color(1f,   1f,   1f,   0.40f);
        [SerializeField] private Color tierUpColor      = new Color(1f,   0.75f,0.1f, 0.50f);
        [SerializeField] private Color gameOverColor    = new Color(0.55f,0f,   0f,   0.65f);
        [SerializeField] private Color victoryColor     = new Color(1f,   0.92f,0.3f, 0.70f);

        [Header("Timing")]
        [SerializeField, Min(0.02f)] private float fadeInTime  = 0.07f;
        [SerializeField, Min(0.05f)] private float fadeOutTime = 0.35f;

        // ── Private ───────────────────────────────────────────────────────────────

        private Coroutine _flashCoroutine;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (flashImage != null)
            {
                flashImage.raycastTarget = false;
                flashImage.color         = Color.clear;
            }
        }

        private void OnEnable()
        {
            EventBus.On<float>       ("OnAgniKundDamaged",  OnAgniKundDamaged);
            EventBus.On<string, int> ("OnBossPhaseChanged", OnBossPhaseChanged);
            EventBus.On<int, int>    ("OnAgniTierChanged",  OnAgniTierChanged);
            EventBus.On              ("EVT_GAME_OVER",      OnGameOver);
            EventBus.On              ("EVT_VICTORY",        OnVictory);
        }

        private void OnDisable()
        {
            EventBus.Off<float>       ("OnAgniKundDamaged",  OnAgniKundDamaged);
            EventBus.Off<string, int> ("OnBossPhaseChanged", OnBossPhaseChanged);
            EventBus.Off<int, int>    ("OnAgniTierChanged",  OnAgniTierChanged);
            EventBus.Off              ("EVT_GAME_OVER",      OnGameOver);
            EventBus.Off              ("EVT_VICTORY",        OnVictory);
            if (flashImage != null) flashImage.color = Color.clear;
        }

        // ── Event Handlers ────────────────────────────────────────────────────────

        private void OnAgniKundDamaged(float damage)
        {
            // Only flash on meaningful damage — ignore chip hits
            if (damage >= 10f) Flash(agniDamageColor);
        }

        private void OnBossPhaseChanged(string bossId, int phase)
        {
            Flash(bossPhaseColor);
        }

        private void OnAgniTierChanged(int newTier, int prevTier)
        {
            if (newTier > prevTier) Flash(tierUpColor);          // tier upgrade: golden burst
            // Tier downgrade has no flash — the desaturation from AgniVisualStateManager is enough
        }

        private void OnGameOver()
        {
            Flash(gameOverColor, holdTime: 1.2f);
        }

        private void OnVictory()
        {
            Flash(victoryColor);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Trigger a flash to <paramref name="color"/>.
        /// An optional <paramref name="holdTime"/> keeps the flash at peak opacity
        /// before fading (used for Game Over to create a "burn" feel).
        /// </summary>
        public void Flash(Color color, float holdTime = 0f)
        {
            if (flashImage == null) return;
            if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(DoFlash(color, holdTime));
        }

        // ── Coroutine ─────────────────────────────────────────────────────────────

        private IEnumerator DoFlash(Color target, float holdTime)
        {
            // ── Fade in ────────────────────────────────────────────────────────────
            float elapsed = 0f;
            while (elapsed < fadeInTime)
            {
                elapsed += Time.deltaTime;
                flashImage.color = Color.Lerp(Color.clear, target,
                    Mathf.Clamp01(elapsed / fadeInTime));
                yield return null;
            }
            flashImage.color = target;

            // ── Hold ───────────────────────────────────────────────────────────────
            if (holdTime > 0f) yield return new WaitForSeconds(holdTime);

            // ── Fade out ───────────────────────────────────────────────────────────
            elapsed = 0f;
            while (elapsed < fadeOutTime)
            {
                elapsed += Time.deltaTime;
                flashImage.color = Color.Lerp(target, Color.clear,
                    Mathf.Clamp01(elapsed / fadeOutTime));
                yield return null;
            }
            flashImage.color = Color.clear;
            _flashCoroutine  = null;
        }
    }
}
