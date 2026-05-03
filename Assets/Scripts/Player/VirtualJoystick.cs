using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AGNIDAWN.Player
{
    /// <summary>
    /// On-screen virtual joystick for Android touch input.
    /// Placed on the joystick background Image; stick child Image follows finger.
    ///
    /// Calls PlayerController.SetMoveInput(dir) each frame — bypasses InputSystem,
    /// works regardless of whether a PlayerInput component is present.
    ///
    /// Phase 15 — Mobile Bootstrap
    /// </summary>
    public class VirtualJoystick : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("References (set by GameBootstrap)")]
        public PlayerController Target;

        [Header("Visuals")]
        public RectTransform StickHandle;   // inner movable dot
        public float         MaxRadius = 60f;

        // ── State ──────────────────────────────────────────────────────────
        private RectTransform _bgRect;
        private Canvas        _canvas;
        private Vector2       _inputDir;
        private int           _pointerId = -1;

        // ── Accessors ───────────────────────────────────────────────────────
        public Vector2 InputDirection => _inputDir;

        // ──────────────────────────────────────────────────────────────────
        private void Awake()
        {
            _bgRect = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
        }

        private void Update()
        {
            if (Target != null)
                Target.SetMoveInput(_inputDir);
        }

        // ──────────────────────────────────────────────────────────────────
        #region IPointer / IDrag

        public void OnPointerDown(PointerEventData e)
        {
            if (_pointerId != -1) return; // already tracking one finger
            _pointerId = e.pointerId;
            UpdateStick(e.position);
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != _pointerId) return;
            UpdateStick(e.position);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != _pointerId) return;
            _pointerId = -1;
            _inputDir  = Vector2.zero;
            if (StickHandle != null)
                StickHandle.anchoredPosition = Vector2.zero;
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        private void UpdateStick(Vector2 screenPos)
        {
            // Convert screen position to local rect position
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _bgRect, screenPos, _canvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null : _canvas.worldCamera,
                out var localPos);

            // Clamp to max radius
            Vector2 clamped = Vector2.ClampMagnitude(localPos, MaxRadius);
            _inputDir = clamped.magnitude > 0.1f ? clamped.normalized * (clamped.magnitude / MaxRadius) : Vector2.zero;

            if (StickHandle != null)
                StickHandle.anchoredPosition = clamped;
        }
    }
}
