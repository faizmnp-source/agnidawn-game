using System.Collections;
using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.Bosses
{
    /// <summary>
    /// Ravana — 10-headed demon king. Spawns at minute 5.
    ///
    /// Mechanic: Each of Ravana's 10 heads must be destroyed individually.
    ///   - Heads are separate child GameObjects with their own small HP pools.
    ///   - Each head has a unique attack type (see HeadType enum).
    ///   - Ravana grows faster and hits harder as heads are destroyed (rage scaling).
    ///   - When all 10 heads die, Ravana's body collapses.
    ///
    /// Phase transitions (from BossData.phaseThresholds):
    ///   - Phase 0 (10-7 heads alive): Standard multi-head alternating attacks
    ///   - Phase 1 (6-4 heads alive): Rage begins — speed/damage buff, adds charge attack
    ///   - Phase 2 (3-0 heads alive): Full rage — all surviving heads fire simultaneously
    ///
    /// Linear: FAI-10
    /// </summary>
    public class RavanaBoss : BaseBoss
    {
        // ── Head System ────────────────────────────────────────────────────
        public enum HeadType
        {
            Fire, Lightning, Poison, Shadow, Wind,
            Ice, Stone, Illusion, Sound, Death
        }

        [System.Serializable]
        public class RavanaHead
        {
            public HeadType  type;
            public Transform headTransform;
            public float     hp        = 80f;
            public bool      isAlive   = true;
        }

        [Header("Ravana — Heads")]
        [SerializeField] private RavanaHead[] _heads = new RavanaHead[10];
        [SerializeField] private float        _headAttackInterval = 1.2f;

        [Header("Ravana — Rage Scaling")]
        [Tooltip("Speed multiplier added per dead head")]
        [SerializeField] private float _rageSpeedPerHead   = 0.08f;
        [Tooltip("Damage multiplier added per dead head")]
        [SerializeField] private float _rageDamagePerHead  = 0.05f;
        [SerializeField] private float _baseSpeed          = 2f;
        [SerializeField] private float _baseDamage         = 15f;

        [Header("Charge Attack")]
        [SerializeField] private float _chargeSpeed        = 12f;
        [SerializeField] private float _chargeWindup       = 0.6f;
        [SerializeField] private float _chargeDuration     = 0.4f;
        [SerializeField] private float _chargeCooldown     = 5f;
        private float                  _chargeTimer;

        // ── Derived ────────────────────────────────────────────────────────
        private int   AliveHeadCount  => System.Array.FindAll(_heads, h => h.isAlive).Length;
        private int   DeadHeadCount   => 10 - AliveHeadCount;
        private float CurrentSpeed    => _baseSpeed  + _rageSpeedPerHead  * DeadHeadCount;
        private float CurrentDamage   => _baseDamage + _rageDamagePerHead * DeadHeadCount;

        // ──────────────────────────────────────────────────────────────────
        #region Intro

        protected override IEnumerator DoIntroSequence()
        {
            // Ground shake + Sanskrit title card via EventBus
            EventBus.Emit<string>("OnBossIntroTitle", "रावण\nRavana approaches!");
            EventBus.Emit<float>("OnScreenShake", 0.8f);

            // Materialise — fade in over 2 seconds
            yield return new WaitForSeconds(2.5f);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Combat Phases

        protected override IEnumerator DoPhaseAttack(int phase)
        {
            _chargeTimer -= Time.deltaTime;

            switch (phase)
            {
                case 0: yield return Phase0_MultiHead();    break;
                case 1: yield return Phase1_Rage();         break;
                case 2: yield return Phase2_FullRage();     break;
                default: yield return Phase2_FullRage();   break;
            }
        }

        private IEnumerator Phase0_MultiHead()
        {
            // Pick a random alive head and fire its attack
            var head = PickRandomAliveHead();
            if (head != null)
            {
                yield return FireHeadAttack(head);
            }

            // Move toward player slowly
            MoveBody(CurrentSpeed * 0.6f);
            yield return new WaitForSeconds(_headAttackInterval);
        }

        private IEnumerator Phase1_Rage()
        {
            // Same as phase 0 but faster + occasional charge
            var head = PickRandomAliveHead();
            if (head != null)
                yield return FireHeadAttack(head);

            MoveBody(CurrentSpeed);

            if (_chargeTimer <= 0f && DistToPlayer() < 8f)
            {
                yield return ChargeAttack();
                _chargeTimer = _chargeCooldown;
            }

            yield return new WaitForSeconds(_headAttackInterval * 0.8f);
        }

        private IEnumerator Phase2_FullRage()
        {
            // All remaining heads fire simultaneously
            foreach (var head in _heads)
            {
                if (head.isAlive)
                    StartCoroutine(FireHeadAttack(head));
            }

            MoveBody(CurrentSpeed * 1.3f);

            if (_chargeTimer <= 0f)
            {
                yield return ChargeAttack();
                _chargeTimer = _chargeCooldown * 0.7f;
            }

            yield return new WaitForSeconds(_headAttackInterval * 0.6f);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Phase Transitions

        protected override IEnumerator OnPhaseTransition(int newPhase)
        {
            EventBus.Emit<float>("OnScreenShake", 1.2f);
            EventBus.Emit<string>("OnBossDialogue",
                newPhase == 1 ? "You dare wound me?!" : "FEEL THE WRATH OF ALL LANKA!");

            // Flash all alive heads red briefly
            foreach (var head in _heads)
            {
                if (head.isAlive && head.headTransform != null)
                {
                    var sr = head.headTransform.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.color = Color.red;
                }
            }

            yield return new WaitForSeconds(0.3f);

            foreach (var head in _heads)
            {
                if (head.isAlive && head.headTransform != null)
                {
                    var sr = head.headTransform.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.color = Color.white;
                }
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Death

        protected override IEnumerator DoDeathSequence()
        {
            EventBus.Emit<float>("OnScreenShake", 2f);
            EventBus.Emit<string>("OnBossDialogue", "Impossible... Lanka shall avenge me...");

            // Kill remaining heads with staggered pops
            foreach (var head in _heads)
            {
                if (head.isAlive)
                {
                    head.isAlive = false;
                    if (head.headTransform != null)
                        head.headTransform.gameObject.SetActive(false);

                    EventBus.Emit<Vector2>("OnMiniExplosion", head.headTransform != null
                        ? (Vector2)head.headTransform.position : (Vector2)transform.position);

                    yield return new WaitForSeconds(0.15f);
                }
            }

            yield return new WaitForSeconds(1.5f);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Head Damage API (called by weapon hit detection on child heads)

        /// Called when a player projectile hits a specific head.
        public void DamageHead(int headIndex, float damage)
        {
            if (headIndex < 0 || headIndex >= _heads.Length) return;
            var head = _heads[headIndex];
            if (!head.isAlive) return;

            head.hp -= damage;

            if (head.hp <= 0f)
            {
                head.isAlive = false;
                if (head.headTransform != null)
                    head.headTransform.gameObject.SetActive(false);

                EventBus.Emit<string, int>("OnRavanaHeadDestroyed", head.type.ToString(), headIndex);
                EventBus.Emit<Vector2>("OnMiniExplosion",
                    head.headTransform != null
                        ? (Vector2)head.headTransform.position
                        : (Vector2)transform.position);

                // If all heads gone, force kill body
                if (AliveHeadCount == 0)
                    _health?.InstantKill(gameObject);
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Private Helpers

        private RavanaHead PickRandomAliveHead()
        {
            var alive = System.Array.FindAll(_heads, h => h.isAlive);
            if (alive.Length == 0) return null;
            return alive[Random.Range(0, alive.Length)];
        }

        private IEnumerator FireHeadAttack(RavanaHead head)
        {
            if (_player == null) yield break;

            switch (head.type)
            {
                case HeadType.Fire:
                    // Cone of fire projectiles
                    FireProjectileFan(head.headTransform?.position ?? transform.position, 3, 20f, CurrentDamage);
                    break;

                case HeadType.Lightning:
                    // Instant lightning strike at player position
                    EventBus.Emit<Vector2, float>("OnLightningStrike",
                        (Vector2)_player.position, CurrentDamage * 1.5f);
                    DamagePlayer(CurrentDamage * 0.8f);
                    break;

                case HeadType.Poison:
                    // Leave a poison cloud at head position
                    EventBus.Emit<Vector2, float, float>("OnPoisonCloud",
                        head.headTransform != null
                            ? (Vector2)head.headTransform.position
                            : (Vector2)transform.position,
                        CurrentDamage * 0.3f, 3f);
                    break;

                case HeadType.Shadow:
                    // Teleport clone decoys (3 fake Ravanas for 2s)
                    EventBus.Emit<Vector2, int>("OnSpawnDecoys", (Vector2)transform.position, 3);
                    break;

                case HeadType.Wind:
                    // Knockback blast pushing player away
                    EventBus.Emit<Vector2, float>("OnKnockback",
                        (Vector2)_player.position, 8f);
                    break;

                case HeadType.Ice:
                    // Slow field — reduces player speed for 2s
                    EventBus.Emit<Vector2, float, float>("OnSlowField",
                        (Vector2)_player.position, 3f, 2f);
                    DamagePlayer(CurrentDamage * 0.6f);
                    break;

                case HeadType.Stone:
                    // Hurl boulders — single heavy hit
                    DamagePlayer(CurrentDamage * 2f);
                    EventBus.Emit<float>("OnScreenShake", 0.4f);
                    break;

                case HeadType.Illusion:
                    // Inverts player controls for 1.5s
                    EventBus.Emit<float>("OnInvertControls", 1.5f);
                    break;

                case HeadType.Sound:
                    // Stun (stop player movement) for 0.8s
                    EventBus.Emit<float>("OnPlayerStunned", 0.8f);
                    DamagePlayer(CurrentDamage * 0.5f);
                    break;

                case HeadType.Death:
                    // Drain Agni Kund fire directly
                    EventBus.Emit<float>("OnAgniKundDrain", 15f);
                    DamagePlayer(CurrentDamage);
                    break;
            }

            yield return null;
        }

        private void FireProjectileFan(Vector3 origin, int count, float spreadDeg, float damage)
        {
            if (_player == null) return;
            Vector2 baseDir = DirToPlayer();
            float stepAngle = spreadDeg / Mathf.Max(1, count - 1);
            float startAngle = -spreadDeg * 0.5f;

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + stepAngle * i;
                Vector2 dir = RotateVector(baseDir, angle);
                EventBus.Emit<Vector2, Vector2, float>("OnSpawnProjectile",
                    origin, dir, damage);
            }
        }

        private IEnumerator ChargeAttack()
        {
            if (_player == null) yield break;

            // Windup
            _rb.linearVelocity = Vector2.zero;
            EventBus.Emit<float>("OnScreenShake", 0.3f);
            yield return new WaitForSeconds(_chargeWindup);

            // Charge
            Vector2 chargeDir = DirToPlayer();
            _rb.linearVelocity = chargeDir * _chargeSpeed;
            yield return new WaitForSeconds(_chargeDuration);

            // Stop
            _rb.linearVelocity = Vector2.zero;

            // Deal damage if player was in the path (proximity check)
            if (DistToPlayer() < 1.5f)
                DamagePlayer(CurrentDamage * 2.5f);
        }

        private void MoveBody(float speed)
        {
            if (_player == null) return;
            _rb.linearVelocity = DirToPlayer() * speed;
        }

        private static Vector2 RotateVector(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        #endregion
    }
}
