using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using AGNIDAWN.Core;

#if URP_ENABLED
using UnityEngine.Rendering.Universal;
#endif

namespace AGNIDAWN.Visuals
{
    /// <summary>
    /// Drives URP post-processing Volume parameters in response to Agni Kund tier
    /// changes and game events.  All communication is via EventBus — no direct
    /// cross-assembly type references needed.
    ///
    /// SCENE SETUP:
    ///   1. Create a GameObject "PostProcessVolume" in your scene.
    ///   2. Add a Volume component (Mode: Global, IsGlobal: true).
    ///   3. Create a VolumeProfile and assign it.  Add overrides for:
    ///         Bloom, Color Adjustments, Vignette,
    ///         Chromatic Aberration, Lens Distortion
    ///   4. Attach this script to the same GameObject.
    ///   5. Assign an AgniTierVisualData SO in the Inspector.
    ///
    /// Linear: FAI-15 (Phase 10)
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public class AgniVisualStateManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [SerializeField] private AgniTierVisualData visualData;

        [Tooltip("Which Agni tier to display before any OnAgniTierChanged event fires.")]
        [SerializeField, Range(1, 5)] private int startingTier = 3;

        // ── Private state ─────────────────────────────────────────────────────────
        private Volume     _volume;
        private Coroutine  _transitionCoroutine;
        private int        _currentTier;
        private bool       _gameEnded;

#if URP_ENABLED
        private Bloom                _bloom;
        private ColorAdjustments     _colorAdj;
        private Vignette             _vignette;
        private ChromaticAberration  _chromatic;
        private LensDistortion       _lensDistortion;
#endif

