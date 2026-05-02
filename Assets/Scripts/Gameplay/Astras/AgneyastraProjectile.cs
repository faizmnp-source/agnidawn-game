using System.Collections;
using UnityEngine;
using AGNIDAWN.Player;
using AGNIDAWN.Core;

namespace AGNIDAWN.Gameplay.Astras
{
    /// <summary>
    /// Agneyastra — Agni's Weapon of Sacred Fire.
    /// Behaviour: on impact, places a Fire Zone at hit position.
    /// Fire Zone persists for 5 seconds, dealing 15% of impact damage
    /// per second to all enemies within radius 1.5f (burn DoT via coroutine).
    /// Multiple overlapping fire zones stack independently.
    /// Linear: FAI-11
    /// </summary>
    public class AgneyastraProjectile : BaseAstraProjectile
    {
        [SerializeField] private float fireDuration   = 5f;
        [SerializeField] private float fireRadius     = 1.5f;
        [SerializeField] private float fireTickPct    = 0.15f; // 15% per second
        [SerializeField] private float fireTickRate   = 1.0f;  // seconds between ticks

        protected override void OnHitEffect(Collider2D other)
        {
            StartFireZone(transform.position);
            EventBus.Emit<string>("OnAstraSpecial", "Agneyastra_FireZone");
        }

        protected override void OnLifetimeExpired()
        {
            // No hit reached — still drop a fire zone at current position
            StartFireZone(transform.position);
            ReturnToPool();
        }

        private void StartFireZone(Vector2 position)
        {
            // Spawn a persistent fire-zone GameObject (simple; no pool needed as it's rare)
            var zone = new GameObject("FireZone_Agni");
            zone.transform.position = position;
            zone.AddComponent<AgneyastraFireZone>().Initialise(
                _damage * fireTickPct,
                fireRadius,
                fireDuration,
                fireTickRate,
                gameObject
            );
        }
    }

    /// <summary>
    /// Self-destructing fire zone component spawned by AgneyastraProjectile.
    /// Applies burn DoT to all enemies within radius each tick.
    /// </summary>
    public class AgneyastraFireZone : MonoBehaviour
    {
        private float   _tickDamage;
        private float   _radius;
        private float   _duration;
        private float   _tickRate;
        private GameObject _source;

        public void Initialise(float tickDamage, float radius, float duration, float tickRate, GameObject source)
        {
            _tickDamage = tickDamage;
            _radius     = radius;
            _duration   = duration;
            _tickRate   = tickRate;
            _source     = source;
            StartCoroutine(BurnLoop());
        }

        private IEnumerator BurnLoop()
        {
            float elapsed = 0f;
            while (elapsed < _duration)
            {
                yield return new WaitForSeconds(_tickRate);
                elapsed += _tickRate;

                var hits = Physics2D.OverlapCircleAll(transform.position, _radius);
                foreach (var h in hits)
                {
                    if (!h.CompareTag("Enemy") && !h.CompareTag("Boss")) continue;
                    if (h.TryGetComponent<HealthSystem>(out var health))
                    {
                        health.TakeDamage(_tickDamage, _source);
                        EventBus.Emit<float, Vector2>("OnProjectileHit", _tickDamage, h.transform.position);
                    }
                }
            }
            Destroy(gameObject);
        }
    }
}
