using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.Player
{
    /// <summary>
    /// Poolable projectile fired by Astras.
    /// Moves in a direction, deals damage on contact, returns to pool on expiry or pierce-out.
    /// Subclasses (e.g. divine Astra projectiles in AGNIDAWN.Gameplay) may override virtual hooks
    /// to add unique trajectory, hit-effect, and on-init behaviour.
    /// Linear: FAI-7 / FAI-11
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [Header("Lifetime")]
        [SerializeField] protected float maxLifetime = 4f;

        // ── Runtime state (set by AstraController on Init) ─────────────────
        protected float   _damage;
        protected float   _speed;
        protected int     _pierceRemaining;
        protected Vector2 _direction;
        protected string  _poolKey;
        protected float   _lifetime;

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
            OnAfterInit();
        }

        /// <summary>Called at the end of Init(). Override for per-astra setup.</summary>
        protected virtual void OnAfterInit() { }

        protected virtual void OnEnable()  => _lifetime = 0f;

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Movement & Lifetime

        protected virtual void Update()
        {
            _direction = GetCurrentDirection();
            transform.Translate(_direction * _speed * Time.deltaTime, Space.World);

            _lifetime += Time.deltaTime;
            OnMovementEffect();
            if (_lifetime >= maxLifetime) OnLifetimeExpired();
        }

        /// <summary>Override to steer the projectile each frame (e.g. homing).</summary>
        protected virtual Vector2 GetCurrentDirection() => _direction;

        /// <summary>Called each frame after movement. Override for trail/scale effects.</summary>
        protected virtual void OnMovementEffect() { }

        /// <summary>Called when lifetime expires. Base returns to pool; override for detonation.</summary>
        protected virtual void OnLifetimeExpired() => ReturnToPool();

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Collision

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Enemy") && !other.CompareTag("Boss")) return;

            if (other.TryGetComponent<HealthSystem>(out var health))
                health.TakeDamage(_damage, gameObject);

            EventBus.Emit<float, Vector2>("OnProjectileHit", _damage, transform.position);

            OnHitEffect(other);

            _pierceRemaining--;
            if (_pierceRemaining < 0) ReturnToPool();
        }

        /// <summary>Called after base hit logic on each valid collision. Override for special effects.</summary>
        protected virtual void OnHitEffect(Collider2D other) { }

        protected virtual void OnTriggerExit2D(Collider2D other)
        {
            // Optionally handle arena boundaries
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Pool Return

        protected void ReturnToPool()
            => ObjectPool.Instance?.Return(_poolKey, gameObject);

        #endregion
    }
}
