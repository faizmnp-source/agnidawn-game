using System.Collections;
using UnityEngine;
using AGNIDAWN.Core;
using AGNIDAWN.Player;

namespace AGNIDAWN.Bosses
{
    /// <summary>
    /// Abstract base class for all AGNIDAWN bosses.
    ///
    /// Handles:
    ///   - Multi-phase FSM (Intro → Phase1..N → Death)
    ///   - Health tracking with phase-break detection
    ///   - Arena modification trigger per phase
    ///   - Intro and death sequences (coroutine-based)
    ///   - EventBus integration (OnBossSpawned, OnBossPhaseChanged, OnBossDied)
    ///   - XP / shard rewards on death
    ///
    /// Each concrete boss overrides:
    ///   - DoIntroSequence()        — cinematic entrance
    ///   - DoPhaseAttack(int phase) — attack logic per phase
    ///   - OnPhaseTransition(int newPhase) — arena/visual changes
    ///   - DoDeathSequence()        — death animation + lore drop
    ///
    /// Linear: FAI-10
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(HealthSystem))]
    public abstract class BaseBoss : MonoBehaviour
    {
        // ── Config ─────────────────────────────────────────────────────────
        [SerializeField] protected BossData data;

        // ── Components ─────────────────────────────────────────────────────
        protected Rigidbody2D    _rb;
        protected HealthSystem   _health;
        protected Animator       _anim;
        protected SpriteRenderer _sprite;

        // ── State ──────────────────────────────────────────────────────────
        public enum BossState { Inactive, Intro, Fighting, PhaseTransition, Dead }
        protected BossState _state = BossState.Inactive;

        protected int     _currentPhase   = 0;   // 0-based; phase 0 = first fight phase
        protected bool    _isDead         = false;
        protected Transform _player;

        // ── Phase tracking ─────────────────────────────────────────────────
        // Derived from data.phaseThresholds. Phase count = thresholds.Length + 1
        private   float[] _phaseHPBreaks;         // absolute HP values at phase breaks
        private   int     _nextBreakIndex = 0;

        // ── Anim hashes ────────────────────────────────────────────────────
        protected static readonly int ANIM_INTRO   = Animator.StringToHash("Intro");
        protected static readonly int ANIM_FIGHT   = Animator.StringToHash("Fight");
        protected static readonly int ANIM_PHASE   = Animator.StringToHash("PhaseChange");
        protected static readonly int ANIM_DEAD    = Animator.StringToHash("Dead");

        // ── Public accessors ───────────────────────────────────────────────
        public BossData Data          => data;
        public BossState State        => _state;
        public int  CurrentPhase      => _currentPhase;
        public bool IsDead            => _isDead;
        public float HealthPercent    => _health != null ? _health.HealthPercent : 0f;

        // ──────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        protected virtual void Awake()
        {
            _rb     = GetComponent<Rigidbody2D>();
            _health = GetComponent<HealthSystem>();
            _anim   = GetComponentInChildren<Animator>();
            _sprite = GetComponentInChildren<SpriteRenderer>();

            _rb.gravityScale   = 0f;
            _rb.freezeRotation = true;
        }

        protected virtual void OnEnable()
        {
            _state          = BossState.Inactive;
            _isDead         = false;
            _currentPhase   = 0;
            _nextBreakIndex = 0;

            BuildPhaseBreaks();
            // Note: health monitoring is done via polling in Update() rather than
            // EventBus subscription, because HealthSystem emits owner-tagged events
            // (e.g. "OnEnemyDamagedWithSource") and BaseBoss does not set ownerTag.
            // Polling guarantees we never miss a death tick regardless of ownerTag.
        }

        protected virtual void OnDisable()
        {
            // No EventBus subscriptions to clean up — health is polled in Update().
        }

        protected virtual void Update()
        {
            if (_isDead || !GameManager.Instance.IsRunning) return;
            if (_state == BossState.Inactive) return;

            FindPlayer();

            // Poll HP for phase transitions (separate from events so we never miss a tick)
            CheckPhaseTransition();

            // Death detection: trigger DeathSequence when HP reaches zero
            if (!_isDead && _health != null && _health.CurrentHealth <= 0f)
                StartCoroutine(DeathSequence());
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Public API

        /// Called by BossManager when this boss's spawn minute is reached.
        public void Activate()
        {
            if (_state != BossState.Inactive) return;
            _state = BossState.Intro;
            StartCoroutine(IntroThenFight());
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Sequences

        private IEnumerator IntroThenFight()
        {
            // Pause normal spawning while boss enters
            EventBus.Emit("OnBossSpawned", data.bossId);

            // Spawn intro VFX
            if (data.introVFXPrefab != null)
                ObjectPool.Instance?.Get("VFX_BossIntro", data.introVFXPrefab,
                    transform.position, Quaternion.identity);

            _anim?.SetTrigger(ANIM_INTRO);
            yield return DoIntroSequence();

            _state = BossState.Fighting;
            _anim?.SetTrigger(ANIM_FIGHT);

            StartCoroutine(FightLoop());
        }

        private IEnumerator FightLoop()
        {
            while (!_isDead && _state == BossState.Fighting)
            {
                yield return DoPhaseAttack(_currentPhase);
            }
        }

        private IEnumerator RunPhaseTransition(int newPhase)
        {
            _state = BossState.PhaseTransition;
            _rb.linearVelocity = Vector2.zero;
            _anim?.SetTrigger(ANIM_PHASE);

            // VFX
            if (data.phaseTransitionVFXPrefab != null)
                ObjectPool.Instance?.Get("VFX_BossPhase", data.phaseTransitionVFXPrefab,
                    transform.position, Quaternion.identity);

            EventBus.Emit<string, int>("OnBossPhaseChanged", data.bossId, newPhase);

            yield return OnPhaseTransition(newPhase);

            _currentPhase = newPhase;
            _state        = BossState.Fighting;
        }

        private IEnumerator DeathSequence()
        {
            _isDead = true;
            _state  = BossState.Dead;
            _rb.linearVelocity = Vector2.zero;

            _anim?.SetTrigger(ANIM_DEAD);

            if (data.deathVFXPrefab != null)
                ObjectPool.Instance?.Get("VFX_BossDead", data.deathVFXPrefab,
                    transform.position, Quaternion.identity);

            yield return DoDeathSequence();

            // Emit rewards
            EventBus.Emit<float, Vector2>("OnXPDropped", data.xpReward, transform.position);
            EventBus.Emit<int>("OnDivineShardsDrop", data.divineShardsDrop);
            EventBus.Emit<string>("OnBossDied", data.bossId);
            EventBus.Emit<string>("OnLoreDrop", data.loreText);

            // Return to pool
            yield return new WaitForSeconds(0.5f);
            ObjectPool.Instance?.Return($"Boss_{data.bossId}", gameObject);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Phase Logic

        private void BuildPhaseBreaks()
        {
            if (data == null || data.phaseThresholds == null) return;

            float totalHP = data.totalHealth;
            _phaseHPBreaks = new float[data.phaseThresholds.Length];
            for (int i = 0; i < data.phaseThresholds.Length; i++)
                _phaseHPBreaks[i] = totalHP * data.phaseThresholds[i];
        }

        private void CheckPhaseTransition()
        {
            if (_state != BossState.Fighting) return;
            if (_health == null) return;
            if (_phaseHPBreaks == null || _nextBreakIndex >= _phaseHPBreaks.Length) return;

            if (_health.CurrentHealth <= _phaseHPBreaks[_nextBreakIndex])
            {
                int nextPhase = _currentPhase + 1;
                _nextBreakIndex++;
                StartCoroutine(RunPhaseTransition(nextPhase));
            }
        }

        private void OnHealthChanged(float dmg, GameObject src)
        {
            if (_health == null || _isDead) return;
            if (_health.CurrentHealth <= 0f)
                StartCoroutine(DeathSequence());
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Abstract / Virtual Overrides

        /// Boss-specific dramatic entrance (camera pan, Sanskrit text, etc.)
        protected abstract IEnumerator DoIntroSequence();

        /// Core attack loop for the given phase index (0, 1, 2…)
        protected abstract IEnumerator DoPhaseAttack(int phase);

        /// Called between phases — change arena, visuals, behaviour pattern
        protected virtual IEnumerator OnPhaseTransition(int newPhase) { yield return null; }

        /// Death animation, lore text reveal, etc.
        protected virtual IEnumerator DoDeathSequence() { yield return new WaitForSeconds(2f); }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Helpers

        protected void FindPlayer()
        {
            if (_player != null) return;
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) _player = go.transform;
        }

        protected void DamagePlayer(float amount)
        {
            if (_player == null) return;
            if (_player.TryGetComponent<HealthSystem>(out var hp))
                hp.TakeDamage(amount, gameObject);
        }

        protected Vector2 DirToPlayer()
        {
            if (_player == null) return Vector2.zero;
            return ((Vector2)(_player.position - transform.position)).normalized;
        }

        protected float DistToPlayer()
        {
            if (_player == null) return float.MaxValue;
            return Vector2.Distance(transform.position, _player.position);
        }

        #endregion
    }
}
