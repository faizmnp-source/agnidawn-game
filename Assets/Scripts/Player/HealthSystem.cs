using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.Player
{
    /// <summary>
    /// Shared health component used by both the player and enemies.
    /// Handles damage, healing, invincibility frames, and death events.
    /// Linear: FAI-7
    /// </summary>
    public class HealthSystem : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────
        [Header("Health")]
        [SerializeField] private float maxHealth        = 100f;
        [SerializeField] private float startingHealth   = -1f; // -1 = use maxHealth

        [Header("Invincibility Frames")]
        [SerializeField] private bool  hasIFrames       = false;
        [SerializeField] private float iFrameDuration   = 0.5f;

        [Header("Owner tag — for event context")]
        [SerializeField] private string ownerTag        = "Player"; // "Player" or "Enemy"

        // ── State ──────────────────────────────────────────────────────────
        private float _currentHealth;
        private float _iFrameTimer;
        private bool  _isDead;

        // ── Public accessors ───────────────────────────────────────────────
        public float CurrentHealth => _currentHealth;
        public float MaxHealth     => maxHealth;
        public float HealthPercent => _currentHealth / maxHealth;
        public bool  IsDead        => _isDead;
        public bool  IsInvincible  => hasIFrames && _iFrameTimer > 0f;

        // ── Boon modifiers ─────────────────────────────────────────────────
        public float DamageReductionMult { get; set; } = 1f; // 0.8 = 20% less damage
        public float HealingMult         { get; set; } = 1f;
        public float MaxHealthBonus      { get; set; } = 0f;

        // ──────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            float hp = startingHealth < 0f ? maxHealth : startingHealth;
            _currentHealth = hp + MaxHealthBonus;
        }

        private void Update()
        {
            if (_iFrameTimer > 0f)
                _iFrameTimer -= Time.deltaTime;
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Public API

        /// Apply damage. Returns actual damage dealt (after reduction).
        public float TakeDamage(float rawDamage, GameObject source = null)
        {
            if (_isDead || IsInvincible) return 0f;

            float actual = Mathf.Max(0f, rawDamage * DamageReductionMult);
            _currentHealth = Mathf.Max(0f, _currentHealth - actual);

            EventBus.Emit<float>($"On{ownerTag}Damaged", actual);
            EventBus.Emit<float, GameObject>($"On{ownerTag}DamagedWithSource", actual, source);

            if (hasIFrames) _iFrameTimer = iFrameDuration;

            if (_currentHealth <= 0f) Die(source);

            return actual;
        }

        /// Heal by amount. Returns actual healing applied.
        public float Heal(float amount)
        {
            if (_isDead) return 0f;

            float effective = amount * HealingMult;
            float before = _currentHealth;
            _currentHealth = Mathf.Min(_currentHealth + effective, maxHealth + MaxHealthBonus);
            float actual = _currentHealth - before;

            if (actual > 0f)
                EventBus.Emit<float>($"On{ownerTag}Healed", actual);

            return actual;
        }

        /// Full heal to max
        public void FullHeal() => Heal(maxHealth + MaxHealthBonus);

        /// Instant kill (bypass damage reduction + iFrames)
        public void InstantKill(GameObject source = null)
        {
            if (_isDead) return;
            _currentHealth = 0f;
            Die(source);
        }

        /// Add permanent max health (from boons)
        public void AddMaxHealth(float bonus)
        {
            MaxHealthBonus += bonus;
            _currentHealth += bonus; // also top up current
        }

        /// Grant temporary invincibility
        public void GrantInvincibility(float duration)
        {
            hasIFrames    = true;
            _iFrameTimer  = Mathf.Max(_iFrameTimer, duration);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Private

        private void Die(GameObject killer = null)
        {
            if (_isDead) return;
            _isDead = true;

            EventBus.Emit<GameObject>($"On{ownerTag}Died", killer);

            if (ownerTag == "Player")
                GameManager.Instance?.TriggerGameOver();
            else
                gameObject.SetActive(false);
        }

        #endregion
    }
}
