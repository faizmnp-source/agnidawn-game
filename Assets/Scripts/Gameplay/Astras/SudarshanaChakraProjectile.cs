using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.Gameplay.Astras
{
    /// <summary>
    /// Sudarshana Chakra — Vishnu's Spinning Disc.
    /// Behaviour: homing projectile — steers smoothly toward the nearest enemy each frame.
    /// If no enemy found, continues in original direction (never wastes a throw).
    /// On expiry, returns to pool (it is a divine disc — it always comes back).
    /// Linear: FAI-11
    /// </summary>
    public class SudarshanaChakraProjectile : BaseAstraProjectile
    {
        [Tooltip("Degrees per second the Chakra can steer toward its target")]
        [SerializeField] private float turnRate = 180f;

        protected override Vector2 GetCurrentDirection()
        {
            GameObject nearest = FindNearestEnemy();
            if (nearest == null) return _direction;

            Vector2 toTarget = ((Vector2)(nearest.transform.position - transform.position)).normalized;
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

        private GameObject FindNearestEnemy()
        {
            var enemies = GameObject.FindGameObjectsWithTag("Enemy");
            var bosses  = GameObject.FindGameObjectsWithTag("Boss");

            GameObject nearest = null;
            float      minDist = float.MaxValue;
            Vector2    myPos   = transform.position;

            foreach (var e in enemies) CheckDistance(e, myPos, ref nearest, ref minDist);
            foreach (var b in bosses)  CheckDistance(b, myPos, ref nearest, ref minDist);

            return nearest;
        }

        private static void CheckDistance(GameObject go, Vector2 from, ref GameObject nearest, ref float minDist)
        {
            float d = Vector2.SqrMagnitude((Vector2)go.transform.position - from);
            if (d < minDist) { minDist = d; nearest = go; }
        }
    }
}
