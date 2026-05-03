using System.Collections;
using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.Gameplay
{
    /// <summary>
    /// The Agni Kund — sacred fire altar at the heart of AGNIDAWN.
    /// The player must protect it from enemies for 20 minutes.
    ///
    /// 5-Tier Agni Dial:
    ///   Tier 5 (MAHAAGNI)   — 81-100% HP : max fire, player gets damage/speed aura
    ///   Tier 4 (PRABHAVA)   — 61-80%  HP : strong fire, mild player aura
    ///   Tier 3 (SADHARAN)   — 41-60%  HP : normal flame
    ///   Tier 2 (KSHEEN)     — 21-40%  HP : dim fire, enemies move faster
    ///   Tier 1 (MRITYUPRAYA)— 0-20%   HP : near-death, vision darkens, panic music
    ///
    /// Each tier triggers URP post-processing and FMOD audio snapshots.
    /// Linear: FAI-9
    /// </summary>
    public class AgniKund : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────
        public static AgniKund Instance { get; private set; }

        // ── Inspector ──────────────────────────────────────────────────────
        [Header("Health")]
        [SerializeField] private float maxHealth          = 500f;
        [SerializeField] private float baseDrainPerSecond = 2f;  // idle drain
        [SerializeField] private float enemyDrainPerHit   = 15f; // when enemy attacks it

        [Header("Drain Scaling")]
        [SerializeField] private float drainScaleAtMinute20 = 5f; // drain ramps up over time

        [Header("Aura Range")]
        [SerializeField] private float auraRadius         = 4f;   // player gets buffs inside

        [Header("Tier Thresholds")]
        [SerializeField] private float tier5Threshold     = 0.81f;
        [SerializeField] private float tier4Threshold     = 0.61f;
        [SerializeField] private float tier3Threshold     = 0.41f;
        [SerializeField] private float tier2Threshold     = 0.21f;

        [Header("VFX")]
        [SerializeField] private ParticleSystem flameParticles;
        [SerializeField] private Light          fireLight;   // standard Unity point light (URP 3D)

        // ── State ──────────────────────────────────────────────────────────
        private float _currentHealth;
        private int   _currentTier = 5;
        private bool  _isDead;

        public float HealthPercent => _currentHealth / maxHealth;
        public int   CurrentTier   => _currentTier;
        public bool  IsDead        => _isDead;

        // ── Tier data ─────────────────────────────────────────────────────
        public static readonly string[] TierNames =
        {
            "",                   // 0 — unused
            "MRITYUPRAYA",        // 1 — near death
            "KSHEEN",             // 2 — weakening
            "SADHARAN",           // 3 — normal
            "PRABHAVA",           // 4 — strong
            "MAHAAGNI"            // 5 — maximum
        };

        // ──────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _currentHealth = maxHealth;
        }

        private void OnEnable()
        {
            EventBus.On(GameManager.EVT_GAME_START,  OnGameStart);
            EventBus.On(GameManager.EVT_GAME_OVER,   OnGameStop);
            EventBus.On(GameManager.EVT_VICTORY,     OnGameStop);
        }

        private void OnDisable()
        {
            EventBus.Off(GameManager.EVT_GAME_START, OnGameStart);
            EventBus.Off(GameManager.EVT_GAME_OVER,  OnGameStop);
            EventBus.Off(GameManager.EVT_VICTORY,    OnGameStop);
        }

        private void Update()
        {
            if (_isDead || !GameManager.Instance.IsRunning) return;

            DrainOverTime();
            UpdateTier();
            UpdateAura();
            UpdateVFX();
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Public API

        /// Enemies call this when they attack the Agni Kund
        public void TakeDamage(float amount)
        {
            if (_isDead) return;

            _currentHealth = Mathf.Max(0f, _currentHealth - amount);
            EventBus.Emit<float>("OnAgniKundDamaged", amount);

            if (_currentHealth <= 0f) Die();
        }

        /// Boons / player actions can restore the flame
        public void Restore(float amount)
        {
            if (_isDead) return;
            float before = _currentHealth;
            _currentHealth = Mathf.Min(_currentHealth + amount, maxHealth);
            float actual = _currentHealth - before;
            if (actual > 0f)
                EventBus.Emit<float>("OnAgniKundRestored", actual);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Private

        private void DrainOverTime()
        {
            float minutes = GameManager.Instance.ElapsedTime / 60f;
            float t       = Mathf.Clamp01(minutes / 20f);
            float drain   = Mathf.Lerp(baseDrainPerSecond, drainScaleAtMinute20, t);

            // Tier 2 and 1 drain faster — flame is struggling
            if (_currentTier <= 2) drain *= 1.5f;

            TakeDamage(drain * Time.deltaTime);
        }

        private void UpdateTier()
        {
            float pct = HealthPercent;
            int newTier;

            if      (pct >= tier5Threshold) newTier = 5;
            else if (pct >= tier4Threshold) newTier = 4;
            else if (pct >= tier3Threshold) newTier = 3;
            else if (pct >= tier2Threshold) newTier = 2;
            else                            newTier = 1;

            if (newTier != _currentTier)
            {
                int prev   = _currentTier;
                _currentTier = newTier;
                EventBus.Emit<int, int>("OnAgniTierChanged", newTier, prev);
                ApplyTierEffects(newTier);
            }
        }

        private void ApplyTierEffects(int tier)
        {
            // Broadcast tier-based player modifier
            float speedBuff   = tier == 5 ? 0.2f : tier == 4 ? 0.1f : 0f;
            float damageBuff  = tier == 5 ? 0.25f : tier == 4 ? 0.1f : 0f;
            float enemySpeedBuff = tier <= 2 ? 0.25f : 0f; // enemies get faster as fire dims

            EventBus.Emit<float, float>("OnAgniAuraChanged", speedBuff, damageBuff);
            EventBus.Emit<float>("OnEnemySpeedModified", enemySpeedBuff);
            EventBus.Emit<int>("OnAgniTierApplied", tier);

            Debug.Log($"[AgniKund] Tier {tier}: {TierNames[tier]} — HP {HealthPercent:P0}");
        }

        private void UpdateAura()
        {
            // Check if player is in aura range and apply tier buffs
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            bool inRange = Vector2.Distance(transform.position, player.transform.position) <= auraRadius;
            EventBus.Emit<bool>("OnPlayerInAgniAura", inRange);
        }

        private void UpdateVFX()
        {
            if (flameParticles == null && fireLight == null) return;

            float intensity = HealthPercent;

            // Scale particles with tier
            if (flameParticles != null)
            {
                var main = flameParticles.main;
                main.startSizeMultiplier = Mathf.Lerp(0.3f, 2.5f, intensity);
                var emission = flameParticles.emission;
                emission.rateOverTimeMultiplier = Mathf.Lerp(5f, 50f, intensity);
            }

            // Scale light intensity
            if (fireLight != null)
                fireLight.intensity = Mathf.Lerp(0.2f, 3f, intensity);
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            if (flameParticles != null) flameParticles.Stop();
            if (fireLight != null) fireLight.intensity = 0f;

            EventBus.Emit("OnAgniKundExtinguished");
            GameManager.Instance?.TriggerGameOver();
        }

        private void OnGameStart()
        {
            _currentHealth = maxHealth;
            _isDead = false;
            _currentTier = 5;
            if (flameParticles != null) flameParticles.Play();
        }

        private void OnGameStop() { }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Enemy Interaction

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Enemy"))
                TakeDamage(enemyDrainPerHit);
        }

        #endregion
    }
}
