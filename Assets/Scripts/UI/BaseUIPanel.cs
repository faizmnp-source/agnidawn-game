using UnityEngine;

namespace AGNIDAWN.UI
{
    /// <summary>
    /// Abstract base for every UI panel in AGNIDAWN.
    ///
    /// Panels are never directly activated by game systems.
    /// UIManager owns the screen stack and calls Show/Hide.
    /// Each panel subscribes to EventBus events it cares about.
    ///
    /// Linear: FAI-14
    /// </summary>
    public abstract class BaseUIPanel : MonoBehaviour
    {
        [Header("Panel Config")]
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected float       fadeSpeed = 6f;

        private bool _visible;
        private bool _fading;

        public bool IsVisible => _visible;

        // ──────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        protected virtual void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            // Start hidden
            SetAlpha(0f);
            gameObject.SetActive(false);
            _visible = false;
        }

        protected virtual void Update()
        {
            if (!_fading) return;

            float target = _visible ? 1f : 0f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, target, fadeSpeed * Time.unscaledDeltaTime);

            if (Mathf.Approximately(canvasGroup.alpha, target))
            {
                _fading = false;
                if (!_visible) gameObject.SetActive(false);
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Public API

        public virtual void Show()
        {
            gameObject.SetActive(true);
            _visible = true;
            _fading  = true;
            canvasGroup.interactable   = true;
            canvasGroup.blocksRaycasts = true;
            OnShow();
        }

        public virtual void Hide()
        {
            _visible = false;
            _fading  = true;
            canvasGroup.interactable   = false;
            canvasGroup.blocksRaycasts = false;
            OnHide();
        }

        public void ShowImmediate()
        {
            gameObject.SetActive(true);
            SetAlpha(1f);
            _visible = true;
            _fading  = false;
            canvasGroup.interactable   = true;
            canvasGroup.blocksRaycasts = true;
            OnShow();
        }

        public void HideImmediate()
        {
            SetAlpha(0f);
            gameObject.SetActive(false);
            _visible = false;
            _fading  = false;
            canvasGroup.interactable   = false;
            canvasGroup.blocksRaycasts = false;
            OnHide();
        }

        /// <summary>Allow external callers (e.g. UIManager stack) to toggle interactivity.</summary>
        public void SetInteractable(bool value)
        {
            if (canvasGroup != null) canvasGroup.interactable = value;
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Virtual Hooks

        /// Called when panel becomes visible — subscribe to events here.
        protected virtual void OnShow() { }

        /// Called when panel starts hiding — unsubscribe from events here.
        protected virtual void OnHide() { }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        private void SetAlpha(float a)
        {
            if (canvasGroup != null)
                canvasGroup.alpha = a;
        }
    }
}
