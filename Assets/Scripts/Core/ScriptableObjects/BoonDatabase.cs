using System.Collections.Generic;
using UnityEngine;

namespace AGNIDAWN.Core
{
    /// <summary>
    /// ScriptableObject container for all BoonData assets.
    /// Provides filtered queries for the LevelUpManager.
    /// Create via: Assets > Create > AGNIDAWN > Boon Database
    /// Linear: FAI-7
    /// </summary>
    [CreateAssetMenu(fileName = "BoonDatabase", menuName = "AGNIDAWN/Boon Database")]
    public class BoonDatabase : ScriptableObject
    {
        [SerializeField] private List<BoonData> allBoons = new List<BoonData>();

        // ── Runtime cache ─────────────────────────────────────────────────
        private readonly HashSet<string> _appliedThisRun = new HashSet<string>();

        /// Get boons available at a given player level (excludes already-applied unique boons)
        public List<BoonData> GetAvailableBoons(int playerLevel)
        {
            var result = new List<BoonData>();
            foreach (var boon in allBoons)
            {
                if (boon == null) continue;
                if (boon.minPlayerLevel > playerLevel) continue;
                if (boon.isUnique && _appliedThisRun.Contains(boon.boonId)) continue;
                result.Add(boon);
            }
            return result;
        }

        /// Mark a boon as applied this run (prevents unique re-roll)
        public void MarkApplied(string boonId) => _appliedThisRun.Add(boonId);

        /// Reset run state (call at game start)
        public void ResetRun() => _appliedThisRun.Clear();

        /// Get all boons of a specific rarity
        public List<BoonData> GetByRarity(BoonRarity rarity)
        {
            var result = new List<BoonData>();
            foreach (var b in allBoons)
                if (b != null && b.rarity == rarity) result.Add(b);
            return result;
        }

        public int TotalCount => allBoons.Count;
    }
}
