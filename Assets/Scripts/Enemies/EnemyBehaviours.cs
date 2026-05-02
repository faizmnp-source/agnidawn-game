using System.Collections;
using UnityEngine;
using AGNIDAWN.Core;
using AGNIDAWN.Player;

namespace AGNIDAWN.Enemies
{
    /// ─────────────────────────────────────────────────────────────────────
    /// RAKSHASA — Shape-shifting flanker
    /// Circles the player from the side before lunging. Elite variant
    /// gains a brief invincibility dash.
    /// ─────────────────────────────────────────────────────────────────────
    public class RakshasaEnemy : BaseEnemy
    {
        private float _circleAngle;
        private float _circleRadius = 3.5f;

        protected override Vector2 GetMoveDirection()
        {
            if (_player == null) return Vector2.zero;
            float dist = Vector2.Distance(transform.position, _player.position);

            // When close enough, circle the player instead of charging straight in
            if (dist < _circleRadius * 1.5f)
            {
                _circleAngle += 90f * Time.fixedDeltaTime;
                Vector2 offset = new Vector2(
                    Mathf.Cos(_circleAngle * Mathf.Deg2Rad),
                    Mathf.Sin(_circleAngle * Mathf.Deg2Rad)
                ) * _circleRadius;
                Vector2 target = (Vector2)_player.position + offset;
                return ((Vector2)(target - (Vector2)transform.position)).normalized;
            }
            return ((Vector2)(_player.position - transform.position)).normalized;
        }

        protected override IEnumerator DoSpecialAbility()
        {
            // Shape-shift burst: go invisible briefly, teleport to player's flank
            if (_sprite != null) _sprite.color = new Color(1f, 1f, 1f, 0.2f);
            yield return new WaitForSeconds(0.4f);
            if (_player != null)
            {
                Vector2 flank = (Vector2)_player.position +
                    new Vector2(-_player.right.x, -_player.right.y) * 1.5f;
                transform.position = flank;
            }
            if (_sprite != null) _sprite.color = Color.white;
            PerformAttack();
        }
    }

    /// ─────────────────────────────────────────────────────────────────────
    /// NAGA — Ranged venom spitter
    /// Keeps a preferred distance, retreats if player gets too close,
    /// fires venom projectiles. Elite fires a spread of 3.
    /// ─────────────────────────────────────────────────────────────────────
    public class NagaEnemy : BaseEnemy
    {
        [SerializeField] private GameObject venomPrefab;
        private const float PREFERRED_RANGE = 5f;

        protected override Vector2 GetMoveDirection()
        {
            if (_player == null) return Vector2.zero;
            float dist = Vector2.Distance(transform.position, _player.position);

            // Retreat if too close
            if (dist < PREFERRED_RANGE * 0.7f)
                return ((Vector2)(transform.position - _player.position)).normalized;
            // Advance if too far
            if (dist > PREFERRED_RANGE * 1.3f)
                return ((Vector2)(_player.position - transform.position)).normalized;

            return Vector2.zero; // Maintain distance — strafe slightly
        }

        protected override void PerformAttack()
        {
            _attackTimer = AttackCooldown;
            _anim?.SetTrigger(ANIM_ATTACK);

            if (venomPrefab == null || _player == null) return;

            Vector2 dir = ((Vector2)(_player.position - transform.position)).normalized;
            SpawnVenom(dir);

            if (isElite) // Spread shot
            {
                SpawnVenom(Quaternion.Euler(0, 0, 20f) * dir);
                SpawnVenom(Quaternion.Euler(0, 0, -20f) * dir);
            }
        }

        private void SpawnVenom(Vector2 dir)
        {
            var proj = ObjectPool.Instance?.Get("Enemy_VenomShot", venomPrefab,
                transform.position, Quaternion.identity);
            if (proj != null && proj.TryGetComponent<Rigidbody2D>(out var rb))
                rb.linearVelocity = dir * 6f;
        }
    }

    /// ─────────────────────────────────────────────────────────────────────
    /// PISACHA — Swarm demon
    /// Very fast, low HP. Explodes on death dealing small AoE damage.
    /// Spawns in groups defined by EnemyData.swarmGroupSize.
    /// ─────────────────────────────────────────────────────────────────────
    public class PisachaEnemy : BaseEnemy
    {
        [SerializeField] private float explosionRadius = 1.5f;
        [SerializeField] private float explosionDamage = 8f;

