using UnityEngine;

namespace AGNIDAWN.Gameplay.MetaProgression
{
    /// <summary>
    /// ScriptableObject defining a single difficulty preset.
    ///
    /// Multipliers are applied by DifficultyManager to enemies and shard rewards
    /// at the start of each run.
    ///
    /// Create via: Assets → AGNIDAWN → MetaProgression → DifficultyData
    /// Linear: FAI-13
    /// </summary>
    [CreateAssetMenu(menuName = "AGNIDAWN/MetaProgression/DifficultyData", fileName = "NewDifficultyData")]
    public class DifficultyData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique ID, e.g. 'Normal', 'Tandav', 'Pralaya'. Stored in SaveSystem.")]
        public string         difficultyId          = "Normal";

        public string         displayName           = "Normal";

        [Tooltip("Subtitle shown on the difficulty select screen")]
        public string         subtitle              = "";

        [TextArea(2, 3)]
        public string         description           = "";

        public DifficultyLevel level                = DifficultyLevel.Normal;

        [Header("Unlock")]
        [Tooltip("Always unlocked if empty. Otherwise requires this shrineId to be purchased.")]
        public string         prerequisiteShrineId  = "";

        [Header("Run Modifiers")]
        [Tooltip("Enemy max-HP multiplier. 1.0 = base, 1.5 = 50% tougher")]
        [Min(0.1f)]
        public float          enemyHpMultiplier     = 1f;

        [Tooltip("Enemy damage output multiplier")]
        [Min(0.1f)]
        public float          enemyDamageMultiplier = 1f;

        [Tooltip("Enemy movement-speed multiplier")]
        [Min(0.1f)]
        public float          enemySpeedMultiplier  = 1f;

        [Tooltip("Divine Shard reward multiplier for this run")]
        [Min(0.1f)]
        public float          shardRewardMultiplier = 1f;

        [Header("Visuals")]
        public Color          accentColour          = Color.white;
        public Sprite         icon                  = null;
    }
}
