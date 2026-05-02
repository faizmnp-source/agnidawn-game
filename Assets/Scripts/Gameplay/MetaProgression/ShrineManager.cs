using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.Gameplay.MetaProgression
{
    /// <summary>
    /// Runtime manager for the five divine shrines.
    ///
    /// Responsibilities:
    ///   - Load all ShrineData ScriptableObjects from Resources
    ///   - Expose purchase / unlock queries
    ///   - Delegate persistence to SaveSystem (Phase 8 helpers)
    ///   - Apply shard multiplier from Lakshmi upgrades
    ///   - Emit EventBus events: OnShrineUnlocked, OnShardSpent
    ///
    /// Singleton — attach to a persistent GameObject in the main scene.
    /// Linear: FAI-13
    /// </summary>
    public class ShrineManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────
        public static ShrineManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────
        [Header("Shrine Catalogue")]
        [Tooltip("All ShrineData assets. Assign in Inspector or load from Resources.")]
        public List<ShrineData> allShrines = new List<ShrineData>();

        // ── Cached multiplier ─────────────────────────────────────────────
        private float _shardMultiplier = 1f;

        // ── EventBus constants ────────────────────────────────────────────
        public const string EVT_SHRINE_UNLOCKED  = "OnShrineUnlocked";  // string shrineId
        public const string EVT_SHARD_SPENT      = "OnShardSpent";      // int amount

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

            if (allShrines == null || allShrines.Count == 0)
                LoadFromResources();

            RefreshShardMultiplier();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────
        #region Public API

        /// <summary>Returns true if the upgrade has already been purchased.</summary>
        public bool IsUnlocked(string shrineId)
            => SaveSystem.IsShrineUnlocked(shrineId);

        /// <summary>Returns true if all prerequisites are met and the player can afford the upgrade.</summary>
        public bool CanPurchase(ShrineData shrine)
        {
            if (shrine == null)                               return false;
            if (IsUnlocked(shrine.shrineId))                  return false;
            if (!SaveSystem.CanAffordShards(shrine.shardCost)) return false;
            if (!string.IsNullOrEmpty(shrine.prerequisiteShrineId) &&
                !IsUnlocked(shrine.prerequisiteShrineId))     return false;
            return true;
        }

        /// <summary>
        /// Attempts to purchase a shrine upgrade. Returns true on success.
        /// Deducts shards, persists the unlock, and fires EventBus events.
        /// </summary>
        public bool Purchase(ShrineData shrine)
        {
            if (!CanPurchase(shrine))
            {
                Debug.LogWarning($"[ShrineManager] Cannot purchase '{shrine?.shrineId}' — prereq or funds missing.");
                return false;
            }

            bool spent = SaveSystem.SpendDivineShards(shrine.shardCost);
            if (!spent)
            {
                Debug.LogWarning($"[ShrineManager] SpendDivineShards failed for '{shrine.shrineId}'.");
                return false;
            }

            SaveSystem.UnlockShrine(shrine.shrineId);
            EventBus.Emit(EVT_SHARD_SPENT, shrine.shardCost);
            EventBus.Emit(EVT_SHRINE_UNLOCKED, shrine.shrineId);

            Debug.Log($"[ShrineManager] Purchased shrine upgrade: {shrine.shrineId} ({shrine.shrineType})");

            if (shrine.shrineType == ShrineType.Lakshmi)
                RefreshShardMultiplier();

            return true;
        }

        /// <summary>Returns all ShrineData for a given shrine type.</summary>
        public List<ShrineData> GetShrinesByType(ShrineType type)
            => allShrines.Where(s => s.shrineType == type).OrderBy(s => s.tier).ToList();

        /// <summary>Returns all purchased upgrades.</summary>
        public List<ShrineData> GetUnlockedUpgrades()
            => allShrines.Where(s => IsUnlocked(s.shrineId)).ToList();

        /// <summary>Current Divine Shard drop multiplier (product of all Lakshmi upgrades).</summary>
        public float ShardMultiplier => _shardMultiplier;

        /// <summary>Returns true if an upgrade of the given type is available to purchase.</summary>
        public bool HasAffordableUpgrade(ShrineType type)
            => GetShrinesByType(type).Any(s => CanPurchase(s));

        #endregion

        // ─────────────────────────────────────────────────────────────────
        #region Private

        private void LoadFromResources()
        {
            var loaded = Resources.LoadAll<ShrineData>("MetaProgression/Shrines");
            allShrines = new List<ShrineData>(loaded);
            Debug.Log($"[ShrineManager] Loaded {allShrines.Count} ShrineData assets from Resources.");
        }

        private void RefreshShardMultiplier()
        {
            _shardMultiplier = 1f;
            foreach (var shrine in allShrines)
            {
                if (shrine.shrineType == ShrineType.Lakshmi && IsUnlocked(shrine.shrineId))
                {
                    if (float.TryParse(shrine.unlockPayload, out float mult))
                        _shardMultiplier *= mult;
                }
            }
            Debug.Log($"[ShrineManager] Shard multiplier = {_shardMultiplier:F2}");
        }

        #endregion
    }
}
