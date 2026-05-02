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
    /// Linear: FAI-8
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(HealthSystem))]
    public class BaseEnemy : MonoBehaviour
    {
        // ── Data ───────────────────────────────────────────────────────────
        [SerializeField] protected EnemyData data;
        [SerializeField] protected bool      isElite = false;

        // ── Components ────────────────────────────────────────────────────
        protected Rigidbody2D  _rb;
        protected HealthSystem _health;
        protected Animator     _anim;
        protected SpriteRenderer _sprite;

        // ── State ──────────────────────────────────────────────────────────
        protected enum AIState { Idle, Chase, Attack, Special, Dead }
        protected AIState _state = AIState.Idle;

        protected Transform _player;
        protected float     _attackTimer;
        protected float     _specialTimer;
        protected bool      _isDead;

        // ── Applied stats (elite-adjusted) ────────────────────────────────
        protected float MaxHealth;
        protected float MoveSpeed;
        protected float Damage;
        protected float AttackRange;
        protected float AttackCooldown;
        protected float XPOnDeath;

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

            _rb.gravityScale  = 0f;
            _rb.freezeRotation = true;

            ApplyStats();
        }

        protected virtual void OnEnable()
        {
            _state     = AIState.Idle;
            _isDead    = false;
            _attackTimer  = 0f;
            _specialTimer = 0f;

            EventBus.On<GameObject>("OnPlayerDied", OnPlayerDied);
            EventBus.On<GameObject>("OnEnemyDied",  CheckSelfDeath);
        }

        protected virtual void OnDisable()
        {
            EventBus.Off<GameObject>("OnPlayerDied", OnPlayerDied);
            EventBus.Off<GameObject>("OnEnemyDied",  CheckSelfDeath);
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
                    if (dist > data.deAggroRadius)          { _state = AIState.Idle; break; }
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
            _rb.linearVelocity = dir * MoveSpeed;

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
        }

        private void ApplyStats()
        {
            if (data == null) return;
            MaxHealth     = isElite ? data.maxHealth * data.eliteHealthMult : data.maxHealth;
            MoveSpeed     = isElite ? data.moveSpeed * data.eliteSpeedMult  : data.moveSpeed;
            Damage        = isElite ? data.damage    * data.eliteDamageMult : data.damage;
            AttackRange   = data.attackRange;
            AttackCooldown = data.attackCooldown;
            XPOnDeath     = isElite ? data.xpOnDeath * 3f : data.xpOnDeath;

            if (isElite && _sprite != null) _sprite.color = data.eliteTintColor;
        }

        private void OnPlayerDied(GameObject _) => _state = AIState.Idle;

        #endregion
    }
}
