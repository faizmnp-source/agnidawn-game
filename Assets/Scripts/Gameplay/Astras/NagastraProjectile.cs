using System.Collections;
using UnityEngine;
using AGNIDAWN.Player;
using AGNIDAWN.Core;

namespace AGNIDAWN.Gameplay.Astras
{
    /// <summary>
    /// Nagastra — Weapon of the Serpent Lords.
    /// Behaviour: on first hit, deals base damage and applies a 5-tick poison
    /// DoT (10% base damage per tick, every 0.8 s for 4 s).
    /// Additionally finds up to 2 nearby enemies in radius 2.5f and applies
    /// the same poison to them (simulating the serpent splitting into many snakes).
    /// Linear: FAI-11
    /// </summary>
    public class NagastraProjectile : BaseAstraProjectile
    {
        private const float PoisonTickPct   = 0.10f; // 10% per tick
        private const int   PoisonTicks     = 5;
        private const float PoisonInterval  = 0.8f;
        private const float SplitRadius     = 2.5f;
        private const int   SplitTargets    = 2;

        private bool _poisonApplied;

        protected override void OnAfterInit()
        {
            _poisonApplied = false;
        }

        protected override void OnHitEffect(Collider2D primary)
        {
            // Apply poison to primary target
            if (primary.TryGetComponent<HealthSystem>(out var primaryHealth))
                StartCoroutine(ApplyPoison(primaryHealth));

            // Snake split — spread poison to nearby enemies
            if (!_poisonApplied)
            {
                _poisonApplied = true;
                SpreadToNearby(primary.gameObject);
                EventBus.Emit<string>("OnAstraSpecial", "Nagastra_PoisonSpread");
            }
        }

        private void SpreadToNearby(GameObject excludeTarget)
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, SplitRadius);
            int spread = 0;

            foreach (var h in hits)
            {
                if (spread >= SplitTargets) break;
                if (h.gameObject == excludeTarget) continue;
                if (!h.CompareTag("Enemy") && !h.CompareTag("Boss")) continue;

                if (h.TryGetComponent<HealthSystem>(out var health))
                {
                    StartCoroutine(ApplyPoison(health));
                    spread++;
                }
            }
        }

        private IEnumerator ApplyPoison(HealthSystem target)
        {
            float tickDamage = _damage * PoisonTickPct;
            for (int i = 0; i < PoisonTicks; i++)
            {
                yield return new WaitForSeconds(PoisonInterval);
                if (target == null || !target.gameObject.activeInHierarchy) yield break;
                target.TakeDamage(tickDamage, gameObject);
                EventBus.Emit<float, Vector2>("OnProjectileHit", tickDamage, target.transform.position);
            }
        }
    }
}
