using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.Gameplay.MetaProgression
{
    /// <summary>
    /// Manages Divine Shard collection during a run.
    ///
    /// Divine Shards are the meta-progression currency that persists between runs.
    /// They are collected by:
    ///   - Killing enemies  (baseEnemyShardValue × DifficultyManager.ShardRewardMultiplier
    ///                                            × ShrineManager.ShardMultiplier)
    ///   - Completing a run (runCompletionBonus × multipliers)
    ///   - Defeating bosses (bossBonusShards × multipliers)
    ///
    /// This manager listens to EventBus events, accumulates shards for the current run,
    /// and persists them to SaveSystem when the run ends.
    ///
    /// Singleton — attach to a persistent GameObject in the main scene.
    /// Linear: FAI-13
    /// </summary>
    public class DivineShardManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────
        public static DivineShardManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────
        [Header("Drop Values")]
        [Tooltip("Shards awarded per enemy kill (before multipliers)")]
        [Min(0)]
        public int   baseEnemyShardValue  = 1;

        [Tooltip("Bonus shards awarded for defeating a boss")]
        [Min(0)]
        public int   bossBonusShards      = 25;

        [Tooltip("Flat bonus awarded when a run is completed (Vritra defeated)")]
        [Min(0)]
        public int   runCompletionBonus   = 50;

        // ── Runtime state (current run only) ──────────────────────────────
        private int  _runShards           = 0;

        // ── EventBus constants ────────────────────────────────────────────
        public const string EVT_SHARD_COLLECTED = "OnShardCollected"; // int amount

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
        }

        private void OnEnable()
        {
            EventBus.On<string>("OnEnemyDied",  OnEnemyDied);
            EventBus.On<string>("OnBossDied",   OnBossDied);
            EventBus.On("EVT_VICTORY",          OnRunCompleted);
            EventBus.On("EVT_GAME_OVER",        OnRunEnded);
            EventBus.On("EVT_GAME_START",       OnRunStarted);
        }

        private void OnDisable()
        {
            EventBus.Off<string>("OnEnemyDied", OnEnemyDied);
            EventBus.Off<string>("OnBossDied",  OnBossDied);
            EventBus.Off("EVT_VICTORY",         OnRunCompleted);
            EventBus.Off("EVT_GAME_OVER",       OnRunEnded);
            EventBus.Off("EVT_GAME_START",      OnRunStarted);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────
        #region Public API

        /// <summary>Shards earned in the current run (not yet persisted).</summary>
        public int CurrentRunShards => _runShards;

        /// <summary>Total shards persisted across all runs.</summary>
        public int TotalShards => SaveSystem.GetDivineShards();

        /// <summary>
        /// Award a custom shard amount (e.g. from a chest, shrine event, debug menu).
        /// Multipliers are NOT applied here — call with the final value.
        /// </summary>
        public void AwardShards(int amount)
        {
            if (amount <= 0) return;
            _runShards += amount;
            EventBus.Emit(EVT_SHARD_COLLECTED, amount);
            Debug.Log($"[DivineShardManager] +{amount} shards (run total: {_runShards})");
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────
        #region Event Handlers

        private void OnRunStarted()
        {
            _runShards = 0;
            Debug.Log("[DivineShardManager] Run started — shard counter reset.");
        }

        private void OnEnemyDied(string enemyId)
        {
            float mult = GetCombinedMultiplier();
            int award  = Mathf.RoundToInt(baseEnemyShardValue * mult);
            AwardShards(award);
        }

        private void OnBossDied(string bossId)
        {
            float mult = GetCombinedMultiplier();
            int award  = Mathf.RoundToInt(bossBonusShards * mult);
            AwardShards(award);
        }

        private void OnRunCompleted()
        {
            float mult = GetCombinedMultiplier();
            int bonus  = Mathf.RoundToInt(runCompletionBonus * mult);
            AwardShards(bonus);
            PersistRunShards();
        }

        private void OnRunEnded()
        {
            // Game Over — still keep shards earned during the run
            PersistRunShards();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────
        #region Private

        private float GetCombinedMultiplier()
        {
            float diff   = DifficultyManager.Instance != null
                           ? DifficultyManager.Instance.ShardRewardMultiplier : 1f;
            float shrine = ShrineManager.Instance != null
                           ? ShrineManager.Instance.ShardMultiplier           : 1f;
            return diff * shrine;
        }

        private void PersistRunShards()
        {
            if (_runShards <= 0) return;
            SaveSystem.AddDivineShards(_runShards);
            Debug.Log($"[DivineShardManager] Persisted {_runShards} shards. Total: {SaveSystem.GetDivineShards()}");
            _runShards = 0;
        }

        #endregion
    }
}
