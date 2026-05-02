using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.Gameplay.MetaProgression
{
    /// <summary>
    /// Manages lore fragment collection across runs.
    ///
    /// Responsibilities:
    ///   - Listen for OnBossDied (string bossId) → award matching LoreFragment
    ///   - Persist collected fragments via SaveSystem
    ///   - Emit OnLoreCollected (string fragmentId) when a new fragment is found
    ///   - Provide the Purana book UI with ordered, collected fragments
    ///
    /// Singleton — attach to a persistent GameObject in the main scene.
    /// Linear: FAI-13
    /// </summary>
    public class LoreManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────
        public static LoreManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────
        [Header("Lore Catalogue")]
        [Tooltip("All LoreFragment assets. Assign in Inspector or load from Resources.")]
        public List<LoreFragment> allFragments = new List<LoreFragment>();

        // ── EventBus constants ────────────────────────────────────────────
        public const string EVT_LORE_COLLECTED = "OnLoreCollected"; // string fragmentId

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

            if (allFragments == null || allFragments.Count == 0)
                LoadFromResources();
        }

        private void OnEnable()
        {
            EventBus.On<string>("OnBossDied", OnBossDied);
        }

        private void OnDisable()
        {
            EventBus.Off<string>("OnBossDied", OnBossDied);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────
        #region Public API

        /// <summary>Returns true if the fragment has been collected in any past run.</summary>
        public bool IsCollected(string fragmentId)
            => SaveSystem.IsLoreCollected(fragmentId);

        /// <summary>Returns all fragments collected so far, ordered by bossId.</summary>
        public List<LoreFragment> GetCollectedFragments()
            => allFragments
                .Where(f => IsCollected(f.fragmentId))
                .OrderBy(f => f.bossId)
                .ToList();

        /// <summary>Returns total fragment count (for progress tracking).</summary>
        public int TotalFragmentCount  => allFragments.Count;
        public int CollectedCount      => GetCollectedFragments().Count;

        /// <summary>
        /// Manually award a lore fragment (e.g. from a shrine unlock or debug menu).
        /// Returns true if it was a new discovery.
        /// </summary>
        public bool AwardFragment(string fragmentId)
        {
            if (IsCollected(fragmentId)) return false;

            SaveSystem.CollectLore(fragmentId);
            EventBus.Emit(EVT_LORE_COLLECTED, fragmentId);
            Debug.Log($"[LoreManager] Fragment collected: {fragmentId}");
            return true;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────
        #region Event Handlers

        private void OnBossDied(string bossId)
        {
            // Find all fragments keyed to this boss and award uncollected ones
            var toAward = allFragments.Where(f => f.bossId == bossId).ToList();
            foreach (var frag in toAward)
                AwardFragment(frag.fragmentId);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────
        #region Private

        private void LoadFromResources()
        {
            var loaded = Resources.LoadAll<LoreFragment>("MetaProgression/Lore");
            allFragments = new List<LoreFragment>(loaded);
            Debug.Log($"[LoreManager] Loaded {allFragments.Count} LoreFragment assets from Resources.");
        }

        #endregion
    }
}
