using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.Gameplay.MetaProgression
{
    /// <summary>
    /// Manages the selected difficulty for each run.
    ///
    /// Responsibilities:
    ///   - Expose difficulty selection to the main-menu screen
    ///   - Validate that a difficulty is unlocked before allowing selection
    ///   - Persist the choice via SaveSystem
    ///   - Provide run-start multipliers to GameManager / SpawnManager
    ///
    /// Singleton — attach to a persistent GameObject in the main scene.
    /// Linear: FAI-13
    /// </summary>
    public class DifficultyManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────
        public static DifficultyManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────
        [Header("Difficulty Catalogue")]
        [Tooltip("All DifficultyData assets ordered Normal → Tandav → Pralaya")]
        public List<DifficultyData> allDifficulties = new List<DifficultyData>();

        // ── Runtime cache ─────────────────────────────────────────────────
        private DifficultyData _current;

        // ── EventBus constants ────────────────────────────────────────────
        public const string EVT_DIFFICULTY_CHANGED = "OnDifficultyChanged"; // string difficultyId

        // ─────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (allDifficulties == null || allDifficulties.Count == 0)
                LoadFromResources();

            // Restore persisted selection
            string savedId = SaveSystem.GetSelectedDifficultyId();
            _current = allDifficulties.FirstOrDefault(d => d.difficultyId == savedId)
                    ?? allDifficulties.FirstOrDefault(d => d.level == DifficultyLevel.Normal);

            Debug.Log($"[DifficultyManager] Active difficulty: {_current?.difficultyId ?? "none"}");
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────
        #region Public API

        /// <summary>The difficulty that will be used when the next run starts.</summary>
        public DifficultyData Current => _current;

        /// <summary>Returns true if the difficulty has been unlocked via shrine or is Normal.</summary>
        public bool IsUnlocked(DifficultyData diff)
        {
            if (diff == null) return false;
            if (string.IsNullOrEmpty(diff.prerequisiteShrineId)) return true;
            return SaveSystem.IsShrineUnlocked(diff.prerequisiteShrineId);
        }

        /// <summary>
        /// Selects a difficulty for the next run.
        /// Returns false if it is locked.
        /// </summary>
        public bool Select(DifficultyData diff)
        {
            if (!IsUnlocked(diff))
            {
                Debug.LogWarning($"[DifficultyManager] '{diff?.difficultyId}' is locked.");
                return false;
            }

            _current = diff;
            SaveSystem.SetSelectedDifficulty(diff.difficultyId);
            EventBus.Emit(EVT_DIFFICULTY_CHANGED, diff.difficultyId);
            Debug.Log($"[DifficultyManager] Selected: {diff.difficultyId}");
            return true;
        }

        /// <summary>Returns all available (unlocked) difficulties.</summary>
        public List<DifficultyData> GetUnlockedDifficulties()
            => allDifficulties.Where(IsUnlocked).OrderBy(d => d.level).ToList();

        /// <summary>Convenience — enemy HP multiplier for the current run.</summary>
        public float EnemyHpMultiplier     => _current?.enemyHpMultiplier     ?? 1f;
        public float EnemyDamageMultiplier => _current?.enemyDamageMultiplier ?? 1f;
        public float EnemySpeedMultiplier  => _current?.enemySpeedMultiplier  ?? 1f;
        public float ShardRewardMultiplier => _current?.shardRewardMultiplier ?? 1f;

        #endregion

        // ─────────────────────────────────────────────────────────────────
        #region Private

        private void LoadFromResources()
        {
            var loaded = Resources.LoadAll<DifficultyData>("MetaProgression/Difficulties");
            allDifficulties = new List<DifficultyData>(loaded);
            Debug.Log($"[DifficultyManager] Loaded {allDifficulties.Count} DifficultyData assets.");
        }

        #endregion
    }
}
