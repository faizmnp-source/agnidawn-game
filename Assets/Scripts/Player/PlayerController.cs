using UnityEngine;
using UnityEngine.InputSystem;
using AGNIDAWN.Core;

namespace AGNIDAWN.Player
{
    /// <summary>
    /// Side-view player movement with dash (Agni Rush).
    /// Uses Unity Input System. Rigidbody2D with gravity for grounded platformer feel.
    /// Left/right input controls horizontal velocity; gravity handles vertical.
    ///
    /// Ground check: Physics2D.OverlapCircle from feet position against ground layer.
    /// Designed for 20-minute survival sessions — responsive feel is critical.
    /// Linear: FAI-7
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────
        [Header("Movement")]
        [SerializeField] private float moveSpeed       = 5f;
        [SerializeField] private float acceleration    = 22f;
        [SerializeField] private float deceleration    = 28f;

        [Header("Gravity & Ground")]
        [SerializeField] private float gravityScale    = 3f;
        // Player origin is now AT feet level (AgniRiggedCharacter.HIP_Y=3.77 lifts boots to Y=0).
        // Small negative offset detects ground just under feet.
        [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.05f);
        [SerializeField] private float   groundCheckRadius = 0.20f;

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
        private float   _velocityX;        // Only X is manually controlled
        private bool    _isDashing;
        private int     _dashCharges;
        private float   _dashCooldownTimer;
        private float   _dashTimer;
        private Vector2 _dashDir;
        private bool    _isGrounded;

        // ── Ground layer mask (everything except Enemy + Ignore Raycast) ──
        private int _groundMask;

        // ── Runtime stats (modifiable by boons) ───────────────────────────
        public float MoveSpeedMult { get; set; } = 1f;
        public float DashCooldownMult { get; set; } = 1f;

        // ── Anim hashes ───────────────────────────────────────────────────
        private static readonly int ANIM_SPEED   = Animator.StringToHash("Speed");
        private static readonly int ANIM_DASHING = Animator.StringToHash("IsDashing");

        // ── Public read ───────────────────────────────────────────────────
        public bool IsGrounded => _isGrounded;

        // ──────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = gravityScale;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _dashCharges = maxDashCharges;

            // Ground mask: everything except Enemy and Ignore Raycast layers
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            int ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");
            _groundMask = ~0; // all layers
            if (enemyLayer >= 0)   _groundMask &= ~(1 << enemyLayer);
            if (ignoreLayer >= 0)  _groundMask &= ~(1 << ignoreLayer);
            _groundMask &= ~(1 << gameObject.layer); // exclude own layer
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

            CheckGrounded();
            HandleDashCooldown();
            UpdateAnimator();
            FlipSprite();
        }

        private void FixedUpdate()
        {
            if (!GameManager.Instance.IsRunning) return;

            if (_isDashing)
            {
                // During dash: horizontal only, preserve gravity
                _rb.linearVelocity = new Vector2(_dashDir.x * dashSpeed, _rb.linearVelocity.y);
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
        #region Ground Check

        private void CheckGrounded()
        {
            Vector2 checkPos = (Vector2)transform.position + groundCheckOffset;
            _isGrounded = Physics2D.OverlapCircle(checkPos, groundCheckRadius, _groundMask) != null;
        }

        // Draw ground check in editor so it's easy to tune
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Vector2 pos  = (Vector2)transform.position + groundCheckOffset;
            Gizmos.DrawWireSphere(pos, groundCheckRadius);
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
            // Only horizontal — gravity handles vertical
            float targetVelX = _inputDir.x * targetSpeed;

            float accel = Mathf.Abs(_inputDir.x) > 0.01f ? acceleration : deceleration;
            _velocityX = Mathf.MoveTowards(_velocityX, targetVelX, accel * Time.fixedDeltaTime);

            // Keep physics-calculated Y (gravity + bounce), only override X
            _rb.linearVelocity = new Vector2(_velocityX, _rb.linearVelocity.y);
        }

        private void FlipSprite()
        {
            // AgniRiggedCharacter handles its own flip from Rigidbody2D velocity.
            // This also flips spriteRoot if one is assigned in Inspector.
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
            // Dash in facing direction (horizontal only)
            float dirX = _inputDir.x != 0 ? Mathf.Sign(_inputDir.x)
                       : (spriteRoot != null && spriteRoot.localScale.x < 0 ? -1f : 1f);
            _dashDir = new Vector2(dirX, 0f);

            EventBus.Emit("OnPlayerDash", transform.position);
            int _enemyLayer = LayerMask.NameToLayer("Enemy");
            if (_enemyLayer >= 0 && _enemyLayer <= 31 &&
                gameObject.layer >= 0 && gameObject.layer <= 31)
                Physics2D.IgnoreLayerCollision(gameObject.layer, _enemyLayer, true);
        }

        private void EndDash()
        {
            _isDashing = false;
            _velocityX = _dashDir.x * (moveSpeed * MoveSpeedMult);
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
            animator.SetFloat(ANIM_SPEED,   Mathf.Abs(_rb.linearVelocity.x));
            animator.SetBool(ANIM_DASHING,  _isDashing);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Event Handlers

        private void OnPause()
        {
            _rb.linearVelocity = Vector2.zero;
            _velocityX = 0f;
        }
        private void OnResume() { }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Public Helpers (for boons)

        public void AddDashCharge()
            => _dashCharges = Mathf.Min(_dashCharges + 1, maxDashCharges + 1);

        public Vector2 GetVelocity() => _rb.linearVelocity;
        public bool    IsDashing()   => _isDashing;

        // ── Touch / virtual-joystick bridge ──────────────────────────────
        /// <summary>
        /// Called by VirtualJoystick. Only X component is used in side-view mode.
        /// </summary>
        public void SetMoveInput(Vector2 dir) => _inputDir = dir;

        /// <summary>Triggers a dash attempt from virtual dash button.</summary>
        public void TriggerDash() => TryDash();

        #endregion
    }
}
