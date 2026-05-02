using System.Collections;
using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.Bosses
{
    /// <summary>
    /// Vritra — Storm dragon. Final boss. Spawns at minute 20.
    ///
    /// Lore: Vritra blocks the sunrise itself, controlling all water and storms.
    ///   Defeating him allows Agni (the fire god) to rise at dawn — ending the run in victory.
    ///
    /// Core Mechanic — Weather Control:
    ///   Vritra cycles through 3 weather states that change fight dynamics:
    ///     - Storm: Lightning strikes player area randomly; Agni Kund drains faster.
    ///     - Rain:  Agni Kund drains at 3x rate; Vritra becomes faster (water-powered).
    ///     - Darkness: Screen blacks out at edges; Vritra becomes semi-invisible.
    ///
    /// Phase Breakdown:
    ///   Phase 0 (100–60% HP): 1 weather type active. Standard attacks.
    ///   Phase 1 (60–30% HP):  2 weather types can overlap. More lightning.
    ///   Phase 2 (30–0% HP):   All 3 weather types. Blocks the sunrise (timer accelerates).
    ///
    /// Attack Roster:
    ///   - Lightning bolt: Instant strike at player position (3-second warning circle).
    ///   - Flood wave: Horizontal water wave pushing player off optimal position.
    ///   - Storm cloud: Persistent AoE that follows player slowly.
    ///   - Dragon breath: Wide cone of lightning — only in phase 2.
    ///   - Sunrise block: Drains Agni Kund directly (ultimate ability).
    ///
    /// Linear: FAI-10
    /// </summary>
    public class VritraBoss : BaseBoss
    {
        // ── Weather System ─────────────────────────────────────────────────
        public enum WeatherState { Clear, Storm, Rain, Darkness }

        [Header("Weather Control")]
        [SerializeField] private float _weatherCycleDuration  = 12f;
        [SerializeField] private float _rainAgniDrainMult     = 3f;
        [SerializeField] private float _stormLightningInterval = 3f;
        [SerializeField] private float _darknessAlpha         = 0.85f; // 0=transparent,1=black
        private WeatherState           _activeWeather         = WeatherState.Clear;
        private float                  _weatherTimer;
        private float                  _lightningTimer;
        private bool                   _weatherOverlapPhase   = false;
        private WeatherState           _secondaryWeather      = WeatherState.Clear;

        [Header("Lightning Bolt")]
        [SerializeField] private float _lightningWarningDuration = 2.5f;
        [SerializeField] private float _lightningDamage         = 40f;
        [SerializeField] private float _lightningRadius         = 1.5f;

        [Header("Flood Wave")]
        [SerializeField] private float _floodWaveDamage        = 25f;
        [SerializeField] private float _floodWaveKnockback     = 10f;
        [SerializeField] private float _floodWaveInterval      = 7f;
        private float                  _floodTimer;

        [Header("Dragon Breath (Phase 2)")]
        [SerializeField] private float _breathConeAngle        = 70f;
        [SerializeField] private float _breathRange            = 8f;
        [SerializeField] private float _breathDamage           = 35f;
        [SerializeField] private float _breathDuration         = 2f;
        [SerializeField] private float _breathCooldown         = 8f;
        private float                  _breathTimer;

        [Header("Sunrise Block (Ultimate)")]
        [SerializeField] private float _sunriseBlockDrain      = 30f;  // direct Agni drain
        [SerializeField] private float _sunriseBlockCooldown   = 20f;
        private float                  _sunriseBlockTimer;

        [Header("Movement")]
        [SerializeField] private float _baseSpeed              = 2f;
        [SerializeField] private float _attackInterval         = 1.6f;

        private float CurrentSpeed => _baseSpeed
            * (1f + (_activeWeather == WeatherState.Rain ? 0.4f : 0f));

        // ──────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        protected override void OnEnable()
        {
            base.OnEnable();
            _activeWeather      = WeatherState.Clear;
            _weatherTimer       = _weatherCycleDuration;
            _lightningTimer     = _stormLightningInterval;
            _floodTimer         = _floodWaveInterval;
            _breathTimer        = 0f;
            _sunriseBlockTimer  = 0f;
        }

        protected override void Update()
        {
            base.Update();

            if (_isDead || State == BaseBoss.BossState.Inactive) return;

            TickWeather();
            TickStormLightning();
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Intro

        protected override IEnumerator DoIntroSequence()
        {
            EventBus.Emit<string>("OnBossIntroTitle", "वृत्र\nVritra — The Blocker of Sunrise!");
            EventBus.Emit<float>("OnScreenShake", 1.5f);

            // Block out the sky — heavy rain begins
            EventBus.Emit("OnVritraRainBegin");
            _activeWeather = WeatherState.Rain;

            yield return new WaitForSeconds(1.5f);
            EventBus.Emit<string>("OnBossDialogue",
                "The sun will NEVER rise again. Agni will die in the cold!");
            yield return new WaitForSeconds(2f);

            // Clear for fight start — Vritra pulls weather back as a taunt
            EventBus.Emit("OnWeatherClear");
            _activeWeather = WeatherState.Storm; // immediately goes to storm
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Combat Phases

        protected override IEnumerator DoPhaseAttack(int phase)
        {
            _floodTimer    -= Time.deltaTime;
            _breathTimer   -= Time.deltaTime;
            _sunriseBlockTimer -= Time.deltaTime;

            _rb.linearVelocity = DirToPlayer() * CurrentSpeed;

            switch (phase)
            {
                case 0: yield return Phase0_Standard();   break;
                case 1: yield return Phase1_Overlapping(); break;
                case 2: yield return Phase2_Apocalypse(); break;
            }
        }

        private IEnumerator Phase0_Standard()
        {
            // Lightning bolt if player is at range
            if (DistToPlayer() > 4f)
                yield return LightningBoltAttack();
            else
                yield return FloodWaveIfReady();

            yield return new WaitForSeconds(_attackInterval);
        }

        private IEnumerator Phase1_Overlapping()
        {
            yield return LightningBoltAttack();

            if (_floodTimer <= 0f)
            {
                yield return FloodWaveAttack();
                _floodTimer = _floodWaveInterval;
            }

            // Sunrise block
            if (_sunriseBlockTimer <= 0f)
            {
                EventBus.Emit<float>("OnAgniKundDrain", _sunriseBlockDrain * 0.5f);
                _sunriseBlockTimer = _sunriseBlockCooldown;
            }

            yield return new WaitForSeconds(_attackInterval * 0.85f);
        }

        private IEnumerator Phase2_Apocalypse()
        {
            // Dragon breath available in phase 2
            if (_breathTimer <= 0f)
            {
                yield return DragonBreathAttack();
                _breathTimer = _breathCooldown;
            }

            yield return LightningBoltAttack();

            if (_floodTimer <= 0f)
            {
                yield return FloodWaveAttack();
                _floodTimer = _floodWaveInterval * 0.7f;
            }

            // Full sunrise block — massive Agni Kund drain
            if (_sunriseBlockTimer <= 0f)
            {
                yield return SunriseBlockUltimate();
                _sunriseBlockTimer = _sunriseBlockCooldown;
            }

            yield return new WaitForSeconds(_attackInterval * 0.7f);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Phase Transitions

        protected override IEnumerator OnPhaseTransition(int newPhase)
        {
            _rb.linearVelocity = Vector2.zero;
            EventBus.Emit<float>("OnScreenShake", 2f);

            if (newPhase == 1)
            {
                _weatherOverlapPhase = true;
                _secondaryWeather    = WeatherState.Darkness;
                EventBus.Emit<string>("OnBossDialogue",
                    "Feel the storm AND the dark! Neither Agni nor Indra can save you!");
                EventBus.Emit("OnVritraRainBegin");
            }
            else if (newPhase == 2)
            {
                // All 3 weathers + timer pressure
                EventBus.Emit<string>("OnBossDialogue",
                    "THIS IS THE END OF DAWN FOREVER! DARKNESS ETERNAL!");
                EventBus.Emit("OnVritraApocalypse");    // trigger all 3 weather VFX simultaneously
                EventBus.Emit("OnTimerAccelerate");     // game timer speeds up briefly (pressure)
            }

            yield return new WaitForSeconds(2f);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Death

        protected override IEnumerator DoDeathSequence()
        {
            // Clear all weather
            EventBus.Emit("OnAllWeatherEnd");
            EventBus.Emit<float>("OnScreenShake", 3f);

            EventBus.Emit<string>("OnBossDialogue",
                "Impossible... The fire... still burns... Agni... you win... this time...");

            yield return new WaitForSeconds(1.5f);

            // Trigger the sunrise sequence — VICTORY
            EventBus.Emit("OnSunriseBegins");
            yield return new WaitForSeconds(3f);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Weather System

        private void TickWeather()
        {
            _weatherTimer -= Time.deltaTime;
            if (_weatherTimer <= 0f)
            {
                CycleWeather();
                _weatherTimer = _weatherCycleDuration;
            }
        }

        private void CycleWeather()
        {
            // Remove current weather effects
            EmitWeatherEnd(_activeWeather);

            // Pick next weather
            WeatherState next = (WeatherState)(((int)_activeWeather % 3) + 1); // cycles Storm→Rain→Darkness→Storm
            _activeWeather = next;
            EmitWeatherBegin(_activeWeather);

            // Apply gameplay effects
            switch (_activeWeather)
            {
                case WeatherState.Rain:
                    EventBus.Emit<float>("OnAgniKundDrainMultiplier", _rainAgniDrainMult);
                    break;
                case WeatherState.Darkness:
                    EventBus.Emit<float>("OnDarknessVignette", _darknessAlpha);
                    break;
            }

            if (_weatherOverlapPhase)
            {
                EmitWeatherBegin(_secondaryWeather);
            }
        }

        private void EmitWeatherBegin(WeatherState w)
        {
            switch (w)
            {
                case WeatherState.Storm:    EventBus.Emit("OnStormBegin");    break;
                case WeatherState.Rain:     EventBus.Emit("OnVritraRainBegin"); break;
                case WeatherState.Darkness: EventBus.Emit("OnDarknessBegin"); break;
            }
        }

        private void EmitWeatherEnd(WeatherState w)
        {
            switch (w)
            {
                case WeatherState.Storm:    EventBus.Emit("OnStormEnd");      break;
                case WeatherState.Rain:     EventBus.Emit("OnVritraRainEnd"); break;
                case WeatherState.Darkness: EventBus.Emit("OnDarknessEnd");   break;
            }
            // Reset Agni drain multiplier on weather change
            EventBus.Emit<float>("OnAgniKundDrainMultiplier", 1f);
        }

        private void TickStormLightning()
        {
            if (_activeWeather != WeatherState.Storm) return;

            _lightningTimer -= Time.deltaTime;
            if (_lightningTimer <= 0f)
            {
                // Ambient lightning near player (separate from boss attack lightning)
                if (_player != null)
                {
                    Vector2 randomOffset = Random.insideUnitCircle * 5f;
                    EventBus.Emit<Vector2>("OnAmbientLightning",
                        (Vector2)_player.position + randomOffset);
                }

                _lightningTimer = _stormLightningInterval;
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Attack Helpers

        private IEnumerator LightningBoltAttack()
        {
            if (_player == null) yield break;

            Vector2 targetPos = (Vector2)_player.position;

            // Warning circle appears at player position
            EventBus.Emit<Vector2, float>("OnLightningWarning",
                targetPos, _lightningWarningDuration);

            yield return new WaitForSeconds(_lightningWarningDuration);

            // Strike — damage if player is still in radius
            EventBus.Emit<Vector2>("OnLightningStrikeVFX", targetPos);

            if (Vector2.Distance(_player.position, targetPos) <= _lightningRadius)
            {
                DamagePlayer(_lightningDamage);
                EventBus.Emit<float>("OnScreenShake", 0.6f);
            }
        }

        private IEnumerator FloodWaveIfReady()
        {
            if (_floodTimer <= 0f)
            {
                yield return FloodWaveAttack();
                _floodTimer = _floodWaveInterval;
            }
            else
            {
                yield return new WaitForSeconds(0.3f);
            }
        }

        private IEnumerator FloodWaveAttack()
        {
            if (_player == null) yield break;

            // Wave travels from Vritra's position toward player
            Vector2 waveDir = DirToPlayer();
            EventBus.Emit<Vector2, Vector2>("OnFloodWave",
                (Vector2)transform.position, waveDir);

            yield return new WaitForSeconds(0.6f); // wave travel time

            // If player is roughly in the wave path, damage + knockback
            if (DistToPlayer() < 10f)
            {
                DamagePlayer(_floodWaveDamage);
                EventBus.Emit<Vector2, float>("OnKnockback",
                    (Vector2)(_player.position + (Vector3)(waveDir * 3f)),
                    _floodWaveKnockback);
            }
        }

        private IEnumerator DragonBreathAttack()
        {
            if (_player == null) yield break;

            EventBus.Emit<string>("OnBossDialogue", "*VRITRA ROARS*");
            EventBus.Emit<float>("OnScreenShake", 0.4f);

            yield return new WaitForSeconds(0.5f); // windup

            // Cone emission — check if player is inside cone
            Vector2 toPlayer = DirToPlayer();
            float angle = Vector2.Angle(toPlayer, DirToPlayer());

            EventBus.Emit<Vector2, float, float>("OnDragonBreathVFX",
                (Vector2)transform.position, _breathConeAngle, _breathRange);

            float elapsed = 0f;
            while (elapsed < _breathDuration)
            {
                elapsed += Time.deltaTime;

                // Re-check angle each frame since Vritra may rotate
                if (DistToPlayer() <= _breathRange)
                {
                    Vector2 currentToPlayer = DirToPlayer();
                    float   currentAngle    = Vector2.Angle(
                        (Vector2)transform.right, currentToPlayer);

                    if (currentAngle <= _breathConeAngle * 0.5f)
                        DamagePlayer(_breathDamage * Time.deltaTime); // DPS tick
                }

                yield return null;
            }
        }

        private IEnumerator SunriseBlockUltimate()
        {
            EventBus.Emit<string>("OnBossDialogue", "THE SUN SHALL NOT RISE!");
            EventBus.Emit<float>("OnScreenShake", 1f);

            // Darkens entire screen briefly, drains Agni Kund
            EventBus.Emit("OnSunriseBlockVFX");
            EventBus.Emit<float>("OnAgniKundDrain", _sunriseBlockDrain);

            yield return new WaitForSeconds(1.5f);
        }

        #endregion
    }
}
