using UnityEngine;
using AGNIDAWN.Player;
using AGNIDAWN.Core;

namespace AGNIDAWN.Gameplay.Astras
{
    /// <summary>
    /// Vayuastra — Vayu's Weapon of the Wind God.
    /// Behaviour: extremely high speed. On hit:
    ///   1. Knocks the struck enemy back (force via EventBus "OnKnockbackApplied").
    ///   2. Ricochets — redirects toward the next nearest enemy within radius 4f.
    ///   Max 3 ricochets per projectile (then returns to pool normally).
    /// Linear: FAI-11
    /// </summary>
    public class VayuastraProjectile : BaseAstraProjectile
    {
        [SerializeField] private float knockbackForce  = 8f;
        [SerializeField] private float ricochetRadius  = 4f;
        [SerializeField] private int   maxRicochets    = 3;

        private int     _ricochetCount;
        private float   _damageFalloff = 1f; // each ricochet reduces damage 20%

        private const float SpeedMultiplier   = 2.0f;
        private const float DamageFalloffRate = 0.8f;

        protected override void OnAfterInit()
        {
            _ricochetCount = 0;
            _damageFalloff = 1f;
            _speed        *= SpeedMultiplier;
        }

        protected override void OnHitEffect(Collider2D struck)
        {
            // 1 — Knockback
            EventBus.Emit<GameObject, Vector2, float>(
                "OnKnockbackApplied",
                struck.gameObject,
                _direction,
                knockbackForce
            );

            // 2 — Ricochet
            if (_ricochetCount < maxRicochets)
            {
                GameObject next = FindNextTarget(struck.gameObject);
                if (next != null)
                {
                    _ricochetCount++;
                    _damageFalloff *= DamageFalloffRate;
                    _damage        *= _damageFalloff;
                    _direction      = ((Vector2)(next.transform.position - transform.position)).normalized;
                    _pierceRemaining++; // restore pierce for the ricochet hit
                    EventBus.Emit<string>("OnAstraSpecial", $"Vayuastra_Ricochet_{_ricochetCount}");
                }
            }
        }

        private GameObject FindNextTarget(GameObject excludeTarget)
        {
            var hits    = Physics2D.OverlapCircleAll(transform.position, ricochetRadius);
            GameObject nearest = null;
            float minDist = float.MaxValue;
            Vector2 myPos = transform.position;

            foreach (var h in hits)
            {
                if (h.gameObject == excludeTarget) continue;
                if (!h.CompareTag("Enemy") && !h.CompareTag("Boss")) continue;
                float d = Vector2.SqrMagnitude((Vector2)h.transform.position - myPos);
                if (d < minDist) { minDist = d; nearest = h.gameObject; }
            }
            return nearest;
        }
    }
}
