using UnityEngine;
using UnityEngine.InputSystem;
using AGNIDAWN.Core;

namespace AGNIDAWN.Player
{
    /// <summary>
    /// Top-down player movement with dash (Agni Rush).
    /// Uses Unity Input System. Rigidbody2D for physics-based movement.
    /// Designed for 20-minute survival sessions — responsive feel is critical.
    /// Linear: FAI-7
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────
        [Header("Movement")]
        [SerializeField] private float moveSpeed       = 5f;
        [SerializeField] private float acceleration    = 20f;
        [SerializeField] private float deceleration    = 25f;

        [Header("Dash — Agni Rush")]
        [SerializeField] private float dashSpeed       = 18f;
        [SerializeField] private float dashDuration    = 0.15f;
        [SerializeField] private float dashCooldown    = 1.2f;
        [SerializeField] private int   maxDashCharges  = 1;

        [Header("Visuals")]
        [SerializeField] private Transform  spriteRoot;
        [SerializeField] private Animator   animator;

        // ── Components ────────────────────────────────────────────────────
        private Rigidbody2D _rb;

        // ── State ──────────────────────────────────────────────────────────
        private Vector2 _inputDir;
        private Vector2 _velocity;
        private bool    _isDashing;
        private int     _dashCharges;
        private float   _dashCooldownTimer;
        private float   _dashTimer;
        private Vector2 _dashDir;

        // ── Runtime stats (modifiable by boons) ───────────────────────────
        public float MoveSpeedMult { get; set; } = 1f;
        public float DashCooldownMult { get; set; } = 1f;

        // ── Anim hashes ───────────────────────────────────────────────────
        private static readonly int ANIM_SPEED  = Animator.StringToHash("Speed");
        private static readonly int ANIM_DASHING = Animator.StringToHash("IsDashing");

        // ──────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _dashCharges = maxDashCharges;
        }

        private void OnEnable()
        {
            EventBus.On(GameManager.EVT_GAME_PAUSE,  OnPause);
            EventBus.On(GameManager.EVT_GAME_RESUME, OnResume);
        }

        private void OnDisable()
        {
            EventBus.Off(GameManager.EVT_GAME_PAUSE,  OnPause);
            EventBus.Off(GameManager.EVT_GAME_RESUME, OnResume);
        }

        private void Update()
        {
            if (!GameManager.Instance.IsRunning) return;

            HandleDashCooldown();
            UpdateAnimator();
            FlipSprite();
        }

        private void FixedUpdate()
        {
            if (!GameManager.Instance.IsRunning) return;

            if (_isDashing)
            {
                _rb.linearVelocity = _dashDir * dashSpeed;
                _dashTimer -= Time.fixedDeltaTime;
                if (_dashTimer <= 0f) EndDash();
            }
            else
            {
                ApplyMovement();
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Input System Callbacks

        // Called by PlayerInput component via Send Messages
        public void OnMove(InputValue value)
            => _inputDir = value.Get<Vector2>();

        public void OnDash(InputValue value)
        {
            if (value.isPressed) TryDash();
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Movement

        private void ApplyMovement()
        {
            float targetSpeed = moveSpeed * MoveSpeedMult;
            Vector2 targetVel = _inputDir.normalized * targetSpeed;

            float accel = _inputDir.sqrMagnitude > 0.01f ? acceleration : deceleration;
            _velocity = Vector2.MoveTowards(_velocity, targetVel, accel * Time.fixedDeltaTime);
            _rb.linearVelocity = _velocity;
        }

        private void FlipSprite()
        {
            if (_inputDir.x != 0 && spriteRoot != null)
            {
                spriteRoot.localScale = new Vector3(
                    _inputDir.x < 0 ? -1f : 1f,
                    1f, 1f
                );
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Dash

        private void TryDash()
        {
            if (_isDashing || _dashCharges <= 0) return;

            _dashCharges--;
            _dashTimer = dashDuration;
            _isDashing = true;
            _dashDir   = _inputDir.sqrMagnitude > 0.01f
                ? _inputDir.normalized
                : (spriteRoot != null && spriteRoot.localScale.x < 0 ? Vector2.left : Vector2.right);

            EventBus.Emit("OnPlayerDash", transform.position);
            int _enemyLayer = LayerMask.NameToLayer("Enemy");
            if (_enemyLayer >= 0 && _enemyLayer <= 31 &&
                gameObject.layer >= 0 && gameObject.layer <= 31)
                Physics2D.IgnoreLayerCollision(gameObject.layer, _enemyLayer, true);
        }

        private void EndDash()
        {
            _isDashing = false;
            _velocity  = _dashDir * (moveSpeed * MoveSpeedMult);
            int _enemyLayerEnd = LayerMask.NameToLayer("Enemy");
            if (_enemyLayerEnd >= 0 && _enemyLayerEnd <= 31 &&
                gameObject.layer >= 0 && gameObject.layer <= 31)
                Physics2D.IgnoreLayerCollision(gameObject.layer, _enemyLayerEnd, false);
        }

        private void HandleDashCooldown()
        {
            if (_dashCharges >= maxDashCharges) return;

            float cd = dashCooldown * DashCooldownMult;
            _dashCooldownTimer += Time.deltaTime;
            if (_dashCooldownTimer >= cd)
            {
                _dashCooldownTimer = 0f;
                _dashCharges = Mathf.Min(_dashCharges + 1, maxDashCharges);
                EventBus.Emit<int>("OnDashChargeRestored", _dashCharges);
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Animator

        private void UpdateAnimator()
        {
            if (animator == null) return;
            animator.SetFloat(ANIM_SPEED,   _rb.linearVelocity.magnitude);
            animator.SetBool(ANIM_DASHING,  _isDashing);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Event Handlers

        private void OnPause()  => _rb.linearVelocity = Vector2.zero;
        private void OnResume() { }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Public Helpers (for boons)

        public void AddDashCharge()
            => _dashCharges = Mathf.Min(_dashCharges + 1, maxDashCharges + 1);

        public Vector2 GetVelocity() => _rb.linearVelocity;
        public bool    IsDashing()   => _isDashing;

        // ── Touch / virtual-joystick bridge ───────────────────────────────────
        /// <summary>
        /// Called by VirtualJoystick (and any other non-InputSystem source) to
        /// drive movement. Equivalent to what OnMove(InputValue) does.
        /// </summary>
        public void SetMoveInput(Vector2 dir) => _inputDir = dir;

        /// <summary>
        /// Triggers a dash attempt from virtual dash button.
        /// </summary>
        public void TriggerDash() => TryDash();

        #endregion
    }
}