        // ── Unity Lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            _volume = GetComponent<Volume>();

#if URP_ENABLED
            if (_volume == null || _volume.profile == null)
            {
                Debug.LogError("[AgniVisualStateManager] Requires a Volume with an assigned profile.");
                return;
            }
            _volume.profile.TryGet(out _bloom);
            _volume.profile.TryGet(out _colorAdj);
            _volume.profile.TryGet(out _vignette);
            _volume.profile.TryGet(out _chromatic);
            _volume.profile.TryGet(out _lensDistortion);
#endif
        }

        private void OnEnable()
        {
            EventBus.On<int, int>("OnAgniTierChanged",  OnAgniTierChanged);
            EventBus.On<string>  ("OnBossSpawned",      OnBossSpawned);
            EventBus.On<string>  ("OnBossDied",         OnBossDied);
            EventBus.On          ("EVT_GAME_OVER",      OnGameOver);
            EventBus.On          ("EVT_VICTORY",        OnVictory);
        }

        private void OnDisable()
        {
            EventBus.Off<int, int>("OnAgniTierChanged", OnAgniTierChanged);
            EventBus.Off<string>  ("OnBossSpawned",     OnBossSpawned);
            EventBus.Off<string>  ("OnBossDied",        OnBossDied);
            EventBus.Off          ("EVT_GAME_OVER",     OnGameOver);
            EventBus.Off          ("EVT_VICTORY",       OnVictory);
        }

        private void Start()
        {
            _currentTier = startingTier;
            ApplyTierImmediate(_currentTier);
        }

        // ── Event Handlers ────────────────────────────────────────────────────────

        private void OnAgniTierChanged(int newTier, int prevTier)
        {
            if (_gameEnded) return;
            _currentTier = newTier;
            BeginTransitionToTier(newTier);
        }

        private void OnBossSpawned(string bossId)
        {
            // Slightly boost saturation while boss is active — heightens tension
#if URP_ENABLED
            if (_colorAdj != null && !_gameEnded)
                _colorAdj.saturation.Override(_colorAdj.saturation.value + 10f);
#endif
        }

        private void OnBossDied(string bossId)
        {
            // Restore to current tier state after boss death
            if (!_gameEnded) BeginTransitionToTier(_currentTier);
        }

        private void OnGameOver()
        {
            _gameEnded = true;
            StopActiveTransition();
            _transitionCoroutine = StartCoroutine(TransitionGameOver());
        }

        private void OnVictory()
        {
            _gameEnded = true;
            StopActiveTransition();
            _transitionCoroutine = StartCoroutine(TransitionVictory());
        }

        // ── Transition Control ────────────────────────────────────────────────────

        private void BeginTransitionToTier(int tier)
        {
            if (visualData == null || !visualData.TryGetConfig(tier, out var cfg)) return;
            StopActiveTransition();
            _transitionCoroutine = StartCoroutine(LerpToConfig(cfg, cfg.transitionDuration));
        }

        private void ApplyTierImmediate(int tier)
        {
            if (visualData == null || !visualData.TryGetConfig(tier, out var cfg)) return;
            ApplyConfigDirect(cfg);
        }

        private void StopActiveTransition()
        {
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
            }
        }

        // ── Coroutines ────────────────────────────────────────────────────────────

        private IEnumerator LerpToConfig(AgniTierVisualData.TierVisualConfig target, float duration)
        {
#if URP_ENABLED
            // Snapshot current values
            float sBloomInt  = _bloom  != null ? _bloom.intensity.value         : 0f;
            float sBloomThr  = _bloom  != null ? _bloom.threshold.value         : 0.9f;
            float sBloomSct  = _bloom  != null ? _bloom.scatter.value           : 0.5f;
            Color sBloomTint = _bloom  != null ? (Color)_bloom.tint.value       : Color.white;
            float sPostExp   = _colorAdj != null ? _colorAdj.postExposure.value : 0f;
            float sSat       = _colorAdj != null ? _colorAdj.saturation.value   : 0f;
            Color sCF        = _colorAdj != null ? (Color)_colorAdj.colorFilter.value : Color.white;
            float sVigInt    = _vignette != null ? _vignette.intensity.value     : 0f;
            Color sVigCol    = _vignette != null ? (Color)_vignette.color.value  : Color.black;
            float sVigSmth   = _vignette != null ? _vignette.smoothness.value    : 0.4f;
            float sChrom     = _chromatic     != null ? _chromatic.intensity.value     : 0f;
            float sLens      = _lensDistortion != null ? _lensDistortion.intensity.value : 0f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

                if (_bloom != null)
                {
                    _bloom.active = true;
                    _bloom.intensity.Override(Mathf.Lerp(sBloomInt,  target.bloomIntensity, t));
                    _bloom.threshold.Override(Mathf.Lerp(sBloomThr,  target.bloomThreshold, t));
                    _bloom.scatter.Override  (Mathf.Lerp(sBloomSct,  target.bloomScatter,   t));
                    _bloom.tint.Override     (Color.Lerp(sBloomTint, target.bloomTint,       t));
                }
                if (_colorAdj != null)
                {
                    _colorAdj.active = true;
                    _colorAdj.postExposure.Override(Mathf.Lerp(sPostExp, target.postExposure, t));
                    _colorAdj.saturation.Override  (Mathf.Lerp(sSat,     target.saturation,   t));
                    _colorAdj.colorFilter.Override (Color.Lerp(sCF,      target.colorFilter,  t));
                }
                if (_vignette != null)
                {
                    _vignette.active = true;
                    _vignette.intensity.Override  (Mathf.Lerp(sVigInt,  target.vignetteIntensity,  t));
                    _vignette.color.Override      (Color.Lerp(sVigCol,  target.vignetteColor,       t));
                    _vignette.smoothness.Override (Mathf.Lerp(sVigSmth, target.vignetteSmoothness,  t));
                }
                if (_chromatic != null)
                {
                    _chromatic.active = target.chromaticAberration > 0f;
                    _chromatic.intensity.Override(Mathf.Lerp(sChrom, target.chromaticAberration, t));
                }
                if (_lensDistortion != null)
                {
                    _lensDistortion.active = Mathf.Abs(target.lensDistortionIntensity) > 0.001f;
                    _lensDistortion.intensity.Override(Mathf.Lerp(sLens, target.lensDistortionIntensity, t));
                }

                yield return null;
            }
            // Snap to exact target values
            ApplyConfigDirect(target);
