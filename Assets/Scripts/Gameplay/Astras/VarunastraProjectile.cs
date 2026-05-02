using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.Gameplay.Astras
{
    /// <summary>
    /// Varunastra — Varuna's Weapon of the Cosmic Ocean.
    /// Behaviour: on hit, pulses an AoE slow — all enemies within radius 3f
    /// are slowed (move-speed halved) for 3 seconds via EventBus.
    /// The projectile leaves a visual wake (scales up slightly while alive).
    /// Linear: FAI-11
    /// </summary>
    public class VarunastraProjectile : BaseAstraProjectile
    {
        [SerializeField] private float slowRadius   = 3f;
        [SerializeField] private float slowDuration = 3f;
        [SerializeField] private float slowFactor   = 0.5f; // 50% speed reduction

        protected override void OnMovementEffect()
        {
            // Subtle ripple pulse on X scale (water-like undulation)
            float pulse = 1f + Mathf.Sin(_lifetime * 8f) * 0.08f;
            transform.localScale = new Vector3(pulse, 1f, 1f);
        }

        protected override void OnHitEffect(Collider2D primary)
        {
            ApplySlowInRadius();
        }

        private void ApplySlowInRadius()
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, slowRadius);
            foreach (var h in hits)
            {
                if (!h.CompareTag("Enemy") && !h.CompareTag("Boss")) continue;

                // Broadcast a slow event — enemies must listen to "OnSlowApplied"
                // and implement the timed debuff on their movement component.
                EventBus.Emit<GameObject, float, float>(
                    "OnSlowApplied",
                    h.gameObject,
                    slowFactor,
                    slowDuration
                );
            }
            EventBus.Emit<string>("OnAstraSpecial", "Varunastra_Slow");
        }
    }
}
