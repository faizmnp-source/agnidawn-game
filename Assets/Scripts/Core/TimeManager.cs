using System.Collections;
using UnityEngine;

namespace AGNIDAWN.Core
{
    /// <summary>
    /// Centralises all time-scale manipulation: pause, slow-motion, and
    /// the Agni Kund "divine surge" effect. Prevents multiple systems from
    /// fighting over Time.timeScale.
    ///
    /// Linear: FAI-6
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────
        public static TimeManager Instance { get; private set; }

        // ── State ──────────────────────────────────────────────────────────
        private float  _baseTimeScale     = 1f;
        private float  _currentTimeScale  = 1f;
        private bool   _isPaused          = false;
        private Coroutine _slowMoRoutine;

        public bool  IsPaused       => _isPaused;
        public float CurrentScale   => _currentTimeScale;

        // ──────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Public API

        /// Set the normal game speed (1 = real-time, 0.5 = half speed)
        public void SetTimeScale(float scale)
        {
            if (_isPaused) { _baseTimeScale = scale; return; }
            _baseTimeScale    = scale;
            _currentTimeScale = scale;
            Apply();
        }

        /// Hard pause (Time.timeScale = 0)
        public void SetPaused(bool paused)
        {
            _isPaused = paused;
            _currentTimeScale = paused ? 0f : _baseTimeScale;
            Apply();
            EventBus.Emit<bool>("OnTimePauseChanged", paused);
        }

        /// Slow-motion burst: ramp down to <targetScale> over <rampTime>,
        /// hold for <duration>, then ramp back up.
        public void TriggerSlowMo(float targetScale = 0.3f, float rampTime = 0.1f, float holdDuration = 1.5f)
        {
            if (_isPaused) return;
            if (_slowMoRoutine != null) StopCoroutine(_slowMoRoutine);
            _slowMoRoutine = StartCoroutine(SlowMoRoutine(targetScale, rampTime, holdDuration));
        }

        /// Stop any active slow-mo and snap back to base speed
        public void CancelSlowMo()
        {
            if (_slowMoRoutine != null) { StopCoroutine(_slowMoRoutine); _slowMoRoutine = null; }
            SetTimeScale(_baseTimeScale);
        }

        /// One-frame freeze (hit-stop feel for boss hits)
        public void HitStop(float duration = 0.05f)
        {
            if (_isPaused) return;
            if (_slowMoRoutine != null) StopCoroutine(_slowMoRoutine);
            _slowMoRoutine = StartCoroutine(HitStopRoutine(duration));
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Private

        private void Apply()
        {
            Time.timeScale = _currentTimeScale;
            // Keep fixedDeltaTime proportional for physics stability
            Time.fixedDeltaTime = 0.02f * Mathf.Clamp(_currentTimeScale, 0.05f, 2f);
        }

        private IEnumerator SlowMoRoutine(float target, float rampTime, float hold)
        {
            // Ramp down
            float start = _currentTimeScale;
            float t = 0f;
            while (t < rampTime)
            {
                t += Time.unscaledDeltaTime;
                _currentTimeScale = Mathf.Lerp(start, target, t / rampTime);
                Apply();
                yield return null;
            }

            // Hold
            yield return new WaitForSecondsRealtime(hold);

            // Ramp back
            t = 0f;
            start = _currentTimeScale;
            while (t < rampTime)
            {
                t += Time.unscaledDeltaTime;
                _currentTimeScale = Mathf.Lerp(start, _baseTimeScale, t / rampTime);
                Apply();
                yield return null;
            }

            _currentTimeScale = _baseTimeScale;
            Apply();
            _slowMoRoutine = null;
        }

        private IEnumerator HitStopRoutine(float duration)
        {
            _currentTimeScale = 0f;
            Apply();
            yield return new WaitForSecondsRealtime(duration);
            _currentTimeScale = _baseTimeScale;
            Apply();
            _slowMoRoutine = null;
        }

        #endregion
    }
}
