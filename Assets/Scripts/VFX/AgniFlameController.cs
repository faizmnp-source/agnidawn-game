using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.VFX
{
    /// <summary>
    /// Drives the Agni Kund's particle system in real-time based on Agni tier.
    ///
    /// Subscribes to <c>OnAgniTierChanged</c> and <c>OnAgniAuraChanged</c> via
    /// EventBus, then lerps emission rate, start size, start speed, start colour,
    /// and light intensity to match the current tier's artistic intent:
    ///
    ///   Tier 1 — small, dim, blue-orange flicker
    ///   Tier 2 — larger, warm orange
    ///   Tier 3 — healthy flame, bright orange-gold
    ///   Tier 4 — large intense fire, deep gold
    ///   Tier 5 — massive blazing inferno, white-gold core
    ///
    /// SCENE SETUP:
    ///   1. Place this script on the Agni Kund GameObject.
    ///   2. Assign the primary flame ParticleSystem and (optional) ember ParticleSystem.
    ///   3. Optionally assign a Light2D for dynamic firelight.
    ///
    /// Linear: FAI-16 (Phase 11)
    /// </summary>
    public class AgniFlameController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Particle Systems")]
        [SerializeField] private ParticleSystem flamePS;
        [SerializeField] private ParticleSystem emberPS;

        [Header("Light")]
        [SerializeField] private Light flameLight;   // Works for 3D; for 2D use Light2D via reflection

        [Header("Per-Tier Flame Config")]
        [SerializeField] private TierFlameConfig[] tierConfigs = new TierFlameConfig[5];

        [Header("Transition")]
        [SerializeField, Min(0.1f)] private float lerpSpeed = 2f;

        // ── Data ──────────────────────────────────────────────────────────────────

        [System.Serializable]
        public struct TierFlameConfig
        {
            [Header("Emission")]
            [Min(0f)] public float emissionRate;
            [Min(0f)] public float emberEmissionRate;

            [Header("Particle Size / Speed")]
            public float startSizeMin;
            public float startSizeMax;
            public float startSpeedMin;
            public float startSpeedMax;

            [Header("Colour (start colour — gradient not used here)")]
            public Color coreColor;
            public Color outerColor;

            [Header("Light")]
            public float lightIntensity;
            public float lightRange;
            public Color lightColor;
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private int    _currentTier = 3;
        private float  _targetEmission;
        private float  _targetEmberEmission;
        private float  _targetSizeMin, _targetSizeMax;
        private float  _targetSpeedMin, _targetSpeedMax;
        private Color  _targetCoreColor;
        private float  _targetLightIntensity;
        private float  _targetLightRange;
        private Color  _targetLightColor;

        private ParticleSystem.EmissionModule _flameEmission;
        private ParticleSystem.MainModule     _flameMain;
        private ParticleSystem.EmissionModule _emberEmission;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            // Default tier configs if not set in Inspector
            if (tierConfigs == null || tierConfigs.Length != 5) InitDefaultTierConfigs();

            if (flamePS != null)
            {
                _flameEmission = flamePS.emission;
                _flameMain     = flamePS.main;
            }
            if (emberPS != null)
                _emberEmission = emberPS.emission;
        }

        private void OnEnable()
        {
            EventBus.On<int, int>       ("OnAgniTierChanged",  OnAgniTierChanged);
            EventBus.On<float, float>   ("OnAgniAuraChanged",  OnAgniAuraChanged);
            EventBus.On                 ("EVT_GAME_OVER",      OnGameOver);
            EventBus.On                 ("EVT_VICTORY",        OnVictory);
        }

        private void OnDisable()
        {
            EventBus.Off<int, int>      ("OnAgniTierChanged",  OnAgniTierChanged);
            EventBus.Off<float, float>  ("OnAgniAuraChanged",  OnAgniAuraChanged);
            EventBus.Off                ("EVT_GAME_OVER",      OnGameOver);
            EventBus.Off                ("EVT_VICTORY",        OnVictory);
        }

        private void Start()
        {
            ApplyTierImmediate(_currentTier);
        }

        private void Update()
        {
            LerpToTargets();
        }

        // ── Event Handlers ────────────────────────────────────────────────────────

        private void OnAgniTierChanged(int newTier, int prevTier)
        {
            _currentTier = Mathf.Clamp(newTier, 1, 5);
            var cfg = tierConfigs[_currentTier - 1];
            _targetEmission       = cfg.emissionRate;
            _targetEmberEmission  = cfg.emberEmissionRate;
            _targetSizeMin        = cfg.startSizeMin;
            _targetSizeMax        = cfg.startSizeMax;
            _targetSpeedMin       = cfg.startSpeedMin;
            _targetSpeedMax       = cfg.startSpeedMax;
            _targetCoreColor      = cfg.coreColor;
            _targetLightIntensity = cfg.lightIntensity;
            _targetLightRange     = cfg.lightRange;
            _targetLightColor     = cfg.lightColor;
        }

        private void OnAgniAuraChanged(float speedBuff, float damageBuff)
        {
            // Aura is active at tier 5 — add extra glow on top
            if (_currentTier >= 5 && flameLight != null)
                flameLight.intensity = _targetLightIntensity * (1f + damageBuff * 0.3f);
        }

        private void OnGameOver()
        {
            // Shrink flame to almost nothing
            _targetEmission      = 2f;
            _targetEmberEmission = 0f;
            _targetSizeMin       = 0.1f;
            _targetSizeMax       = 0.2f;
            _targetCoreColor     = new Color(0.2f, 0.2f, 0.4f);
            _targetLightIntensity= 0.1f;
        }

        private void OnVictory()
        {
            // Maximize flame on victory — blazing triumph
            _targetEmission      = 300f;
            _targetEmberEmission = 100f;
            _targetSizeMin       = 3.0f;
            _targetSizeMax       = 5.0f;
            _targetCoreColor     = new Color(1f, 0.95f, 0.6f);
            _targetLightIntensity= 8f;
            _targetLightRange    = 20f;
            _targetLightColor    = new Color(1f, 0.9f, 0.5f);
        }

        // ── Lerp ──────────────────────────────────────────────────────────────────

        private void LerpToTargets()
        {
            float t = lerpSpeed * Time.deltaTime;

            if (flamePS != null)
            {
                var em = flamePS.emission;
                float curRate = em.rateOverTime.constant;
                em.rateOverTime = Mathf.Lerp(curRate, _targetEmission, t);

                var main = flamePS.main;
                var sMin = main.startSize;
                sMin.constantMin = Mathf.Lerp(sMin.constantMin, _targetSizeMin, t);
                sMin.constantMax = Mathf.Lerp(sMin.constantMax, _targetSizeMax, t);
                main.startSize = sMin;

                var spd = main.startSpeed;
                spd.constantMin = Mathf.Lerp(spd.constantMin, _targetSpeedMin, t);
                spd.constantMax = Mathf.Lerp(spd.constantMax, _targetSpeedMax, t);
                main.startSpeed = spd;

                var col = main.startColor;
                col.color = Color.Lerp(col.color, _targetCoreColor, t);
                main.startColor = col;
            }

            if (emberPS != null)
            {
                var em = emberPS.emission;
                float curRate = em.rateOverTime.constant;
                em.rateOverTime = Mathf.Lerp(curRate, _targetEmberEmission, t);
            }

            if (flameLight != null)
            {
                flameLight.intensity = Mathf.Lerp(flameLight.intensity, _targetLightIntensity, t);
                flameLight.range     = Mathf.Lerp(flameLight.range,     _targetLightRange,     t);
                flameLight.color     = Color.Lerp(flameLight.color,      _targetLightColor,     t);
            }
        }

        private void ApplyTierImmediate(int tier)
        {
            int idx = Mathf.Clamp(tier - 1, 0, 4);
            var cfg = tierConfigs[idx];

            _targetEmission       = cfg.emissionRate;
            _targetEmberEmission  = cfg.emberEmissionRate;
            _targetSizeMin        = cfg.startSizeMin;
            _targetSizeMax        = cfg.startSizeMax;
            _targetSpeedMin       = cfg.startSpeedMin;
            _targetSpeedMax       = cfg.startSpeedMax;
            _targetCoreColor      = cfg.coreColor;
            _targetLightIntensity = cfg.lightIntensity;
            _targetLightRange     = cfg.lightRange;
            _targetLightColor     = cfg.lightColor;

            if (flamePS != null)
            {
                var em = flamePS.emission;
                em.rateOverTime = cfg.emissionRate;
                var main = flamePS.main;
                main.startSize  = new ParticleSystem.MinMaxCurve(cfg.startSizeMin, cfg.startSizeMax);
                main.startSpeed = new ParticleSystem.MinMaxCurve(cfg.startSpeedMin, cfg.startSpeedMax);
                var col = main.startColor;
                col.color = cfg.coreColor;
                main.startColor = col;
            }
            if (emberPS != null)
            {
                var em = emberPS.emission;
                em.rateOverTime = cfg.emberEmissionRate;
            }
            if (flameLight != null)
            {
                flameLight.intensity = cfg.lightIntensity;
                flameLight.range     = cfg.lightRange;
                flameLight.color     = cfg.lightColor;
            }
        }

        // ── Default Tier Configs ──────────────────────────────────────────────────

        private void InitDefaultTierConfigs()
        {
            tierConfigs = new TierFlameConfig[5];

            // Tier 1 — Dying: dim, cold, minimal
            tierConfigs[0] = new TierFlameConfig
            {
                emissionRate      = 15f,  emberEmissionRate  = 2f,
                startSizeMin      = 0.2f, startSizeMax       = 0.5f,
                startSpeedMin     = 0.5f, startSpeedMax      = 1.0f,
                coreColor         = new Color(0.4f, 0.5f, 1.0f),
                lightIntensity    = 0.5f, lightRange         = 3f,
                lightColor        = new Color(0.5f, 0.6f, 1.0f)
            };
            // Tier 2 — Weak: small warm flame
            tierConfigs[1] = new TierFlameConfig
            {
                emissionRate      = 30f,  emberEmissionRate  = 5f,
                startSizeMin      = 0.3f, startSizeMax       = 0.7f,
                startSpeedMin     = 0.8f, startSpeedMax      = 1.5f,
                coreColor         = new Color(1.0f, 0.6f, 0.2f),
                lightIntensity    = 1.0f, lightRange         = 4f,
                lightColor        = new Color(1.0f, 0.7f, 0.3f)
            };
            // Tier 3 — Normal: healthy orange flame
            tierConfigs[2] = new TierFlameConfig
            {
                emissionRate      = 60f,  emberEmissionRate  = 15f,
                startSizeMin      = 0.5f, startSizeMax       = 1.2f,
                startSpeedMin     = 1.0f, startSpeedMax      = 2.0f,
                coreColor         = new Color(1.0f, 0.65f, 0.1f),
                lightIntensity    = 2.0f, lightRange         = 6f,
                lightColor        = new Color(1.0f, 0.8f, 0.4f)
            };
            // Tier 4 — Strong: intense gold fire
            tierConfigs[3] = new TierFlameConfig
            {
                emissionRate      = 100f, emberEmissionRate  = 35f,
                startSizeMin      = 0.8f, startSizeMax       = 2.0f,
                startSpeedMin     = 1.5f, startSpeedMax      = 3.0f,
                coreColor         = new Color(1.0f, 0.9f, 0.3f),
                lightIntensity    = 4.0f, lightRange         = 9f,
                lightColor        = new Color(1.0f, 0.9f, 0.5f)
            };
            // Tier 5 — Blazing: massive divine inferno
            tierConfigs[4] = new TierFlameConfig
            {
                emissionRate      = 200f, emberEmissionRate  = 80f,
                startSizeMin      = 1.5f, startSizeMax       = 3.5f,
                startSpeedMin     = 2.0f, startSpeedMax      = 4.5f,
                coreColor         = new Color(1.0f, 0.97f, 0.85f),
                lightIntensity    = 7.0f, lightRange         = 14f,
                lightColor        = new Color(1.0f, 0.95f, 0.7f)
            };
        }
    }
}
