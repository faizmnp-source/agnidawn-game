using System.Collections;
using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.Gameplay
{
    /// <summary>
    /// Rotates through the 5 mythological biomes across a 20-minute session.
    ///
    /// Default schedule (configurable via biomeSequence in Inspector):
    ///   0:00 -  4:00  Forest    (Dandaka Vana)
    ///   4:00 -  8:00  Desert    (Thar Marustan)
    ///   8:00 - 12:00  Mountain  (Himavat)
    ///  12:00 - 16:00  Ocean     (Samudra)
    ///  16:00 - 20:00  Underworld (Patala)
    ///
    /// Emits:
    ///   OnBiomeEntered (BiomeData)  — when a biome becomes fully active
    ///   OnBiomeExited  (BiomeData)  — when a biome is about to end
    ///
    /// SpawnManager + BaseEnemy read modifiers from BiomeManager.Current.
    ///
    /// Linear: FAI-12
    /// </summary>
    public class BiomeManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────
        public static BiomeManager Instance { get; private set; }

        // ── EventBus event names ───────────────────────────────────────────
        public const string EVT_BIOME_ENTERED = "OnBiomeEntered";
        public const string EVT_BIOME_EXITED  = "OnBiomeExited";

        // ── Inspector ──────────────────────────────────────────────────────
        [Header("Biome Sequence (ordered, should total 20 min)")]
        [SerializeField] private BiomeData[] biomeSequence;

        [Header("Fallback if biomeSequence is empty")]
        [SerializeField] private BiomeData defaultBiome;

        // ── Public state ───────────────────────────────────────────────────
        /// <summary>The currently active biome, or null before game start.</summary>
        public BiomeData Current { get; private set; }

        /// <summary>Index of the currently active biome in biomeSequence.</summary>
        public int CurrentIndex { get; private set; } = -1;

        // ── Private state ──────────────────────────────────────────────────
        private bool      _isRunning;
        private Coroutine _biomeRoutine;

        // ──────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.On(GameManager.EVT_GAME_START,  OnGameStart);
            EventBus.On(GameManager.EVT_GAME_OVER,   OnGameStop);
            EventBus.On(GameManager.EVT_VICTORY,     OnGameStop);
            EventBus.On(GameManager.EVT_GAME_PAUSE,  OnPause);
            EventBus.On(GameManager.EVT_GAME_RESUME, OnResume);
        }

        private void OnDisable()
        {
            EventBus.Off(GameManager.EVT_GAME_START,  OnGameStart);
            EventBus.Off(GameManager.EVT_GAME_OVER,   OnGameStop);
            EventBus.Off(GameManager.EVT_VICTORY,     OnGameStop);
            EventBus.Off(GameManager.EVT_GAME_PAUSE,  OnPause);
            EventBus.Off(GameManager.EVT_GAME_RESUME, OnResume);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Event Handlers

        private void OnGameStart()
        {
            _isRunning = true;
            CurrentIndex = -1;
            if (_biomeRoutine != null) StopCoroutine(_biomeRoutine);
            _biomeRoutine = StartCoroutine(BiomeCycleRoutine());
        }

        private void OnGameStop()
        {
            _isRunning = false;
            if (_biomeRoutine != null)
            {
                StopCoroutine(_biomeRoutine);
                _biomeRoutine = null;
            }
        }

        private void OnPause()  { _isRunning = false; }
        private void OnResume() { _isRunning = true;  }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Biome Cycle

        private IEnumerator BiomeCycleRoutine()
        {
            if (biomeSequence == null || biomeSequence.Length == 0)
            {
                // Fallback: single biome for entire run
                if (defaultBiome != null)
                    yield return ActivateBiome(defaultBiome);
                yield break;
            }

            for (int i = 0; i < biomeSequence.Length; i++)
            {
                if (biomeSequence[i] == null) continue;

                yield return ActivateBiome(biomeSequence[i]);

                // Wait for the biome's duration (respecting pause state)
                float elapsed = 0f;
                float duration = biomeSequence[i].activeDurationMinutes * 60f;

                while (elapsed < duration)
                {
                    if (_isRunning) elapsed += Time.deltaTime;
                    yield return null;
                }

                // Notify exit before transitioning to next
                EventBus.Emit<BiomeData>(EVT_BIOME_EXITED, biomeSequence[i]);
            }
        }

        private IEnumerator ActivateBiome(BiomeData biome)
        {
            CurrentIndex = System.Array.IndexOf(biomeSequence, biome);
            Current = biome;

            Debug.Log($"[BiomeManager] Entering biome: {biome.displayName} ({biome.biomeType})");

            // Notify all listeners (SpawnManager, BaseEnemy, UI, Camera)
            EventBus.Emit<BiomeData>(EVT_BIOME_ENTERED, biome);

            // Spawn ambient VFX if configured
            if (!string.IsNullOrEmpty(biome.ambientVFXPoolKey))
            {
                ObjectPool.Instance?.Get(
                    biome.ambientVFXPoolKey, null,
                    Vector3.zero, Quaternion.identity);
            }

            // Transition crossfade — yield for the duration
            // (actual visual crossfade is handled by CameraController in Phase 10)
            yield return new WaitForSeconds(biome.transitionDurationSeconds);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Modifier Accessors

        /// <summary>
        /// Current biome spawn-interval multiplier.
        /// SpawnManager should call this to scale its wait time.
        /// </summary>
        public float GetSpawnIntervalMult()
            => Current != null ? Current.spawnIntervalMultiplier : 1f;

        /// <summary>
        /// Current biome enemy speed multiplier.
        /// BaseEnemy.ApplyBiomeModifiers() calls this each OnEnable.
        /// </summary>
        public float GetEnemySpeedMult()
            => Current != null ? Current.enemySpeedMultiplier : 1f;

        /// <summary>
        /// Current biome enemy damage multiplier.
        /// </summary>
        public float GetEnemyDamageMult()
            => Current != null ? Current.enemyDamageMultiplier : 1f;

        /// <summary>
        /// Current biome elite chance multiplier.
        /// SpawnManager.ShouldSpawnElite() should multiply its roll against this.
        /// </summary>
        public float GetEliteChanceMult()
            => Current != null ? Current.eliteChanceMultiplier : 1f;

        /// <summary>
        /// Current biome player speed multiplier — read by PlayerController.
        /// </summary>
        public float GetPlayerSpeedMult()
            => Current != null ? Current.playerSpeedMultiplier : 1f;

        /// <summary>
        /// Current biome Agni Kund vulnerability multiplier.
        /// AgniKund.TakeDamage should scale incoming damage by this.
        /// </summary>
        public float GetAgniKundVulnerabilityMult()
            => Current != null ? Current.agniKundVulnerabilityMult : 1f;

        #endregion
    }
}
