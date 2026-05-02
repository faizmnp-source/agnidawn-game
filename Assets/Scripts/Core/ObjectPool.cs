using System.Collections.Generic;
using UnityEngine;

namespace AGNIDAWN.Core
{
    /// <summary>
    /// Generic Object Pool. Eliminates runtime Instantiate/Destroy calls for
    /// bullets, enemies, VFX particles, and UI popups.
    ///
    /// Usage:
    ///   var go = ObjectPool.Instance.Get("Bullet_Trishul", prefab, transform.position, rotation);
    ///   ObjectPool.Instance.Return("Bullet_Trishul", go);
    ///
    /// Linear: FAI-6
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────
        public static ObjectPool Instance { get; private set; }

        // ── Internal pool data ────────────────────────────────────────────
        private class Pool
        {
            public GameObject       prefab;
            public Queue<GameObject> inactive = new Queue<GameObject>();
            public Transform         parent;
        }

        private readonly Dictionary<string, Pool> _pools = new Dictionary<string, Pool>();

        // ── Config ────────────────────────────────────────────────────────
        [Header("Pool Config")]
        [SerializeField] private int defaultPrewarmCount = 10;
        [SerializeField] private Transform poolRoot;

        // ──────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (poolRoot == null)
            {
                var go = new GameObject("_PoolRoot");
                go.transform.SetParent(transform);
                poolRoot = go.transform;
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Public API

        /// <summary>Pre-warm a pool before it's needed (e.g. at level start).</summary>
        public void Prewarm(string key, GameObject prefab, int count = -1)
        {
            var pool = GetOrCreatePool(key, prefab);
            int n = count < 0 ? defaultPrewarmCount : count;
            for (int i = 0; i < n; i++)
            {
                var obj = CreateNew(pool);
                obj.SetActive(false);
                pool.inactive.Enqueue(obj);
            }
        }

        /// <summary>Get a pooled object (creates one if pool is empty).</summary>
        public GameObject Get(string key, GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var pool = GetOrCreatePool(key, prefab);
            GameObject obj;

            if (pool.inactive.Count > 0)
            {
                obj = pool.inactive.Dequeue();
                // Object might have been destroyed externally
                if (obj == null)
                {
                    obj = CreateNew(pool);
                }
            }
            else
            {
                obj = CreateNew(pool);
            }

            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            return obj;
        }

        /// <summary>Return an object to its pool.</summary>
        public void Return(string key, GameObject obj)
        {
            if (obj == null) return;

            obj.SetActive(false);

            if (_pools.TryGetValue(key, out var pool))
            {
                obj.transform.SetParent(pool.parent);
                pool.inactive.Enqueue(obj);
            }
            else
            {
                // Pool doesn't exist anymore — just destroy
                Destroy(obj);
            }
        }

        /// <summary>Return after a delay (fire-and-forget).</summary>
        public void ReturnDelayed(string key, GameObject obj, float delay)
            => StartCoroutine(ReturnAfterDelay(key, obj, delay));

        /// <summary>Clear and destroy all objects in a specific pool.</summary>
        public void DestroyPool(string key)
        {
            if (!_pools.TryGetValue(key, out var pool)) return;
            while (pool.inactive.Count > 0)
            {
                var obj = pool.inactive.Dequeue();
                if (obj != null) Destroy(obj);
            }
            if (pool.parent != null) Destroy(pool.parent.gameObject);
            _pools.Remove(key);
        }

        /// <summary>Clear ALL pools (call on scene unload).</summary>
        public void DestroyAll()
        {
            foreach (var key in new List<string>(_pools.Keys))
                DestroyPool(key);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Private Helpers

        private Pool GetOrCreatePool(string key, GameObject prefab)
        {
            if (!_pools.TryGetValue(key, out var pool))
            {
                pool = new Pool { prefab = prefab };
                var parentGo = new GameObject($"Pool_{key}");
                parentGo.transform.SetParent(poolRoot);
                pool.parent = parentGo.transform;
                _pools[key] = pool;
            }
            return pool;
        }

        private GameObject CreateNew(Pool pool)
        {
            var obj = Instantiate(pool.prefab, pool.parent);
            return obj;
        }

        private System.Collections.IEnumerator ReturnAfterDelay(string key, GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            Return(key, obj);
        }

        #endregion
    }
}
