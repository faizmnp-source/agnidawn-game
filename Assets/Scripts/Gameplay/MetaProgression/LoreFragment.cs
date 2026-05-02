using UnityEngine;

namespace AGNIDAWN.Gameplay.MetaProgression
{
    /// <summary>
    /// ScriptableObject representing a single Sanskrit lore fragment.
    ///
    /// Dropped by bosses on death (keyed to bossId).
    /// Collected fragments unlock entries in the Purana book UI.
    ///
    /// Create via: Assets → AGNIDAWN → MetaProgression → LoreFragment
    /// Linear: FAI-13
    /// </summary>
    [CreateAssetMenu(menuName = "AGNIDAWN/MetaProgression/LoreFragment", fileName = "NewLoreFragment")]
    public class LoreFragment : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique ID used in SaveSystem. Once set, do NOT change.")]
        public string   fragmentId        = "";

        [Tooltip("Which boss drops this fragment (matches BossData.bossId)")]
        public string   bossId            = "";

        [Header("Lore Content")]
        [Tooltip("Original Sanskrit shloka text")]
        [TextArea(3, 6)]
        public string   sanskritShloka    = "";

        [Tooltip("English translation of the shloka")]
        [TextArea(3, 6)]
        public string   translatedText    = "";

        [Tooltip("Source scripture, e.g. 'Rigveda 10.129', 'Mahabharata 5.49'")]
        public string   scriptureSource   = "";

        [Header("Character")]
        [Tooltip("Character or deity this shloka relates to")]
        public string   characterName     = "";

        [Tooltip("Brief backstory paragraph unlocked with this fragment")]
        [TextArea(4, 8)]
        public string   characterBio      = "";

        [Header("Visuals")]
        [Tooltip("Illustrated artwork shown in the Purana book")]
        public Sprite   illustration      = null;

        [Tooltip("Background parchment colour for this entry")]
        public Color    parchmentTint     = new Color(0.95f, 0.87f, 0.70f, 1f);
    }
}
