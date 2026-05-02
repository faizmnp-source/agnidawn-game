using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.Enemies
{
    /// <summary>
    /// Wave-based enemy spawner with dynamic difficulty scaling.
    ///
    /// 20-minute session breakdown (vs 20 Min Until Dawn's simpler spawning):
    ///   0:00 -  3:00  →  Asura swarms, intro Pisacha groups
    ///   3:00 -  6:00  →  Naga ranged + Rakshasa flankers introduced
    ///   6:00 -  9:00  →  Yaksha tanks + elite variants begin
    ///   9:00 - 12:00  →  Vetala + BrahmaRakshasa summoners
    ///  12:00 - 15:00  →  Full mix, Yaksha shields, high elite rate
    ///  15:00 - 18:00  →  Boss prep waves — max difficulty
    ///  18:00 - 20:00  →  Boss wave + continuous pressure
    ///
    /// Linear: FAI-8
    /// </summary>
    public class SpawnManager : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────
        [Header("Spawn Radius")]
        [SerializeField] private float minSpawnRadius = 10f;
        [SerializeField] private float maxSpawnRadius = 14f;

        [Header("Wave Config")]
        [SerializeField] private float baseSpawnInterval = 2.5f;  // seconds between spawns
        [SerializeField] private float minSpawnInterval  = 0.4f;  // floor at max difficulty
        [SerializeField] private int   baseEnemiesPerWave = 3;
        [SerializeField] private int   maxEnemiesPerWave  = 12;

        [Header("Enemy Pools (assign in Inspector)")]
        [SerializeField] private EnemyPool[] enemyPools;

        [Header("Elite Settings")]
        [SerializeField] private float eliteChanceBase  = 0.05f;  // 5% at start
        [SerializeField] private float eliteChanceMax   = 0.40f;  // 40% at 20 min

        // ── State ──────────────────────────────────────────────────────────
        private Transform _player;
        private bool      _isRunning;
        private int       _totalSpawned;
        private int       _currentAliveCount;
        private Coroutine _spawnRoutine;

        // ──────────────────────────────────────────────────────────────────
        #region Data Structures

        [System.Serializable]
        public class EnemyPool
        {
            public EnemyData   data;
            public GameObject  prefab;
            public float       unlockAtMinute = 0f; // when this type starts spawning
            [Range(0f, 1f)]
            public float       spawnWeight    = 1f;
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void OnEnable()
        {
            EventBus.On(GameManager.EVT_GAME_START,   OnGameStart);
            EventBus.On(GameManager.EVT_GAME_PAUSE,   OnPause);
            EventBus.On(GameManager.EVT_GAME_RESUME,  OnResume);
            EventBus.On(GameManager.EVT_GAME_OVER,    OnStop);
            EventBus.On(GameManager.EVT_VICTORY,      OnStop);
            EventBus.On<GameObject>("OnEnemyDied",    OnEnemyDied);
        }

        private void OnDisable()
        {
            EventBus.Off(GameManager.EVT_GAME_START,  OnGameStart);
            EventBus.Off(GameManager.EVT_GAME_PAUSE,  OnPause);
            EventBus.Off(GameManager.EVT_GAME_RESUME, OnResume);
            EventBus.Off(GameManager.EVT_GAME_OVER,   OnStop);
            EventBus.Off(GameManager.EVT_VICTORY,     OnStop);
            EventBus.Off<GameObject>("OnEnemyDied",   OnEnemyDied);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Spawning

        private IEnumerator SpawnLoop()
        {
            while (_isRunning)
            {
                float elapsed  = GameManager.Instance.ElapsedTime;
                float minutes  = elapsed / 60f;
                float t        = Mathf.Clamp01(minutes / 20f); // 0 at start, 1 at 20 min

                // Dynamic interval
                float interval = Mathf.Lerp(baseSpawnInterval, minSpawnInterval, t);

                // Dynamic wave size
                int count = Mathf.RoundToInt(Mathf.Lerp(baseEnemiesPerWave, maxEnemiesPerWave, t));

                // Spawn wave
                for (int i = 0; i < count; i++)
                {
                    SpawnOne(minutes, t);
                    yield return new WaitForSeconds(0.15f); // stagger within wave
                }

                GameManager.Instance?.IncrementWave();
                yield return new WaitForSeconds(interval);
            }
        }

        private void SpawnOne(float minutes, float t)
        {
            if (_player == null) FindPlayer();
            if (_player == null) return;

            var pool = PickEnemyPool(minutes);
            if (pool == null) return;

            Vector2 spawnPos = GetSpawnPosition();
            bool    elite    = ShouldSpawnElite(t);

            var go = ObjectPool.Instance?.Get(
                $"Enemy_{pool.data.enemyId}",
                pool.prefab,
                spawnPos,
                Quaternion.identity);

            if (go != null && go.TryGetComponent<BaseEnemy>(out _))
            {
                _totalSpawned++;
                _currentAliveCount++;
                EventBus.Emit<Vector2>("OnEnemySpawned", spawnPos);

                // Handle swarm — spawn the group together
                if (pool.data.enemyType == EnemyType.Pisacha && pool.data.swarmGroupSize > 1)
                {
                    for (int i = 1; i < pool.data.swarmGroupSize; i++)
                    {
                        Vector2 offset = Random.insideUnitCircle * 1.5f;
                        ObjectPool.Instance?.Get($"Enemy_{pool.data.enemyId}",
                            pool.prefab, spawnPos + offset, Quaternion.identity);
                        _currentAliveCount++;
                    }
                }
            }
        }

        private EnemyPool PickEnemyPool(float minutes)
        {
            var available = new List<EnemyPool>();
            float totalWeight = 0f;

            foreach (var pool in enemyPools)
            {
                if (pool?.data == null) continue;
                if (minutes < pool.unlockAtMinute) continue;
                available.Add(pool);
                totalWeight += pool.spawnWeight;
            }

            if (available.Count == 0) return null;

            float roll = Random.value * totalWeight;
            float cumulative = 0f;
            foreach (var pool in available)
            {
                cumulative += pool.spawnWeight;
                if (roll <= cumulative) return pool;
            }
            return available[available.Count - 1];
        }

        private Vector2 GetSpawnPosition()
        {
            if (_player == null) return Vector2.zero;
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = Random.Range(minSpawnRadius, maxSpawnRadius);
            return (Vector2)_player.position + new Vector2(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius);
        }

        private bool ShouldSpawnElite(float t)
            => Random.value < Mathf.Lerp(eliteChanceBase, eliteChanceMax, t);

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Event Handlers

        private void OnGameStart()
        {
            _isRunning = true;
            _totalSpawned = 0;
            _currentAliveCount = 0;
            FindPlayer();
            if (_spawnRoutine != null) StopCoroutine(_spawnRoutine);
            _spawnRoutine = StartCoroutine(SpawnLoop());
        }

        private void OnPause()  { _isRunning = false; }
        private void OnResume() { _isRunning = true; }
        private void OnStop()
        {
            _isRunning = false;
            if (_spawnRoutine != null) StopCoroutine(_spawnRoutine);
        }

        private void OnEnemyDied(GameObject _)
        {
            _currentAliveCount = Mathf.Max(0, _currentAliveCount - 1);
        }

        private void FindPlayer()
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) _player = go.transform;
        }

        #endregion
    }
}
