using UnityEngine;
using AGNIDAWN.Player;
using AGNIDAWN.Core;

namespace AGNIDAWN.Gameplay.Astras
{
    /// <summary>
    /// Brahmastra — Brahma's Absolute Weapon.
    /// Behaviour: slow, massive projectile. On impact creates an AoE explosion
    /// (radius 3f) dealing full damage to all enemies in range.
    /// The projectile expands as it travels (scale grows over lifetime).
    /// Linear: FAI-11
    /// </summary>
    public class BrahmastraProjectile : BaseAstraProjectile
    {
        [SerializeField] private float explosionRadius = 3f;

        private const float SpeedMultiplier = 0.4f; // Brahmastra is slow but devastating

        protected override void OnAfterInit()
        {
            _speed *= SpeedMultiplier;
        }

        protected override void OnMovementEffect()
        {
            // Grow proportionally as it travels — cosmic scale
            float t = Mathf.Clamp01(_lifetime / maxLifetime);
            float s = Mathf.Lerp(0.5f, 2.0f, t);
            transform.localScale = Vector3.one * s;
        }

        protected override void OnHitEffect(Collider2D primaryHit)
        {
            Detonate();
        }

        protected override void OnLifetimeExpired()
        {
            Detonate();
        }

        private void Detonate()
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
            foreach (var h in hits)
            {
                if (!h.CompareTag("Enemy") && !h.CompareTag("Boss")) continue;
                if (h.TryGetComponent<HealthSystem>(out var health))
                    health.TakeDamage(_damage, gameObject);
            }
            EventBus.Emit<float, Vector2>("OnAoEDetonation", explosionRadius, transform.position);
            EventBus.Emit<string>("OnAstraSpecial", "Brahmastra_Detonation");
            ReturnToPool();
        }
    }
}
