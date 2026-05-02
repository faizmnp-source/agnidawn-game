using System.Collections;
using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.Bosses
{
    /// <summary>
    /// Mahishasura — Demon king of buffaloes. Spawns at minute 10.
    ///
    /// 3 visual/behavioural phases triggered by HP thresholds:
    ///   Phase 0 — Human Form (100–66% HP):
    ///     Sword slashes, regal movement, summons demon guards, throws spears.
    ///   Phase 1 — Half-Buffalo Form (66–33% HP):
    ///     Faster + stronger, stampede charge attack, ground stomp AoE.
    ///   Phase 2 — Full Buffalo Form (33–0% HP):
    ///     Full beast, relentless charges, horn gore, dust-storm arena.
    ///
    /// Linear: FAI-10
    /// </summary>
    public class MahishasuraBoss : BaseBoss
    {
        [Header("Phase 0 — Human Form")]
        [SerializeField] private float _swordDamage        = 22f;
        [SerializeField] private float _spearDamage        = 18f;
        [SerializeField] private float _swordRange         = 1.8f;
        [SerializeField] private int   _minionSpawnCount  = 3;  // demon guards per summon

        [Header("Phase 1 — Half-Buffalo Form")]
        [SerializeField] private float _stompRadius        = 3.5f;
        [SerializeField] private float _stompDamage        = 28f;
        [SerializeField] private float _chargeSpeed1       = 9f;
        [SerializeField] private float _chargeDuration1    = 0.6f;
        [SerializeField] private float _chargeDamage1      = 35f;

        [Header("Phase 2 — Full Buffalo Form")]
        [SerializeField] private float _chargeSpeed2       = 14f;
        [SerializeField] private float _hornGoreDamage     = 45f;
        [SerializeField] private float _hornGoreRange      = 2.2f;
        [SerializeField] private float _dustStormInterval  = 8f;
        private float                  _dustStormTimer;

        [Header("Movement")]
        [SerializeField] private float _baseSpeed          = 2.5f;
        [SerializeField] private float _attackInterval     = 1.8f;
        private float                  _chargeTimer;
        private const float            CHARGE_COOLDOWN     = 4.5f;

        // ──────────────────────────────────────────────────────────────────
        #region Intro

        protected override IEnumerator DoIntroSequence()
        {
            EventBus.Emit<string>("OnBossIntroTitle", "महिषासुर\nMahishasura rises!");
            EventBus.Emit<float>("OnScreenShake", 0.6f);

            // Begin in human form — short roar then bow taunt
            yield return new WaitForSeconds(2f);

            EventBus.Emit<string>("OnBossDialogue",
                "You dare challenge the lord of all demons? Prepare to kneel!");
            yield return new WaitForSeconds(1.5f);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Combat Phases

        protected override IEnumerator DoPhaseAttack(int phase)
        {
            _chargeTimer   -= Time.deltaTime;
            _dustStormTimer -= Time.deltaTime;

            switch (phase)
            {
                case 0: yield return Phase0_HumanForm();    break;
                case 1: yield return Phase1_HalfBuffalo();  break;
                case 2: yield return Phase2_FullBuffalo();  break;
                default: yield return Phase2_FullBuffalo(); break;
            }
        }

        // ── Phase 0: Human Form ──
        private IEnumerator Phase0_HumanForm()
        {
            _rb.linearVelocity = DirToPlayer() * _baseSpeed;

            float dist = DistToPlayer();

            if (dist <= _swordRange)
            {
                // Sword slash combo: 3 rapid hits
                _rb.linearVelocity = Vector2.zero;
                for (int i = 0; i < 3; i++)
                {
                    DamagePlayer(_swordDamage);
                    EventBus.Emit<Vector2>("OnSwordSlash", (Vector2)transform.position);
                    yield return new WaitForSeconds(0.25f);
                }
            }
            else if (dist < 8f && Random.value < 0.4f)
            {
                // Spear throw
                EventBus.Emit<Vector2, Vector2, float>("OnSpawnProjectile",
                    transform.position, DirToPlayer(), _spearDamage);
            }
            else if (Random.value < 0.15f)
            {
                // Summon demon guards
                EventBus.Emit<Vector2, int, string>("OnSummonMinions",
                    (Vector2)transform.position, _minionSpawnCount, "DemonGuard");
                yield return new WaitForSeconds(0.5f);
            }

            yield return new WaitForSeconds(_attackInterval);
        }

        // ── Phase 1: Half-Buffalo ──
        private IEnumerator Phase1_HalfBuffalo()
        {
            _rb.linearVelocity = DirToPlayer() * (_baseSpeed * 1.4f);

            float dist = DistToPlayer();

            // Ground stomp AoE
            if (dist <= _stompRadius && Random.value < 0.45f)
            {
                _rb.linearVelocity = Vector2.zero;
                yield return StompAttack();
            }

            // Charge
            if (_chargeTimer <= 0f && dist < 10f)
            {
                yield return ChargeAttack(_chargeSpeed1, _chargeDuration1, _chargeDamage1);
                _chargeTimer = CHARGE_COOLDOWN;
            }

            yield return new WaitForSeconds(_attackInterval * 0.8f);
        }

        // ── Phase 2: Full Buffalo ──
        private IEnumerator Phase2_FullBuffalo()
        {
            _rb.linearVelocity = DirToPlayer() * (_baseSpeed * 1.8f);

            float dist = DistToPlayer();

            // Horn gore at close range
            if (dist <= _hornGoreRange)
            {
                _rb.linearVelocity = Vector2.zero;
                DamagePlayer(_hornGoreDamage);
                EventBus.Emit<float>("OnScreenShake", 0.5f);
                yield return new WaitForSeconds(0.4f);
            }

            // Relentless charges — shorter cooldown
            if (_chargeTimer <= 0f)
            {
                yield return ChargeAttack(_chargeSpeed2, _chargeDuration1 * 1.2f, _chargeDamage1 * 1.3f);
                _chargeTimer = CHARGE_COOLDOWN * 0.5f;
            }

            // Periodic dust storm — dims screen, applies slow to player
            if (_dustStormTimer <= 0f)
            {
                EventBus.Emit("OnDustStormBegin");
                EventBus.Emit<float>("OnSlowPlayer", 4f);  // slow for 4 seconds
                _dustStormTimer = _dustStormInterval;
            }

            yield return new WaitForSeconds(_attackInterval * 0.6f);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Phase Transitions

        protected override IEnumerator OnPhaseTransition(int newPhase)
        {
            _rb.linearVelocity = Vector2.zero;
            EventBus.Emit<float>("OnScreenShake", 1.5f);

            if (newPhase == 1)
            {
                EventBus.Emit<string>("OnBossDialogue",
                    "Enough of pleasantries — witness my TRUE form!");
                EventBus.Emit("OnMahishasuraTransform_HalfBuffalo");
            }
            else if (newPhase == 2)
            {
                EventBus.Emit<string>("OnBossDialogue",
                    "RAAAH! NO MORE MERCY!");
                EventBus.Emit("OnMahishasuraTransform_FullBuffalo");
                _dustStormTimer = _dustStormInterval; // start dust storm cycle
            }

            yield return new WaitForSeconds(1.5f);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Death

        protected override IEnumerator DoDeathSequence()
        {
            EventBus.Emit<float>("OnScreenShake", 2.5f);
            EventBus.Emit<string>("OnBossDialogue",
                "A woman... will avenge me... Durga... this is not over...");

            EventBus.Emit("OnDustStormEnd");
            yield return new WaitForSeconds(2.5f);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Attack Helpers

        private IEnumerator StompAttack()
        {
            EventBus.Emit<float>("OnScreenShake", 0.7f);
            EventBus.Emit<Vector2, float>("OnGroundStompVFX",
                (Vector2)transform.position, _stompRadius);

            if (DistToPlayer() <= _stompRadius)
                DamagePlayer(_stompDamage);

            yield return new WaitForSeconds(0.6f);
        }

        private IEnumerator ChargeAttack(float speed, float duration, float damage)
        {
            if (_player == null) yield break;

            // Windup snort
            _rb.linearVelocity = Vector2.zero;
            EventBus.Emit<string>("OnBossDialogue", "CHARGE!");
            yield return new WaitForSeconds(0.5f);

            // Charge
            Vector2 dir = DirToPlayer();
            _rb.linearVelocity = dir * speed;
            yield return new WaitForSeconds(duration);
            _rb.linearVelocity = Vector2.zero;

            // Damage on contact
            if (DistToPlayer() < 2f)
            {
                DamagePlayer(damage);
                EventBus.Emit<float>("OnScreenShake", 0.8f);
            }
        }

        #endregion
    }
}
