using UnityEngine;
using AGNIDAWN.Player;
using AGNIDAWN.Core;

namespace AGNIDAWN.Gameplay.Astras
{
    /// <summary>
    /// Pashupatastra — Shiva's Ultimate Destroyer.
    /// Behaviour: pierces ALL enemies with no limit. Grows in scale as it travels.
    /// On lifetime expiry, detonates a final AoE explosion — the weapon always ends
    /// with destruction regardless of remaining targets.
    /// Linear: FAI-11
    /// </summary>
    public class PashupatastraProjectile : BaseAstraProjectile
    {
        [SerializeField] private float explosionRadius = 4f;
        private int _enemiesHit;

        protected override void OnAfterInit()
        {
            _pierceRemaining = 999; // unlimited pierce
            _enemiesHit      = 0;
        }

        protected override void OnMovementEffect()
        {
            // Grow dramatically — Pashupatastra consumes everything in its path
            float s = Mathf.Lerp(0.3f, 3.5f, _lifetime / maxLifetime);
            transform.localScale = Vector3.one * s;
        }

        protected override void OnHitEffect(Collider2D other)
        {
            _enemiesHit++;
            EventBus.Emit<string>("OnAstraSpecial", $"Pashupatastra_Hit_{_enemiesHit}");
        }

        protected override void OnLifetimeExpired()
        {
            FinalDetonation();
        }

        private void FinalDetonation()
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
            foreach (var h in hits)
            {
                if (!h.CompareTag("Enemy") && !h.CompareTag("Boss")) continue;
                if (h.TryGetComponent<HealthSystem>(out var health))
                    health.TakeDamage(_damage * 1.5f, gameObject); // 150% damage on detonation
            }
            EventBus.Emit<float, Vector2>("OnAoEDetonation", explosionRadius, transform.position);
            EventBus.Emit<string>("OnAstraSpecial", "Pashupatastra_FinalDetonation");
            ReturnToPool();
        }
    }
}
