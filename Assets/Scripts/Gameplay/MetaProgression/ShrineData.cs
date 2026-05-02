using UnityEngine;

namespace AGNIDAWN.Gameplay.MetaProgression
{
    /// <summary>
    /// ScriptableObject defining a single shrine upgrade node.
    ///
    /// Each shrine upgrade:
    ///   - belongs to one of the five shrines (ShrineType)
    ///   - costs a fixed number of Divine Shards
    ///   - may require a prerequisite upgrade to be unlocked first
    ///   - carries a payload string interpreted by ShrineManager
    ///     (e.g. weapon ID, passive ID, character ID, lore page ID, shard multiplier value)
    ///
    /// Create via: Assets → AGNIDAWN → MetaProgression → ShrineData
    /// Linear: FAI-13
    /// </summary>
    [CreateAssetMenu(menuName = "AGNIDAWN/MetaProgression/ShrineData", fileName = "NewShrineData")]
    public class ShrineData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique ID used in SaveSystem. Once set, do NOT change.")]
        public string   shrineId          = "";

        [Tooltip("Display name shown in UI, e.g. 'Trishul (Shiva's Trident)'")]
        public string   displayName       = "";

        [Tooltip("Short description shown on the shrine upgrade card")]
        [TextArea(2, 4)]
        public string   description       = "";

        [Header("Shrine")]
        public ShrineType shrineType      = ShrineType.Brahma;

        [Tooltip("Tier within this shrine (1 = cheapest, higher = more powerful)")]
        [Range(1, 5)]
        public int      tier              = 1;

        [Header("Cost & Prerequisites")]
        [Tooltip("Divine Shards required to purchase this upgrade")]
        [Min(0)]
        public int      shardCost         = 50;

        [Tooltip("shrineId of the upgrade that must be purchased first. Leave empty if none.")]
        public string   prerequisiteShrineId = "";

        [Header("Unlock Payload")]
        [Tooltip(
            "Interpreted by ShrineManager based on shrineType:\n" +
            "Brahma    → weapon ID (AstraData name)\n" +
            "Vishnu    → passive ID (BoonData name)\n" +
            "Shiva     → character ID (string)\n" +
            "Saraswati → lore page / art gallery ID\n" +
            "Lakshmi   → float multiplier value as string, e.g. '1.25'"
        )]
        public string   unlockPayload     = "";

        [Header("Visuals")]
        public Sprite   icon              = null;
        public Color    accentColour      = Color.white;
    }
}
