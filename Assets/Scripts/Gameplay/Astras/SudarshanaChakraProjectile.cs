using UnityEngine;
using AGNIDAWN.Core;
using AGNIDAWN.Enemies;

namespace AGNIDAWN.Gameplay.Astras
{
    /// <summary>
    /// Sudarshana Chakra — Vishnu's Spinning Disc.
    /// Behaviour: homing projectile — steers smoothly toward the nearest enemy each frame.
    /// If no enemy found, continues in original direction (never wastes a throw).
    /// On expiry, returns to pool (it is a divine disc — it always comes back).
    ///
    /// Phase 7 fix: replaced expensive FindGameObjectsWithTag (O(n) per-frame tag scan)
    /// with SpawnManager.ActiveEnemies registry — O(1) lookup into a maintained list.
    ///
    /// Linear: FAI-11 / FAI-12
    /// </summary>
    public class SudarshanaChakraProjectile : BaseAstraProjectile
    {
        [Tooltip("Degrees per second the Chakra can steer toward its target")]
        [SerializeField] private float turnRate = 180f;

        protected override Vector2 GetCurrentDirection()
        {
            Transform nearest = FindNearestEnemy();
            if (nearest == null) return _direction;

            Vector2 toTarget = ((Vector2)(nearest.position - transform.position)).normalized;
            _direction = Vector2.MoveTowards(_direction, toTarget,
                             turnRate * Mathf.Deg2Rad * Time.deltaTime).normalized;
            return _direction;
        }

        protected override void OnMovementEffect()
        {
            // Spin the sprite visually
            transform.Rotate(0f, 0f, -720f * Time.deltaTime, Space.Self);
        }

        protected override void OnHitEffect(Collider2D other)
        {
            EventBus.Emit<string>("OnAstraSpecial", "Chakra_Hit");
        }

        /// <summary>
        /// Finds the nearest enemy using the SpawnManager registry.
        /// Falls back to null if SpawnManager is unavailable (e.g. in tests).
        /// </summary>
        private Transform FindNearestEnemy()
        {
            var sm = SpawnManager.Instance;
            if (sm == null) return null;

            Transform nearest = null;
            float     minSqrDist = float.MaxValue;
            Vector2   myPos = transform.position;

            var enemies = sm.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || !e.gameObject.activeInHierarchy) continue;

                float sqrDist = Vector2.SqrMagnitude((Vector2)e.transform.position - myPos);
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    nearest = e.transform;
                }
            }

            return nearest;
        }
    }
}
