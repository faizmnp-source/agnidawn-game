using UnityEngine;

namespace AGNIDAWN.Core
{
    /// <summary>
    /// Auto-detects GPU capability on startup and applies matching quality settings.
    /// Emits OnQualityTierSet (int tier) via EventBus so other systems can react.
    ///
    /// Tiers:
    ///   0 = Low    — < 1 GB VRAM or shader level < 35
    ///   1 = Medium — 1–2 GB VRAM
    ///   2 = High   — > 2 GB VRAM
    ///
    /// Target: Samsung Z Fold7 (Adreno 830, ~6 GB VRAM) → Tier 2.
    /// Fallback: any device with < 512 MB reported VRAM → Tier 0.
    ///
    /// Phase 16 — FAI-20
    /// </summary>
    public class QualityManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────
        public static QualityManager Instance { get; private set; }

        // ── Public state ───────────────────────────────────────────────────
        /// <summary>0 = Low, 1 = Medium, 2 = High</summary>
        public int CurrentTier { get; private set; } = -1;

        // ── EventBus key ───────────────────────────────────────────────────
        public static readonly string EVT_QUALITY_TIER_SET = "OnQualityTierSet";

        // ── Tier thresholds (MB) ───────────────────────────────────────────
        private const int VRAM_LOW_MB    = 1024;   // < 1 GB → Low
        private const int VRAM_MEDIUM_MB = 2048;   // < 2 GB → Medium, else High
        private const int SHADER_MIN_FOR_MEDIUM = 35; // Unity shader level for SM 3.5+

        // ── Per-tier settings ──────────────────────────────────────────────
        private static readonly QualityProfile[] Profiles = new QualityProfile[]
        {
            // Low
            new QualityProfile
            {
                UnityQualityLevel  = 0,          // "Very Low" in Project Quality settings
                TargetFrameRate    = 30,
                ShadowDistance     = 0f,          // shadows off
                MaxParticlesBudget = 500,
                TextureQuality     = 2,           // half-res
            },
            // Medium
            new QualityProfile
            {
                UnityQualityLevel  = 2,           // "Medium"
                TargetFrameRate    = 60,
                ShadowDistance     = 15f,
                MaxParticlesBudget = 2000,
                TextureQuality     = 1,           // quarter → actually full in Unity 6 = 0; 1=half
            },
            // High
            new QualityProfile
            {
                UnityQualityLevel  = 5,           // "Ultra"
                TargetFrameRate    = 120,
                ShadowDistance     = 40f,
                MaxParticlesBudget = 8000,
                TextureQuality     = 0,           // full-res
            },
        };

        // ──────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            DetectAndApply();
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Public API

        /// <summary>Force a specific tier (useful for settings menu).</summary>
        public void SetTier(int tier)
        {
            tier = Mathf.Clamp(tier, 0, Profiles.Length - 1);
            ApplyProfile(tier);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Detection

        private void DetectAndApply()
        {
            int vramMB      = SystemInfo.graphicsMemorySize;
            int shaderLevel = SystemInfo.graphicsShaderLevel;

            int tier;
            if (vramMB < VRAM_LOW_MB || shaderLevel < SHADER_MIN_FOR_MEDIUM)
                tier = 0;
            else if (vramMB < VRAM_MEDIUM_MB)
                tier = 1;
            else
                tier = 2;

            Debug.Log($"[QualityManager] GPU: {SystemInfo.graphicsDeviceName} | "
                    + $"VRAM: {vramMB} MB | ShaderLevel: {shaderLevel} → Tier {tier}");

            ApplyProfile(tier);
        }

        private void ApplyProfile(int tier)
        {
            var p = Profiles[tier];

            QualitySettings.SetQualityLevel(p.UnityQualityLevel, applyExpensiveChanges: true);
            Application.targetFrameRate = p.TargetFrameRate;

#if UNITY_6000_0_OR_NEWER
            // Unity 6: globalTextureMipmapLimit replaces masterTextureLimit
            QualitySettings.globalTextureMipmapLimit = p.TextureQuality;
#else
            QualitySettings.masterTextureLimit = p.TextureQuality;
#endif

            CurrentTier = tier;
            EventBus.Emit<int>(EVT_QUALITY_TIER_SET, tier);

            Debug.Log($"[QualityManager] Applied tier {tier} — "
                    + $"FPS cap: {p.TargetFrameRate}, Shadows: {p.ShadowDistance}m, "
                    + $"Particles budget: {p.MaxParticlesBudget}");
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Data

        private struct QualityProfile
        {
            public int   UnityQualityLevel;
            public int   TargetFrameRate;
            public float ShadowDistance;
            public int   MaxParticlesBudget;
            public int   TextureQuality;
        }

        #endregion
    }
}
