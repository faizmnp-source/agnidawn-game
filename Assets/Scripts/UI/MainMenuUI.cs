using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AGNIDAWN.Core;

namespace AGNIDAWN.UI
{
    /// <summary>
    /// MainMenuUI — Title screen with Sanskrit-inspired aesthetic.
    ///
    /// Layout:
    ///   - Animated title "AGNIDAWN" (pulsing divine gold)
    ///   - Subtitle in Devanagari-style font: "अग्निदॉन"
    ///   - Play button → loads game scene
    ///   - Shrines button → meta-progression screen
    ///   - Settings button
    ///   - Quit button
    ///   - Animated temple fire in background (Particle System)
    ///
    /// Linear: FAI-14
    /// </summary>
    public class MainMenuUI : BaseUIPanel
    {
        [Header("Title")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private float           titlePulseSpeed  = 1.5f;
        [SerializeField] private float           titlePulseAmount = 0.08f;

        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button shrinesButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Background")]
        [SerializeField] private ParticleSystem backgroundFire;

        private float _pulseTimer;
        private Vector3 _titleBaseScale;

        // ──────────────────────────────────────────────────────────────────
        protected override void Awake()
        {
            base.Awake();
            if (titleText != null) _titleBaseScale = titleText.transform.localScale;
        }

        protected override void OnShow()
        {
            playButton?.onClick.AddListener(OnPlayClicked);
            shrinesButton?.onClick.AddListener(OnShrinesClicked);
            settingsButton?.onClick.AddListener(OnSettingsClicked);
            quitButton?.onClick.AddListener(OnQuitClicked);

            backgroundFire?.Play();
        }

        protected override void OnHide()
        {
            playButton?.onClick.RemoveListener(OnPlayClicked);
            shrinesButton?.onClick.RemoveListener(OnShrinesClicked);
            settingsButton?.onClick.RemoveListener(OnSettingsClicked);
            quitButton?.onClick.RemoveListener(OnQuitClicked);

            backgroundFire?.Stop();
        }

        protected override void Update()
        {
            base.Update();
            if (!IsVisible || titleText == null) return;

            // Gentle scale pulse on title
            _pulseTimer += Time.unscaledDeltaTime * titlePulseSpeed;
            float scale = 1f + Mathf.Sin(_pulseTimer) * titlePulseAmount;
            titleText.transform.localScale = _titleBaseScale * scale;
        }

        // ──────────────────────────────────────────────────────────────────
        #region Button Handlers

        private void OnPlayClicked()
        {
            EventBus.Emit("OnMainMenuPlayPressed");
            // GameManager will handle scene loading / state transition
            GameManager.Instance?.StartGame();
        }

        private void OnShrinesClicked()
        {
            EventBus.Emit("OnShrinesMenuRequested");
        }

        private void OnSettingsClicked()
        {
            EventBus.Emit("OnSettingsMenuRequested");
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion
    }
}
