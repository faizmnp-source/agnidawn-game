using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.Gameplay.Astras
{
    /// <summary>
    /// Trishul — Shiva's Divine Trident.
    /// Behaviour: boomerang — travels forward for the first half of lifetime,
    /// then reverses direction back toward the fire origin.
    /// Pierce: 2 (hits enemies both ways).
    /// Linear: FAI-11
    /// </summary>
    public class TrishulProjectile : BaseAstraProjectile
    {
        private Vector2 _origin;
        private bool    _returning;

        protected override void OnAfterInit()
        {
            _origin    = transform.position;
            _returning = false;
        }

        protected override Vector2 GetCurrentDirection()
        {
            // Switch to return arc at half lifetime
            if (!_returning && _lifetime >= maxLifetime * 0.5f)
            {
                _returning = true;
                _direction = (_origin - (Vector2)transform.position).normalized;
                EventBus.Emit<string>("OnAstraSpecial", "Trishul_Return");
            }
            return _direction;
        }

        protected override void OnHitEffect(Collider2D other)
        {
            // Trishul pierces enemies on both forward and return trips — no extra logic needed
            // (pierce counter is handled by base)
        }

        protected override void OnLifetimeExpired()
        {
            // Snap back to pool once it completes the round trip
            ReturnToPool();
        }
    }
}
