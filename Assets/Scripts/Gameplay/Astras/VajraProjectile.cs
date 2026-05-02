using UnityEngine;
using AGNIDAWN.Player;
using AGNIDAWN.Core;

namespace AGNIDAWN.Gameplay.Astras
{
    /// <summary>
    /// Vajra — Indra's Thunderbolt.
    /// Behaviour: on impact, chains to up to 2 nearest enemies within radius 4f,
    /// dealing 60% of original damage to each chain target.
    /// Chain does NOT further chain (single hop only).
    /// Emits "OnLightningChain" EventBus event for VFX listeners.
    /// Linear: FAI-11
    /// </summary>
    public class VajraProjectile : BaseAstraProjectile
    {
        [SerializeField] private float chainRadius      = 4f;
        [SerializeField] private int   maxChainTargets  = 2;
        [SerializeField] private float chainDamagePct   = 0.60f;

        protected override void OnHitEffect(Collider2D primary)
        {
            ChainLightning(primary.gameObject);
        }

        private void ChainLightning(GameObject excludeTarget)
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, chainRadius);

            // Sort by distance
            int chainCount = 0;
            float minDist1 = float.MaxValue, minDist2 = float.MaxValue;
            GameObject target1 = null, target2 = null;
            Vector2 myPos = transform.position;

            foreach (var h in hits)
            {
                if (h.gameObject == excludeTarget) continue;
                if (!h.CompareTag("Enemy") && !h.CompareTag("Boss")) continue;

                float d = Vector2.SqrMagnitude((Vector2)h.transform.position - myPos);
                if (d < minDist1)
                {
                    minDist2 = minDist1; target2 = target1;
                    minDist1 = d;        target1 = h.gameObject;
                }
                else if (d < minDist2)
                {
                    minDist2 = d; target2 = h.gameObject;
                }
            }

            // Chain to target1
            if (target1 != null)
            {
                DealChainDamage(target1);
                EventBus.Emit<Vector2, Vector2>("OnLightningChain",
                    (Vector2)excludeTarget.transform.position,
                    (Vector2)target1.transform.position);
                chainCount++;
            }

            // Chain to target2
            if (maxChainTargets >= 2 && target2 != null)
            {
                DealChainDamage(target2);
                EventBus.Emit<Vector2, Vector2>("OnLightningChain",
                    (Vector2)excludeTarget.transform.position,
                    (Vector2)target2.transform.position);
                chainCount++;
            }

            if (chainCount > 0)
                EventBus.Emit<string>("OnAstraSpecial", $"Vajra_Chain_{chainCount}");
        }

        private void DealChainDamage(GameObject target)
        {
            if (target.TryGetComponent<HealthSystem>(out var health))
            {
                float chainDmg = _damage * chainDamagePct;
                health.TakeDamage(chainDmg, gameObject);
                EventBus.Emit<float, Vector2>("OnProjectileHit", chainDmg, target.transform.position);
            }
        }
    }
}
