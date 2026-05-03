using System.Collections;
using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.VFX
{
    /// <summary>
    /// Subtle divine aura particle effect around the player that activates
    /// at high Agni tiers (4 and 5) and fades at lower tiers.
    ///
    /// At Tier 4: soft golden shimmer (Agni blessing active)
    /// At Tier 5: blazing divine corona — white-gold motes orbit the player
    ///
    /// SCENE SETUP:
    ///   Attach to the Player GameObject.
    ///   Assign the auraPS (a looping ParticleSystem child of the player).
    ///   Optionally assign a secondary crownPS for the tier-5 corona.
    ///
    /// Linear: FAI-16 (Phase 11)
    /// </summary>
    public class PlayerDivineAura : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [SerializeField] private ParticleSystem auraPS;
        [SerializeField] private ParticleSystem crownPS;

        [Header("Tier 4 — Agni Blessing")]
        [SerializeField] private Color tier4AuraColor      = new Color(1f, 0.82f, 0.2f, 0.7f);
        [SerializeField] private float tier4EmissionRate   = 12f;
        [SerializeField] private float tier4StartSize      = 0.25f;

        [Header("Tier 5 — Divine Blaze")]
        [SerializeField] private Color tier5AuraColor      = new Color(1f, 0.97f, 0.8f, 0.9f);
        [SerializeField] private float tier5EmissionRate   = 30f;
        [SerializeField] private float tier5StartSize      = 0.4f;
        [SerializeField] private float tier5CrownEmission  = 20f;

        [Header("Fade timing")]
        [SerializeField, Min(0.2f)] private float fadeInDuration  = 0.8f;
        [SerializeField, Min(0.2f)] private float fadeOutDuration = 1.2f;

        // ── Private ───────────────────────────────────────────────────────────────

        private int       _currentTier;
        private Coroutine _fadeCoroutine;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void OnEnable()
        {
            EventBus.On<int, int>("OnAgniTierChanged", OnAgniTierChanged);
            EventBus.On          ("EVT_GAME_OVER",     OnGameEnded);
            EventBus.On          ("EVT_VICTORY",       OnGameEnded);
        }

        private void OnDisable()
        {
            EventBus.Off<int, int>("OnAgniTierChanged", OnAgniTierChanged);
            EventBus.Off          ("EVT_GAME_OVER",     OnGameEnded);
            EventBus.Off          ("EVT_VICTORY",       OnGameEnded);
            StopAura();
        }

        private void Start()
        {
            StopAura();         // off by default; event will enable if tier >= 4
        }

        // ── Event Handlers ────────────────────────────────────────────────────────

        private void OnAgniTierChanged(int newTier, int prevTier)
        {
            _currentTier = newTier;
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);

            if (newTier >= 5)
                _fadeCoroutine = StartCoroutine(FadeToTier5());
            else if (newTier >= 4)
                _fadeCoroutine = StartCoroutine(FadeToTier4());
            else
                _fadeCoroutine = StartCoroutine(FadeOut());
        }

        private void OnGameEnded()
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeOut());
        }

        // ── Coroutines ────────────────────────────────────────────────────────────

        private IEnumerator FadeToTier4()
        {
            if (crownPS != null && crownPS.isPlaying) crownPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            if (auraPS == null) yield break;
            if (!auraPS.isPlaying) auraPS.Play();

            var main = auraPS.main;
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / fadeInDuration);
                var em = auraPS.emission;
                em.rateOverTime = Mathf.Lerp(0f, tier4EmissionRate, t);
                var c = main.startColor;
                c.color = Color.Lerp(Color.clear, tier4AuraColor, t);
                main.startColor = c;
                main.startSize  = new ParticleSystem.MinMaxCurve(Mathf.Lerp(0f, tier4StartSize, t));
                yield return null;
            }
        }

        private IEnumerator FadeToTier5()
        {
            if (auraPS == null) yield break;
            if (!auraPS.isPlaying) auraPS.Play();

            var main = auraPS.main;
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / fadeInDuration);
                var em = auraPS.emission;
                em.rateOverTime = Mathf.Lerp(tier4EmissionRate, tier5EmissionRate, t);
                var c = main.startColor;
                c.color = Color.Lerp(tier4AuraColor, tier5AuraColor, t);
                main.startColor = c;
                main.startSize  = new ParticleSystem.MinMaxCurve(Mathf.Lerp(tier4StartSize, tier5StartSize, t));
                yield return null;
            }

            // Activate crown corona for tier 5
            if (crownPS != null)
            {
                crownPS.Play();
                var crownEm = crownPS.emission;
                crownEm.rateOverTime = tier5CrownEmission;
            }
        }

        private IEnumerator FadeOut()
        {
            if (crownPS != null && crownPS.isPlaying)
                crownPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            if (auraPS == null || !auraPS.isPlaying) yield break;

            var main    = auraPS.main;
            float startRate = auraPS.emission.rateOverTime.constant;
            float elapsed   = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / fadeOutDuration);
                var em = auraPS.emission;
                em.rateOverTime = Mathf.Lerp(startRate, 0f, t);
                yield return null;
            }
            auraPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void StopAura()
        {
            if (auraPS  != null) auraPS.Stop(true,  ParticleSystemStopBehavior.StopEmittingAndClear);
            if (crownPS != null) crownPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
