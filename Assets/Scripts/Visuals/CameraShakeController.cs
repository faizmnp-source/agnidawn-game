using System.Collections;
using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.Visuals
{
    /// <summary>
    /// Positional camera shake driven by AGNIDAWN EventBus events.
    ///
    /// Uses a simple decaying noise offset on the Camera's local position.
    /// For Cinemachine projects, replace <see cref="DoShake"/> with a
    /// CinemachineImpulseSource call — the EventBus wiring stays identical.
    ///
    /// SCENE SETUP:
    ///   Attach to the Main Camera GameObject.
    ///   The component remembers the camera's starting localPosition as its rest origin.
    ///
    /// Shake requests are additive — a new request only overrides the current one
    /// when its magnitude is larger, preventing small hits from interrupting big shakes.
    ///
    /// Linear: FAI-15 (Phase 10)
    /// </summary>
    public class CameraShakeController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("AoE Detonation")]
        [Tooltip("Shake magnitude per unit of AoE radius")]
        [SerializeField, Range(0f, 0.05f)] private float aoeShakePerRadius   = 0.008f;
        [SerializeField, Min(0f)]          private float aoeShakeDuration    = 0.4f;

        [Header("Boss Phase Change")]
        [SerializeField, Range(0f, 0.3f)]  private float bossPhaseShakeMag   = 0.15f;
        [SerializeField, Min(0f)]          private float bossPhaseShakeDur   = 0.6f;

        [Header("Agni Kund Damage")]
        [Tooltip("Shake magnitude = damage * scale / 100")]
        [SerializeField, Range(0f, 0.01f)] private float agniDamageScale     = 0.004f;
        [SerializeField, Min(0f)]          private float agniDamageShakeDur  = 0.2f;

        [Header("Game Over")]
        [SerializeField, Range(0f, 0.5f)]  private float gameOverShakeMag    = 0.35f;
        [SerializeField, Min(0f)]          private float gameOverShakeDur    = 1.8f;

        [Header("Global Cap")]
        [Tooltip("Maximum magnitude regardless of source")]
        [SerializeField, Range(0f, 0.5f)]  private float maxMagnitude        = 0.3f;

        // ── Private ───────────────────────────────────────────────────────────────

        private Vector3   _restPosition;
        private Coroutine _shakeCoroutine;
        private float     _activeMagnitude;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            _restPosition = transform.localPosition;
        }

        private void OnEnable()
        {
            EventBus.On<float, Vector2>  ("OnAoEDetonation",   OnAoEDetonation);
            EventBus.On<string, int>     ("OnBossPhaseChanged", OnBossPhaseChanged);
            EventBus.On<float>           ("OnAgniKundDamaged",  OnAgniKundDamaged);
            EventBus.On                  ("EVT_GAME_OVER",      OnGameOver);
            EventBus.On                  ("EVT_VICTORY",        OnVictory);
        }

        private void OnDisable()
        {
            EventBus.Off<float, Vector2> ("OnAoEDetonation",   OnAoEDetonation);
            EventBus.Off<string, int>    ("OnBossPhaseChanged", OnBossPhaseChanged);
            EventBus.Off<float>          ("OnAgniKundDamaged",  OnAgniKundDamaged);
            EventBus.Off                 ("EVT_GAME_OVER",      OnGameOver);
            EventBus.Off                 ("EVT_VICTORY",        OnVictory);
            // Restore camera on disable so we never leave it offset
            transform.localPosition = _restPosition;
        }

        // ── Event Handlers ────────────────────────────────────────────────────────

        private void OnAoEDetonation(float radius, Vector2 origin)
        {
            float mag = Mathf.Min(radius * aoeShakePerRadius, maxMagnitude);
            TryShake(mag, aoeShakeDuration);
        }

        private void OnBossPhaseChanged(string bossId, int phase)
        {
            TryShake(bossPhaseShakeMag, bossPhaseShakeDur);
        }

        private void OnAgniKundDamaged(float damage)
        {
            float mag = Mathf.Min(damage * agniDamageScale, maxMagnitude * 0.4f);
            if (mag > 0.001f) TryShake(mag, agniDamageShakeDur);
        }

        private void OnGameOver()
        {
            // Always override on game over — it is the loudest possible event
            ForceShake(gameOverShakeMag, gameOverShakeDur);
        }

        private void OnVictory()
        {
            TryShake(0.07f, 0.5f);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Trigger a shake only if <paramref name="magnitude"/> exceeds the current one.
        /// Use this so small hits don't interrupt big boss shakes.
        /// </summary>
        public void TryShake(float magnitude, float duration)
        {
            magnitude = Mathf.Min(magnitude, maxMagnitude);
            if (_shakeCoroutine != null && magnitude <= _activeMagnitude) return;
            ForceShake(magnitude, duration);
        }

        /// <summary>Always starts a new shake, interrupting any current one.</summary>
        public void ForceShake(float magnitude, float duration)
        {
            magnitude = Mathf.Min(magnitude, maxMagnitude);
            if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
            _activeMagnitude = magnitude;
            _shakeCoroutine  = StartCoroutine(DoShake(magnitude, duration));
        }

        // ── Shake Coroutine ───────────────────────────────────────────────────────

        private IEnumerator DoShake(float magnitude, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float decay = 1f - Mathf.SmoothStep(0f, 1f, elapsed / duration);
                float ox = Random.Range(-1f, 1f) * magnitude * decay;
                float oy = Random.Range(-1f, 1f) * magnitude * decay;
                transform.localPosition = _restPosition + new Vector3(ox, oy, 0f);
                yield return null;
            }
            transform.localPosition = _restPosition;
            _activeMagnitude = 0f;
            _shakeCoroutine  = null;
        }
    }
}
