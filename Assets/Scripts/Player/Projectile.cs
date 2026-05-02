using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.Player
{
    /// <summary>
    /// Poolable projectile fired by Astras.
    /// Moves in a direction, deals damage on contact, returns to pool on expiry or pierce-out.
    /// Linear: FAI-7
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [Header("Lifetime")]
        [SerializeField] private float maxLifetime = 4f;

        // ── Runtime state (set by AstraController on Init) ─────────────────
        private float   _damage;
        private float   _speed;
        private int     _pierceRemaining;
        private Vector2 _direction;
        private string  _poolKey;
        private float   _lifetime;

        // ──────────────────────────────────────────────────────────────────
        #region Init / Pool lifecycle

        public void Init(float damage, float speed, int pierce, Vector2 direction, string poolKey)
        {
            _damage          = damage;
            _speed           = speed;
            _pierceRemaining = pierce;
            _direction       = direction.normalized;
            _poolKey         = poolKey;
            _lifetime        = 0f;
        }

        private void OnEnable()  => _lifetime = 0f;

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Movement & Lifetime

        private void Update()
        {
            transform.Translate(_direction * _speed * Time.deltaTime, Space.World);

            _lifetime += Time.deltaTime;
            if (_lifetime >= maxLifetime) ReturnToPool();
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Collision

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Enemy") && !other.CompareTag("Boss")) return;

            if (other.TryGetComponent<HealthSystem>(out var health))
                health.TakeDamage(_damage, gameObject);

            EventBus.Emit<float, Vector2>("OnProjectileHit", _damage, transform.position);

            _pierceRemaining--;
            if (_pierceRemaining < 0) ReturnToPool();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            // Optionally handle arena boundaries
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Pool Return

        private void ReturnToPool()
            => ObjectPool.Instance?.Return(_poolKey, gameObject);

        #endregion
    }
}
