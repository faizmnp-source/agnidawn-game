using System.Collections;
using UnityEngine;
using AGNIDAWN.Core;
using AGNIDAWN.Player;

namespace AGNIDAWN.Enemies
{
    /// <summary>
    /// Base class for all mythological enemies in AGNIDAWN.
    /// Each EnemyType overrides the special ability via DoSpecialAbility().
    ///
    /// Unlike 20 Minutes Until Dawn's simple "chase player" AI, each type here
    /// has a distinct behaviour profile matching its mythological role:
    ///   - Asura: rage-charges at full speed
    ///   - Rakshasa: flanks the player, circling from the side
    ///   - Naga: maintains range, fires venom projectiles
    ///   - Pisacha: swarms in groups, explodes on death dealing AoE
    ///   - Vetala: teleports behind the player
    ///   - Yaksha: slow tank that shields nearby allies
    ///   - Brahmarakshasa: hangs back, summons minions from a distance
    ///
    /// Phase 7 additions:
    ///   - OnSlowApplied consumer (Varunastra slow effect)
    ///   - OnKnockbackApplied consumer (Vayuastra knockback effect)
    ///   - Biome speed/damage modifiers applied via OnBiomeEntered EventBus event
    ///     (EventBus used to avoid circular dependency with AGNIDAWN.Gameplay)
    ///
    /// Linear: FAI-8 / FAI-12
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(HealthSystem))]
    public class BaseEnemy : MonoBehaviour
    {
        // ── Data ───────────────────────────────────────────────────────────
        [SerializeField] protected EnemyData data;
        [SerializeField] protected bool      isElite = false;

        // ── Components ────────────────────────────────────────────────────
        protected Rigidbody2D    _rb;
        protected HealthSystem   _health;
        protected Animator       _anim;
        protected SpriteRenderer _sprite;

        // ── State ──────────────────────────────────────────────────────────
        protected enum AIState { Idle, Chase, Attack, Special, Dead }
        protected AIState _state = AIState.Idle;

        protected Transform _player;
        protected float     _attackTimer;
        protected float     _specialTimer;
        protected bool      _isDead;

        // ── Applied stats (elite-adjusted + biome-adjusted) ───────────────
        protected float MaxHealth;
        protected float MoveSpeed;
        protected float Damage;
        protected float AttackRange;
        protected float AttackCooldown;
        protected float XPOnDeath;

        // ── Biome modifier cache ───────────────────────────────────────────
        private float _biomeSpdMult = 1f;
        private float _biomeDmgMult = 1f;

        // ── Status effects ────────────────────────────────────────────────
        private float _slowMultiplier  = 1f;   // 1 = no slow
        private float _slowTimer       = 0f;
        private bool  _isKnockedBack   = false;
        private float _knockbackTimer  = 0f;

        // ── Anim hashes ───────────────────────────────────────────────────
        protected static readonly int ANIM_WALK   = Animator.StringToHash("Walk");
        protected static readonly int ANIM_ATTACK = Animator.StringToHash("Attack");
        protected static readonly int ANIM_DEAD   = Animator.StringToHash("Dead");

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

            ApplyStats();
        }

        protected virtual void OnEnable()
        {
            _state          = AIState.Idle;
            _isDead         = false;
            _attackTimer    = 0f;
            _specialTimer   = 0f;
            _slowMultiplier = 1f;
            _slowTimer      = 0f;
            _isKnockedBack  = false;
            _knockbackTimer = 0f;

            // Re-apply stats with current biome modifiers (pool re-use)
            ApplyStats();

            EventBus.On<GameObject>("OnPlayerDied",                       OnPlayerDied);
            EventBus.On<GameObject>("OnEnemyDied",                        CheckSelfDeath);
            EventBus.On<GameObject, float, float>("OnSlowApplied",        OnSlowApplied);
            EventBus.On<GameObject, Vector2, float>("OnKnockbackApplied", OnKnockbackApplied);
            // Phase 7 — listen for biome transitions to refresh stat modifiers
            EventBus.On<BiomeData>("OnBiomeEntered",                      OnBiomeEntered);
        }

        protected virtual void OnDisable()
        {
            EventBus.Off<GameObject>("OnPlayerDied",                       OnPlayerDied);
            EventBus.Off<GameObject>("OnEnemyDied",                        CheckSelfDeath);
            EventBus.Off<GameObject, float, float>("OnSlowApplied",        OnSlowApplied);
            EventBus.Off<GameObject, Vector2, float>("OnKnockbackApplied", OnKnockbackApplied);
            EventBus.Off<BiomeData>("OnBiomeEntered",                      OnBiomeEntered);

            // Unregister from SpawnManager registry when returned to pool
            SpawnManager.Instance?.UnregisterEnemy(this);
        }

        protected virtual void Update()
        {
            if (_isDead || !GameManager.Instance.IsRunning) return;

            FindPlayer();
            TickTimers();
            RunAI();
        }

        protected virtual void FixedUpdate()
        {
            if (_isDead || !GameManager.Instance.IsRunning) return;
            if (_isKnockedBack) return; // knockback overrides movement this frame
            if (_state == AIState.Chase) MoveTowardTarget();
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region AI Core

        protected virtual void RunAI()
        {
            if (_player == null) { _state = AIState.Idle; return; }

            float dist = Vector2.Distance(transform.position, _player.position);

            switch (_state)
            {
                case AIState.Idle:
                    if (dist <= data.aggroRadius)
                    {
                        _state = AIState.Chase;
                        EventBus.Emit("OnEnemyAggro");
                    }
                    break;

                case AIState.Chase:
                    if (dist > data.deAggroRadius)          { _state = AIState.Idle;   break; }
                    if (dist <= data.attackRange)           { _state = AIState.Attack; break; }
                    if (CanUseSpecial(dist))                { StartCoroutine(UseSpecial()); break; }
                    break;

                case AIState.Attack:
                    if (dist > data.attackRange * 1.5f)    { _state = AIState.Chase; break; }
                    if (_attackTimer <= 0f)                 { PerformAttack(); break; }
                    break;

                case AIState.Special:
                    // Handled by coroutine
                    break;
            }

            _anim?.SetBool(ANIM_WALK, _state == AIState.Chase);
        }

        protected virtual void MoveTowardTarget()
        {
            if (_player == null) return;

            Vector2 dir = GetMoveDirection();
            // Apply slow multiplier on top of biome-adjusted speed
            _rb.linearVelocity = dir * (MoveSpeed * _slowMultiplier);

            // Flip sprite
            if (_sprite != null && dir.x != 0)
                _sprite.flipX = dir.x < 0;
        }

        /// Override in subclasses for unique movement (e.g. Rakshasa flanks, Naga backs off)
        protected virtual Vector2 GetMoveDirection()
            => ((Vector2)(_player.position - transform.position)).normalized;

        protected virtual void PerformAttack()
        {
            _attackTimer = AttackCooldown;
            _anim?.SetTrigger(ANIM_ATTACK);

            if (_player != null && _player.TryGetComponent<HealthSystem>(out var hp))
                hp.TakeDamage(Damage, gameObject);

            EventBus.Emit<Vector2>("OnEnemyAttack", transform.position);
        }

        protected virtual bool CanUseSpecial(float dist)
            => _specialTimer <= 0f && dist <= data.specialRange;

        protected virtual IEnumerator UseSpecial()
        {
            _state = AIState.Special;
            _rb.linearVelocity = Vector2.zero;

            yield return DoSpecialAbility();

            _specialTimer = data.specialCooldown;
            _state = AIState.Chase;
        }

        /// Override per enemy type to implement unique mythological ability
        protected virtual IEnumerator DoSpecialAbility() { yield return null; }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Status Effects (Phase 7)

        /// <summary>
        /// Varunastra emits OnSlowApplied (target, speedMultiplier, duration).
        /// Only applies if this GameObject is the target.
        /// </summary>
        private void OnSlowApplied(GameObject target, float speedMult, float duration)
        {
            if (target != gameObject) return;
            _slowMultiplier = Mathf.Clamp01(speedMult);
            _slowTimer      = duration;

            // Blue tint while slowed
            if (_sprite != null)
                _sprite.color = Color.Lerp(_sprite.color, new Color(0.4f, 0.6f, 1f, 1f), 0.6f);
        }

        /// <summary>
        /// Vayuastra emits OnKnockbackApplied (target, force, duration).
        /// Applies an impulse and temporarily disables normal AI movement.
        /// </summary>
        private void OnKnockbackApplied(GameObject target, Vector2 force, float duration)
        {
            if (target != gameObject) return;
            _isKnockedBack  = true;
            _knockbackTimer = duration;
            _rb.linearVelocity = force;
        }

        private void TickStatusEffects()
        {
            // Slow expiry
            if (_slowTimer > 0f)
            {
                _slowTimer -= Time.deltaTime;
                if (_slowTimer <= 0f)
                {
                    _slowMultiplier = 1f;
                    // Restore original tint
                    if (_sprite != null)
                        _sprite.color = (isElite && data != null) ? data.eliteTintColor : Color.white;
                }
            }

            // Knockback expiry
            if (_isKnockedBack)
            {
                _knockbackTimer -= Time.deltaTime;
                if (_knockbackTimer <= 0f)
                {
                    _isKnockedBack = false;
                    _rb.linearVelocity = Vector2.zero;
                }
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Biome Modifiers (Phase 7)

        /// <summary>
        /// Cache biome multipliers and re-apply stats when a biome transition fires.
        /// Uses EventBus to avoid a circular dependency with AGNIDAWN.Gameplay.
        /// </summary>
        private void OnBiomeEntered(BiomeData biome)
        {
            if (biome == null) return;
            _biomeSpdMult = biome.enemySpeedMultiplier;
            _biomeDmgMult = biome.enemyDamageMultiplier;
            ApplyStats();
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Death

        public virtual void Die()
        {
            if (_isDead) return;
            _isDead = true;
            _state  = AIState.Dead;
            _rb.linearVelocity = Vector2.zero;

            _anim?.SetTrigger(ANIM_DEAD);

            // Drop XP
            EventBus.Emit<float, Vector2>("OnXPDropped", XPOnDeath, transform.position);
            EventBus.Emit<GameObject>("OnEnemyDied", gameObject);

            // Spawn death VFX
            if (data?.deathVFXPrefab != null)
                ObjectPool.Instance?.Get("VFX_Death", data.deathVFXPrefab,
                    transform.position, Quaternion.identity);

            StartCoroutine(DieDelay());
        }

        private IEnumerator DieDelay()
        {
            yield return new WaitForSeconds(0.4f);
            ObjectPool.Instance?.Return($"Enemy_{data?.enemyId}", gameObject);
        }

        private void CheckSelfDeath(GameObject who)
        {
            if (who == gameObject) Die();
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Helpers

        private void FindPlayer()
        {
            if (_player != null) return;
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) _player = go.transform;
        }

        private void TickTimers()
        {
            if (_attackTimer  > 0f) _attackTimer  -= Time.deltaTime;
            if (_specialTimer > 0f) _specialTimer -= Time.deltaTime;
            TickStatusEffects();
        }

        private void ApplyStats()
        {
            if (data == null) return;

            float speedMult  = _biomeSpdMult;
            float damageMult = _biomeDmgMult;

            // Elite multipliers stack on top of biome multipliers
            if (isElite)
            {
                MaxHealth = data.maxHealth * data.eliteHealthMult;
                speedMult  *= data.eliteSpeedMult;
                damageMult *= data.eliteDamageMult;
            }
            else
            {
                MaxHealth = data.maxHealth;
            }

            MoveSpeed      = data.moveSpeed      * speedMult;
            Damage         = data.damage         * damageMult;
            AttackRange    = data.attackRange;
            AttackCooldown = data.attackCooldown;
            XPOnDeath      = isElite ? data.xpOnDeath * 3f : data.xpOnDeath;

            if (isElite && _sprite != null) _sprite.color = data.eliteTintColor;
        }

        private void OnPlayerDied(GameObject _) => _state = AIState.Idle;

        #endregion
    }
}
