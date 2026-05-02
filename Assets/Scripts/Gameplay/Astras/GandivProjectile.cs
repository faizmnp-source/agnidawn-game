using UnityEngine;
using AGNIDAWN.Player;
using AGNIDAWN.Core;

namespace AGNIDAWN.Gameplay.Astras
{
    /// <summary>
    /// Gandiv — Arjuna's Divine Bow.
    /// Behaviour: extremely fast arrow with pierce 3.
    /// On each hit, fires a second "volley" shot toward the nearest OTHER enemy
    /// within radius 5f (simulating Arjuna's legendary marksmanship — one nock, many arrows).
    /// Max one volley per hit to avoid infinite chains.
    /// Linear: FAI-11
    /// </summary>
    public class GandivProjectile : BaseAstraProjectile
    {
        private const float VolleyRadius   = 5f;
        private const float VolleyDamagePct = 0.6f; // 60% of base damage

        [SerializeField] private GameObject volleyPrefab; // assign in prefab; falls back to self-prefab

        protected override void OnAfterInit()
        {
            // Gandiv is fast — speed is already set by AstraController (high in AstraData)
        }

        protected override void OnHitEffect(Collider2D primaryHit)
        {
            FireVolleyShotAt(primaryHit.gameObject);
        }

        private void FireVolleyShotAt(GameObject excludeTarget)
        {
            // Find nearest enemy that is NOT the one we just hit
            var hits = Physics2D.OverlapCircleAll(transform.position, VolleyRadius);
            GameObject nearest = null;
            float minDist      = float.MaxValue;

            foreach (var h in hits)
            {
                if (h.gameObject == excludeTarget) continue;
                if (!h.CompareTag("Enemy") && !h.CompareTag("Boss")) continue;

                float d = Vector2.SqrMagnitude((Vector2)transform.position - (Vector2)h.transform.position);
                if (d < minDist) { minDist = d; nearest = h.gameObject; }
            }

            if (nearest == null) return;

            Vector2 dir = ((Vector2)(nearest.transform.position - transform.position)).normalized;

            // Deal volley damage directly (no extra projectile required for simple implementation)
            if (nearest.TryGetComponent<HealthSystem>(out var health))
                health.TakeDamage(_damage * VolleyDamagePct, gameObject);

            EventBus.Emit<float, Vector2>("OnProjectileHit", _damage * VolleyDamagePct, nearest.transform.position);
            EventBus.Emit<string>("OnAstraSpecial", "Gandiv_Volley");
        }
    }
}