        public override void Die()
        {
            // AoE explosion on death
            var hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player") && hit.TryGetComponent<HealthSystem>(out var hp))
                    hp.TakeDamage(explosionDamage, gameObject);
            }
            EventBus.Emit<Vector2>("OnPisachaExplode", transform.position);
            base.Die();
        }
    }

    /// ─────────────────────────────────────────────────────────────────────
    /// VETALA — Teleporting stalker
    /// Vanishes and reappears directly behind the player.
    /// Elite variant teleports twice and slows the player briefly.
    /// ─────────────────────────────────────────────────────────────────────
    public class VetalaEnemy : BaseEnemy
    {
        protected override IEnumerator DoSpecialAbility()
        {
            if (_player == null) yield break;

            // Vanish
            if (_sprite != null) _sprite.enabled = false;
            yield return new WaitForSeconds(0.3f);

            // Blink behind player
            Vector2 behind = (Vector2)_player.position -
                ((Vector2)_player.right.normalized) * 1.2f;
            transform.position = behind;

            if (_sprite != null) _sprite.enabled = true;

            if (isElite)
            {
                // Slow the player briefly via EventBus
                EventBus.Emit<float>("OnPlayerSlowed", 1.5f); // 1.5 second slow
                yield return new WaitForSeconds(0.2f);

                // Second blink
                if (_sprite != null) _sprite.enabled = false;
                yield return new WaitForSeconds(0.2f);
                behind = (Vector2)_player.position +
                    new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
                transform.position = behind;
                if (_sprite != null) _sprite.enabled = true;
            }

            PerformAttack();
        }
    }

    /// ─────────────────────────────────────────────────────────────────────
    /// YAKSHA — Shielding tank
    /// Moves slowly but generates a shield aura that reduces damage for
    /// all nearby enemies. Killing a Yaksha removes the shield from allies.
    /// ─────────────────────────────────────────────────────────────────────
    public class YakshaEnemy : BaseEnemy
    {
        private bool _shieldActive = true;

        protected override void Update()
        {
            base.Update();
            if (_shieldActive) ApplyShieldToNearbyEnemies();
        }

        private void ApplyShieldToNearbyEnemies()
        {
            var nearby = Physics2D.OverlapCircleAll(transform.position, data.shieldRadius);
            foreach (var c in nearby)
            {
                if (c.gameObject == gameObject) continue;
                if (c.TryGetComponent<HealthSystem>(out var hp))
                    hp.DamageReductionMult = 0.5f; // 50% damage reduction while Yaksha lives
            }
        }

        public override void Die()
        {
            // Remove shields from nearby enemies when Yaksha dies
            _shieldActive = false;
            var nearby = Physics2D.OverlapCircleAll(transform.position, data.shieldRadius * 2f);
            foreach (var c in nearby)
            {
                if (c.TryGetComponent<HealthSystem>(out var hp))
                    hp.DamageReductionMult = 1f;
            }
            EventBus.Emit<Vector2>("OnYakshaShieldBroken", transform.position);
            base.Die();
        }
    }

    /// ─────────────────────────────────────────────────────────────────────
    /// BRAHMARAKSHASA — Spellcasting summoner
    /// Stays far from the player, periodically summons waves of Pisachas.
    /// Must be targeted and killed to stop the summons.
    /// ─────────────────────────────────────────────────────────────────────
    public class BrahmaRakshasaEnemy : BaseEnemy
    {
        [SerializeField] private GameObject pisachaPrefab;
        [SerializeField] private int        summonCount = 3;
        private const float SAFE_DISTANCE = 8f;

        protected override Vector2 GetMoveDirection()
        {
            if (_player == null) return Vector2.zero;
            float dist = Vector2.Distance(transform.position, _player.position);
            // Always try to maintain SAFE_DISTANCE
            if (dist < SAFE_DISTANCE)
                return ((Vector2)(transform.position - _player.position)).normalized;
            return Vector2.zero;
        }

        protected override IEnumerator DoSpecialAbility()
        {
            if (pisachaPrefab == null) yield break;

            // Summon animation
            _anim?.SetTrigger(ANIM_ATTACK);
            yield return new WaitForSeconds(0.6f);

            // Spawn Pisachas in a ring
            for (int i = 0; i < summonCount; i++)
            {
                float angle = (360f / summonCount) * i;
                Vector2 spawnPos = (Vector2)transform.position +
                    new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad),
                                Mathf.Sin(angle * Mathf.Deg2Rad)) * 1.5f;

                ObjectPool.Instance?.Get("Enemy_Pisacha", pisachaPrefab,
                    spawnPos, Quaternion.identity);
            }
            EventBus.Emit<Vector2>("OnBrahmaRakshasaSummon", transform.position);
        }
    }
}
