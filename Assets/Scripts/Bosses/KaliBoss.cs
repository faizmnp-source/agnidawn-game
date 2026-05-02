using System.Collections;
using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.Bosses
{
    /// <summary>
    /// Kali — Goddess of time, death and destruction. Spawns at minute 15.
    ///
    /// Core Mechanic: Kali grows stronger for EVERY enemy death nearby.
    ///   - Tracks a "Bloodlust" stack (capped at 50).
    ///   - Each stack adds +2% damage and +1% move speed.
    ///   - At 25 stacks: arena fills with dark energy (reduces player visibility).
    ///   - At 50 stacks: Kali enters Mahakali form — double damage, AOE chain-sickle.
    ///
    /// Primary weapon: Chain-sickle (Khadga).
    ///   - Phase 0: Single-target sickle slash at close range.
    ///   - Phase 1: Chain throw — sickle extends outward then pulls player in.
    ///   - Phase 2: Spinning sickle AoE — rotates around Kali, hits all nearby.
    ///
    /// Linear: FAI-10
    /// </summary>
    public class KaliBoss : BaseBoss
    {
        [Header("Bloodlust System")]
        [SerializeField] private int   _bloodlustPerKill     = 1;
        [SerializeField] private int   _bloodlustCap         = 50;
        [SerializeField] private int   _darkEnergyThreshold  = 25;   // stacks to trigger dark arena
        [SerializeField] private int   _mahakaliThreshold    = 50;   // stacks to trigger Mahakali
        private int                    _bloodlustStacks      = 0;
        private bool                   _isDarkEnergyActive   = false;
        private bool                   _isMahakali           = false;

        [Header("Chain-Sickle")]
        [SerializeField] private float _slashDamage          = 25f;
        [SerializeField] private float _slashRange           = 2.2f;
        [SerializeField] private float _chainThrowDamage     = 30f;
        [SerializeField] private float _chainThrowRange      = 7f;
        [SerializeField] private float _chainPullForce       = 8f;
        [SerializeField] private float _spinningAoERadius    = 4f;
        [SerializeField] private float _spinningAoEDamage    = 20f;
        [SerializeField] private float _spinningAoEDuration  = 3f;

        [Header("Movement")]
        [SerializeField] private float _baseSpeed            = 3.5f;
        [SerializeField] private float _attackInterval       = 1.4f;

        // ── Derived stats with bloodlust scaling ──────────────────────────
        private float BloodlustSpeedMult   => 1f + _bloodlustStacks * 0.01f;
        private float BloodlustDamageMult  => 1f + _bloodlustStacks * 0.02f;
        private float CurrentSpeed         => _baseSpeed  * BloodlustSpeedMult;
        private float CurrentSlashDamage   => _slashDamage * BloodlustDamageMult;
        private float CurrentChainDamage   => _chainThrowDamage * BloodlustDamageMult;
        private float CurrentSpinDamage    => _spinningAoEDamage * BloodlustDamageMult;

        // ──────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        protected override void OnEnable()
        {
            base.OnEnable();
            _bloodlustStacks    = 0;
            _isDarkEnergyActive = false;
            _isMahakali         = false;
            EventBus.On<GameObject>("OnEnemyDied", OnAnyEnemyDied);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EventBus.Off<GameObject>("OnEnemyDied", OnAnyEnemyDied);

            // Clean up arena effects
            if (_isDarkEnergyActive) EventBus.Emit("OnDarkEnergyEnd");
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Bloodlust Tracking

        private void OnAnyEnemyDied(GameObject enemy)
        {
            if (_isDead) return;

            // Only count enemies that died near Kali (within 12 units)
            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist > 12f) return;

            _bloodlustStacks = Mathf.Min(_bloodlustStacks + _bloodlustPerKill, _bloodlustCap);

            EventBus.Emit<int>("OnKaliBloodlust", _bloodlustStacks);

            // Dark energy threshold
            if (!_isDarkEnergyActive && _bloodlustStacks >= _darkEnergyThreshold)
            {
                _isDarkEnergyActive = true;
                EventBus.Emit("OnDarkEnergyBegin");
            }

            // Mahakali threshold
            if (!_isMahakali && _bloodlustStacks >= _mahakaliThreshold)
            {
                _isMahakali = true;
                StartCoroutine(EnterMahakali());
            }
        }

        private IEnumerator EnterMahakali()
        {
            EventBus.Emit<float>("OnScreenShake", 2f);
            EventBus.Emit<string>("OnBossDialogue", "JAY MAHAKALI! ALL SHALL BE CONSUMED!");
            EventBus.Emit("OnMahakaliTransform");
            yield return new WaitForSeconds(1f);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Intro

        protected override IEnumerator DoIntroSequence()
        {
            EventBus.Emit<string>("OnBossIntroTitle", "काली\nKali emerges from the darkness!");
            EventBus.Emit("OnDarkEnergyBegin");    // immediate arena darkening on intro
            _isDarkEnergyActive = true;

            EventBus.Emit<float>("OnScreenShake", 1f);
            yield return new WaitForSeconds(1f);

            EventBus.Emit<string>("OnBossDialogue",
                "Your blood will feed the eternal darkness...");
            yield return new WaitForSeconds(1.5f);

            EventBus.Emit("OnDarkEnergyEnd");      // clear for fight start
            _isDarkEnergyActive = false;
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Combat Phases

        protected override IEnumerator DoPhaseAttack(int phase)
        {
            _rb.linearVelocity = DirToPlayer() * CurrentSpeed;

            float dist = DistToPlayer();

            switch (phase)
            {
                case 0: yield return Phase0_SickleSweep(dist); break;
                case 1: yield return Phase1_ChainThrow(dist);  break;
                case 2: yield return Phase2_SpinningSickle();   break;
            }
        }

        private IEnumerator Phase0_SickleSweep(float dist)
        {
            if (dist <= _slashRange)
            {
                _rb.linearVelocity = Vector2.zero;
                // Quick 2-hit combo
                DamagePlayer(CurrentSlashDamage);
                yield return new WaitForSeconds(0.15f);
                DamagePlayer(CurrentSlashDamage * 0.8f);
                EventBus.Emit<Vector2>("OnSickleSweep", (Vector2)transform.position);
            }

            yield return new WaitForSeconds(_attackInterval);
        }

        private IEnumerator Phase1_ChainThrow(float dist)
        {
            if (dist <= _slashRange)
            {
                // Slash first
                _rb.linearVelocity = Vector2.zero;
                DamagePlayer(CurrentSlashDamage * 1.2f);
                EventBus.Emit<Vector2>("OnSickleSweep", (Vector2)transform.position);
            }
            else if (dist <= _chainThrowRange && Random.value < 0.6f)
            {
                // Chain throw — launch sickle toward player, then yank them back
                _rb.linearVelocity = Vector2.zero;
                yield return ChainThrowAttack();
            }

            yield return new WaitForSeconds(_attackInterval * 0.85f);
        }

        private IEnumerator Phase2_SpinningSickle()
        {
            // Occasionally trigger the full spinning sickle AoE
            if (Random.value < 0.35f || _isMahakali)
            {
                _rb.linearVelocity = Vector2.zero;
                yield return SpinningSickleAoE();
            }
            else
            {
                // Otherwise do chain throw + slash combo
                if (DistToPlayer() <= _chainThrowRange)
                    yield return ChainThrowAttack();

                if (DistToPlayer() <= _slashRange)
                    DamagePlayer(CurrentSlashDamage * (_isMahakali ? 2f : 1.4f));
            }

            yield return new WaitForSeconds(_attackInterval * 0.65f);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Phase Transitions

        protected override IEnumerator OnPhaseTransition(int newPhase)
        {
            _rb.linearVelocity = Vector2.zero;
            EventBus.Emit<float>("OnScreenShake", 1.2f);

            if (newPhase == 1)
            {
                EventBus.Emit<string>("OnBossDialogue",
                    "Feed me more death. FEED ME!");
            }
            else if (newPhase == 2)
            {
                EventBus.Emit<string>("OnBossDialogue",
                    "The end of all things begins NOW.");
                // Auto-populate some bloodlust since she's near-death
                _bloodlustStacks = Mathf.Max(_bloodlustStacks, _darkEnergyThreshold);
                if (!_isDarkEnergyActive)
                {
                    _isDarkEnergyActive = true;
                    EventBus.Emit("OnDarkEnergyBegin");
                }
            }

            yield return new WaitForSeconds(1.2f);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Death

        protected override IEnumerator DoDeathSequence()
        {
            EventBus.Emit<float>("OnScreenShake", 2f);
            EventBus.Emit("OnDarkEnergyEnd");
            _isDarkEnergyActive = false;

            EventBus.Emit<string>("OnBossDialogue",
                "I am eternal... I will return... when the next Yuga begins...");

            yield return new WaitForSeconds(2.5f);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Attack Helpers

        private IEnumerator ChainThrowAttack()
        {
            if (_player == null) yield break;

            // Visual: sickle launches
            EventBus.Emit<Vector2, Vector2>("OnChainSickleThrow",
                (Vector2)transform.position, (Vector2)_player.position);

            yield return new WaitForSeconds(0.3f);

            // Hit
            if (DistToPlayer() <= _chainThrowRange)
            {
                DamagePlayer(CurrentChainDamage);

                // Pull player toward Kali
                EventBus.Emit<Vector2, float>("OnKnockback",
                    (Vector2)transform.position, -_chainPullForce); // negative = pull
            }

            yield return new WaitForSeconds(0.4f);
        }

        private IEnumerator SpinningSickleAoE()
        {
            EventBus.Emit<Vector2, float, float>("OnSpinningSickleVFX",
                (Vector2)transform.position, _spinningAoERadius, _spinningAoEDuration);

            float elapsed = 0f;
            float tickInterval = 0.4f;
            float nextTick = tickInterval;

            while (elapsed < _spinningAoEDuration)
            {
                elapsed += Time.deltaTime;
                nextTick -= Time.deltaTime;

                if (nextTick <= 0f)
                {
                    // Damage player if in range
                    if (DistToPlayer() <= _spinningAoERadius)
                        DamagePlayer(CurrentSpinDamage * (_isMahakali ? 2f : 1f));

                    nextTick = tickInterval;
                }

                yield return null;
            }
        }

        #endregion
    }
}
