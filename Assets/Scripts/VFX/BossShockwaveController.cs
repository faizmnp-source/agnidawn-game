using System.Collections;
using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.VFX
{
    /// <summary>
    /// Procedural shockwave ring effect for boss phase transitions.
    ///
    /// Uses a SpriteRenderer (or LineRenderer circle) that rapidly expands and
    /// fades — no particle system required.  Place a simple circle sprite on the
    /// BossShockwave prefab, then pool it under key "VFX_*Shockwave".
    ///
    /// Each boss has a unique shockwave colour configured via Inspector.
    /// OnBossPhaseChanged drives the effect; VFXManager spawns the prefab
    /// and this component plays itself then disables its GameObject.
    ///
    /// SCENE SETUP:
    ///   1. Create a prefab "BossShockwavePrefab" with a SpriteRenderer (circle).
    ///   2. Attach this script.
    ///   3. Add an AutoReturnToPool component so it returns to pool after play.
    ///   4. Register the prefab in ObjectPool under the relevant "VFX_*Shockwave" key.
    ///
    /// Linear: FAI-16 (Phase 11)
    /// </summary>
    public class BossShockwaveController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [SerializeField] private SpriteRenderer shockwaveRenderer;

        [Header("Animation")]
        [SerializeField, Min(0.01f)] private float startRadius    = 0.5f;
        [SerializeField, Min(0.1f)]  private float endRadius      = 12f;
        [SerializeField, Min(0.1f)]  private float duration       = 0.55f;

        [Header("Visual")]
        [SerializeField] private Color shockwaveColor              = new Color(1f, 0.6f, 0.1f, 0.9f);
        [SerializeField, Range(0f, 1f)] private float peakAlpha   = 0.85f;
        [SerializeField, Range(0f, 1f)] private float endAlpha    = 0f;

        // ── Private ───────────────────────────────────────────────────────────────

        private Coroutine _playCoroutine;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void OnEnable()
        {
            // Every time this GameObject is activated (pulled from pool), play immediately
            if (_playCoroutine != null) StopCoroutine(_playCoroutine);
            _playCoroutine = StartCoroutine(PlayShockwave());
        }

        private void OnDisable()
        {
            if (_playCoroutine != null)
            {
                StopCoroutine(_playCoroutine);
                _playCoroutine = null;
            }
        }

        // ── Shockwave Playback ────────────────────────────────────────────────────

        private IEnumerator PlayShockwave()
        {
            if (shockwaveRenderer == null) yield break;

            shockwaveRenderer.enabled = true;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

                float radius = Mathf.Lerp(startRadius, endRadius, t);
                transform.localScale = Vector3.one * radius * 2f;   // diameter

                float alpha = Mathf.Lerp(peakAlpha, endAlpha, t);
                shockwaveRenderer.color = new Color(
                    shockwaveColor.r, shockwaveColor.g, shockwaveColor.b, alpha);

                yield return null;
            }

            shockwaveRenderer.enabled = false;
            gameObject.SetActive(false);   // AutoReturnToPool sees this and returns to pool
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Override colour for this shockwave instance before activation.</summary>
        public void SetColor(Color color) => shockwaveColor = color;
    }

    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Attach to any pooled VFX prefab.  When the ParticleSystem finishes
    /// (or the GameObject is deactivated), returns itself to ObjectPool.
    ///
    /// Works for both particle-based VFX and the BossShockwaveController.
    /// </summary>
    public class AutoReturnToPool : MonoBehaviour
    {
        [SerializeField, Tooltip("Must match the ObjectPool key this prefab was registered under.")]
        private string poolKey;

        private ParticleSystem _ps;

        private void Awake()  => _ps = GetComponent<ParticleSystem>();

        private void OnEnable()
        {
            if (_ps == null)
            {
                // Non-particle VFX (e.g. shockwave) — return handled by SetActive(false) flow
                // Nothing to do here; OnDisable handles return.
            }
        }

        private void Update()
        {
            // Particle-based: wait until stopped playing
            if (_ps != null && !_ps.IsAlive())
            {
                Return();
            }
        }

        private void OnDisable()
        {
            // Called when SetActive(false) — return to pool
            Return();
        }

        private void Return()
        {
            if (ObjectPool.Instance != null && !string.IsNullOrEmpty(poolKey))
                ObjectPool.Instance.Return(poolKey, gameObject);
        }
    }
}