#else
            yield return null;
#endif
        }

        private IEnumerator TransitionGameOver()
        {
            // 2-second desaturation + darken + heavy red vignette
#if URP_ENABLED
            float startSat = _colorAdj != null ? _colorAdj.saturation.value  : 0f;
            float startExp = _colorAdj != null ? _colorAdj.postExposure.value : 0f;
            Color startCF  = _colorAdj != null ? (Color)_colorAdj.colorFilter.value : Color.white;
            float startVig = _vignette  != null ? _vignette.intensity.value          : 0f;

            float elapsed = 0f, duration = 2.0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (_colorAdj != null)
                {
                    _colorAdj.saturation.Override  (Mathf.Lerp(startSat, -80f, t));
                    _colorAdj.postExposure.Override (Mathf.Lerp(startExp, -1.5f, t));
                    _colorAdj.colorFilter.Override  (Color.Lerp(startCF, new Color(0.3f, 0f, 0f), t));
                }
                if (_vignette != null)
                    _vignette.intensity.Override(Mathf.Lerp(startVig, 0.7f, t));

                yield return null;
            }
#else
            yield return null;
#endif
        }

        private IEnumerator TransitionVictory()
        {
            // 1.5-second golden bloom flash
#if URP_ENABLED
            float startBloom = _bloom    != null ? _bloom.intensity.value          : 0f;
            Color startTint  = _bloom    != null ? (Color)_bloom.tint.value        : Color.white;
            float startSat   = _colorAdj != null ? _colorAdj.saturation.value      : 0f;
            float startExp   = _colorAdj != null ? _colorAdj.postExposure.value    : 0f;
            float startVig   = _vignette  != null ? _vignette.intensity.value       : 0f;

            float elapsed = 0f, duration = 1.5f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

                if (_bloom != null)
                {
                    _bloom.intensity.Override(Mathf.Lerp(startBloom, 12f, t));
                    _bloom.tint.Override     (Color.Lerp(startTint, new Color(1f, 0.9f, 0.4f), t));
                }
                if (_colorAdj != null)
                {
                    _colorAdj.saturation.Override  (Mathf.Lerp(startSat, 60f,  t));
                    _colorAdj.postExposure.Override (Mathf.Lerp(startExp, 1.5f, t));
                }
                if (_vignette != null)
                    _vignette.intensity.Override(Mathf.Lerp(startVig, 0f, t));

                yield return null;
            }
#else
            yield return null;
#endif
        }

        // ── Direct Apply (no lerp) ────────────────────────────────────────────────

        private void ApplyConfigDirect(AgniTierVisualData.TierVisualConfig cfg)
        {
#if URP_ENABLED
            if (_bloom != null)
            {
                _bloom.active = true;
                _bloom.intensity.Override (cfg.bloomIntensity);
                _bloom.threshold.Override (cfg.bloomThreshold);
                _bloom.scatter.Override   (cfg.bloomScatter);
                _bloom.tint.Override      (cfg.bloomTint);
            }
            if (_colorAdj != null)
            {
                _colorAdj.active = true;
                _colorAdj.postExposure.Override (cfg.postExposure);
                _colorAdj.saturation.Override   (cfg.saturation);
                _colorAdj.colorFilter.Override  (cfg.colorFilter);
            }
            if (_vignette != null)
            {
                _vignette.active = true;
                _vignette.intensity.Override  (cfg.vignetteIntensity);
                _vignette.color.Override      (cfg.vignetteColor);
                _vignette.smoothness.Override (cfg.vignetteSmoothness);
            }
            if (_chromatic != null)
            {
                _chromatic.active = cfg.chromaticAberration > 0f;
                _chromatic.intensity.Override(cfg.chromaticAberration);
            }
            if (_lensDistortion != null)
            {
                _lensDistortion.active = Mathf.Abs(cfg.lensDistortionIntensity) > 0.001f;
                _lensDistortion.intensity.Override(cfg.lensDistortionIntensity);
            }
#endif
        }
    }
}
