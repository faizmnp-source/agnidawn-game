using System.Collections;
using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.VFX
{
    /// <summary>
    /// Ritual mandala circle effect for Brahmastra and Pashupatastra cast events.
    ///
    /// A mandala appears under the player when a special Astra is fired
    /// (<c>OnAstraSpecial</c> event), rotates briefly, then fades.
    ///
    /// The effect uses a SpriteRenderer with a mandala sprite that:
    ///   1. Scales up from zero in ~0.15s (slam in)
    ///   2. Rotates for the hold duration
    ///   3. Fades out over ~0.4s
    ///
    /// SCENE SETUP:
    ///   1. Create prefab "MandalaFX" with a SpriteRenderer (mandala circle art).
    ///   2. Attach this script.
    ///   3. Also attach AutoReturnToPool with the appropriate pool key.
    ///   4. Register under the pool key matching VFXEventData.castPoolKey
    ///      (e.g. "VFX_BrahmaMandala", "VFX_PashupataMandala").
    ///
    /// Designers: swap the sprite in the SpriteRenderer to change the mandala art.
    ///
    /// Linear: FAI-16 (Phase 11)
    /// </summary>
    public class MandalaFXController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [SerializeField] private SpriteRenderer mandalaRenderer;

        [Header("Astra Filter")]
        [Tooltip("Only show for these astraIds. Leave empty to show for all OnAstraSpecial events.")]
        [SerializeField] private string[] triggerAstraIds = { "Brahmastra", "Pashupatastra" };

        [Header("Animation")]
        [SerializeField, Min(0.05f)] private float scaleInDuration  = 0.15f;
        [SerializeField, Min(0.1f)]  private float holdDuration     = 0.6f;
        [SerializeField, Min(0.1f)]  private float fadeOutDuration  = 0.4f;
        [SerializeField, Min(0f)]    private float maxScale         = 2.5f;
        [SerializeField]             private float rotationSpeed    = 45f;   // deg/s

        [Header("Visual")]
        [SerializeField] private Color brahmastraColor    = new Color(1f, 0.85f, 0.4f, 0.85f);
        [SerializeField] private Color pashupatastraColor = new Color(0.6f, 0.2f, 1.0f, 0.85f);

        // ── Private ───────────────────────────────────────────────────────────────

        private Coroutine _playCoroutine;
        private bool      _subscribed;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void OnEnable()
        {
            EventBus.On<string>("OnAstraSpecial", OnAstraSpecial);
            _subscribed = true;

            // Hide until triggered
            if (mandalaRenderer != null) mandalaRenderer.color = Color.clear;
            transform.localScale = Vector3.zero;
        }

        private void OnDisable()
        {
            if (_subscribed)
            {
                EventBus.Off<string>("OnAstraSpecial", OnAstraSpecial);
                _subscribed = false;
            }
            if (_playCoroutine != null) StopCoroutine(_playCoroutine);
        }

        // ── Event Handler ─────────────────────────────────────────────────────────

        private void OnAstraSpecial(string astraId)
        {
            // Check if this astraId is in our trigger list (or list is empty = all)
            if (triggerAstraIds != null && triggerAstraIds.Length > 0)
            {
                bool match = false;
                foreach (var id in triggerAstraIds)
                    if (string.Equals(id, astraId, System.StringComparison.OrdinalIgnoreCase))
                    { match = true; break; }
                if (!match) return;
            }

            // Snap to player position
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) transform.position = player.transform.position;

            Color color = string.Equals(astraId, "Brahmastra", System.StringComparison.OrdinalIgnoreCase)
                ? brahmastraColor : pashupatastraColor;

            if (_playCoroutine != null) StopCoroutine(_playCoroutine);
            _playCoroutine = StartCoroutine(PlayMandala(color));
        }

        // ── Coroutine ─────────────────────────────────────────────────────────────

        private IEnumerator PlayMandala(Color baseColor)
        {
            if (mandalaRenderer == null) yield break;

            // ── Scale in ──────────────────────────────────────────────────────────
            float elapsed = 0f;
            while (elapsed < scaleInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / scaleInDuration));
                transform.localScale        = Vector3.one * Mathf.Lerp(0f, maxScale, t);
                mandalaRenderer.color       = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * t);
                transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
                yield return null;
            }
            transform.localScale  = Vector3.one * maxScale;
            mandalaRenderer.color = baseColor;

            // ── Hold ──────────────────────────────────────────────────────────────
            elapsed = 0f;
            while (elapsed < holdDuration)
            {
                elapsed += Time.deltaTime;
                transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
                yield return null;
            }

            // ── Fade out ──────────────────────────────────────────────────────────
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeOutDuration));
                float alpha = Mathf.Lerp(baseColor.a, 0f, t);
                mandalaRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                transform.Rotate(Vector3.forward, rotationSpeed * 0.5f * Time.deltaTime);
                yield return null;
            }

            mandalaRenderer.color = Color.clear;
            transform.localScale  = Vector3.zero;
            gameObject.SetActive(false);
        }
    }
}
