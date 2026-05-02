using UnityEngine;

namespace AGNIDAWN.Visuals
{
    /// <summary>
    /// ScriptableObject containing 5 URP post-process configurations — one per Agni Kund tier.
    ///
    /// Tier 1 (index 0) — dying Agni:  cool blue, dim, heavy vignette
    /// Tier 2 (index 1) — weak Agni:   slightly warm, low bloom
    /// Tier 3 (index 2) — normal Agni: neutral orange glow
    /// Tier 4 (index 3) — strong Agni: golden/intense bloom
    /// Tier 5 (index 4) — blazing Agni (Mahakali): fire red/gold, heat distortion
    ///
    /// <see cref="AgniVisualStateManager"/> reads this SO and lerps the live URP Volume
    /// whenever EventBus fires "OnAgniTierChanged".
    ///
    /// Linear: FAI-15 (Phase 10)
    /// </summary>
    [CreateAssetMenu(menuName = "AGNIDAWN/Visuals/Agni Tier Visual Data", fileName = "AgniTierVisualData")]
    public class AgniTierVisualData : ScriptableObject
    {
        // ── Tier Config ───────────────────────────────────────────────────────────

        [System.Serializable]
        public struct TierVisualConfig
        {
            [Header("Bloom")]
            [Range(0f, 10f)]   public float bloomIntensity;
            [Range(0f, 1f)]    public float bloomThreshold;
            [Range(0f, 1f)]    public float bloomScatter;
            [ColorUsage(false, true)] public Color bloomTint;

            [Header("Colour Adjustments")]
            [Range(-5f, 5f)]   public float postExposure;
            [Range(-100f, 100f)] public float saturation;
            [ColorUsage(false)] public Color colorFilter;

            [Header("Vignette")]
            [Range(0f, 1f)]    public float vignetteIntensity;
            public Color       vignetteColor;
            [Range(0f, 1f)]    public float vignetteSmoothness;

            [Header("Chromatic Aberration")]
            [Range(0f, 1f)]    public float chromaticAberration;

            [Header("Lens Distortion")]
            [Range(-1f, 1f)]   public float lensDistortionIntensity;

            [Header("Transition")]
            [Tooltip("How long the lerp to this tier takes in seconds")]
            [Min(0.1f)]        public float transitionDuration;
        }

        // ── Inspector ─────────────────────────────────────────────────────────────

        [SerializeField, Tooltip("5 entries — index 0 = Tier 1 (dying), index 4 = Tier 5 (blazing)")]
        private TierVisualConfig[] tierConfigs = new TierVisualConfig[5];

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Number of tier configs (always 5).</summary>
        public int TierCount => tierConfigs != null ? tierConfigs.Length : 0;

        /// <summary>
        /// Retrieves the visual config for <paramref name="tier"/> (1-based).
        /// Returns false when tier is out of range [1..5].
        /// </summary>
        public bool TryGetConfig(int tier, out TierVisualConfig config)
        {
            int index = tier - 1;
            if (tierConfigs != null && index >= 0 && index < tierConfigs.Length)
            {
                config = tierConfigs[index];
                return true;
            }
            config = default;
            return false;
        }

        // ── Reset / Defaults ──────────────────────────────────────────────────────

        /// <summary>Called by Unity when the SO is first created. Populates sensible defaults.</summary>
        private void Reset()
        {
            tierConfigs = new TierVisualConfig[5];

            // ── Tier 1 — Dying Agni: cool blue, dim, heavy vignette ──────────────
            tierConfigs[0] = new TierVisualConfig
            {
                bloomIntensity          = 0.5f,
                bloomThreshold          = 0.9f,
                bloomScatter            = 0.5f,
                bloomTint               = new Color(0.4f, 0.5f, 1.0f),
                postExposure            = -0.5f,
                saturation              = -30f,
                colorFilter             = new Color(0.7f, 0.75f, 1.0f),
                vignetteIntensity       = 0.35f,
                vignetteColor           = new Color(0.0f, 0.0f, 0.2f),
                vignetteSmoothness      = 0.4f,
                chromaticAberration     = 0.3f,
                lensDistortionIntensity = -0.1f,
                transitionDuration      = 1.5f
            };

            // ── Tier 2 — Weak Agni: slightly warm, low bloom ─────────────────────
            tierConfigs[1] = new TierVisualConfig
            {
                bloomIntensity          = 1.0f,
                bloomThreshold          = 0.8f,
                bloomScatter            = 0.5f,
                bloomTint               = new Color(1.0f, 0.8f, 0.5f),
                postExposure            = 0f,
                saturation              = -10f,
                colorFilter             = Color.white,
                vignetteIntensity       = 0.25f,
                vignetteColor           = new Color(0.2f, 0.0f, 0.0f),
                vignetteSmoothness      = 0.4f,
                chromaticAberration     = 0.1f,
                lensDistortionIntensity = 0f,
                transitionDuration      = 1.2f
            };

            // ── Tier 3 — Normal Agni: neutral orange glow ────────────────────────
            tierConfigs[2] = new TierVisualConfig
            {
                bloomIntensity          = 2.0f,
                bloomThreshold          = 0.7f,
                bloomScatter            = 0.55f,
                bloomTint               = new Color(1.0f, 0.65f, 0.2f),
                postExposure            = 0.2f,
                saturation              = 10f,
                colorFilter             = new Color(1.0f, 0.95f, 0.8f),
                vignetteIntensity       = 0.2f,
                vignetteColor           = new Color(0.4f, 0.1f, 0.0f),
                vignetteSmoothness      = 0.35f,
                chromaticAberration     = 0.05f,
                lensDistortionIntensity = 0.02f,
                transitionDuration      = 1.0f
            };

            // ── Tier 4 — Strong Agni: intense orange/gold bloom ──────────────────
            tierConfigs[3] = new TierVisualConfig
            {
                bloomIntensity          = 4.0f,
                bloomThreshold          = 0.6f,
                bloomScatter            = 0.6f,
                bloomTint               = new Color(1.0f, 0.5f, 0.1f),
                postExposure            = 0.5f,
                saturation              = 25f,
                colorFilter             = new Color(1.0f, 0.88f, 0.6f),
                vignetteIntensity       = 0.15f,
                vignetteColor           = new Color(0.6f, 0.2f, 0.0f),
                vignetteSmoothness      = 0.3f,
                chromaticAberration     = 0.0f,
                lensDistortionIntensity = 0.05f,
                transitionDuration      = 0.8f
            };

            // ── Tier 5 — Blazing Agni (Mahakali): fire red/gold, heat distortion ─
            tierConfigs[4] = new TierVisualConfig
            {
                bloomIntensity          = 7.0f,
                bloomThreshold          = 0.5f,
                bloomScatter            = 0.7f,
                bloomTint               = new Color(1.0f, 0.3f, 0.05f),
                postExposure            = 0.9f,
                saturation              = 45f,
                colorFilter             = new Color(1.0f, 0.8f, 0.4f),
                vignetteIntensity       = 0.08f,
                vignetteColor           = new Color(0.8f, 0.1f, 0.0f),
                vignetteSmoothness      = 0.25f,
                chromaticAberration     = 0.0f,
                lensDistortionIntensity = 0.12f,
                transitionDuration      = 0.5f
            };
        }
    }
}
