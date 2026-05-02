using UnityEngine;

namespace AGNIDAWN.Bosses
{
    /// <summary>
    /// ScriptableObject config for a single mythological boss in AGNIDAWN.
    /// One asset per boss — assign in BossManager's inspector list.
    /// Linear: FAI-10
    /// </summary>
    [CreateAssetMenu(fileName = "BossData_New", menuName = "AGNIDAWN/Boss Data")]
    public class BossData : ScriptableObject
    {
        [Header("Identity")]
        public string  bossId;          // "Ravana", "Mahishasura", "Kali", "Vritra"
        public string  displayName;
        [TextArea(2, 5)]
        public string  loreText;        // Shown on death lore-drop

        [Header("Spawn")]
        public int     spawnAtMinute;   // 5, 10, 15, 20
        public GameObject bossPrefab;

        [Header("Stats — Base")]
        public float   totalHealth      = 1000f;
        public float   contactDamage    = 20f;
        public float   moveSpeed        = 2.5f;

        [Header("Phase Thresholds (0–1 of total health)")]
        [Tooltip("Boss transitions to next phase when HP drops below this fraction. One entry per phase break.")]
        public float[] phaseThresholds  = { 0.66f, 0.33f };  // e.g. at 66% and 33%

        [Header("XP & Rewards")]
        public float   xpReward         = 500f;
        public int     divineShardsDrop = 50;

        [Header("VFX Prefabs")]
        public GameObject introVFXPrefab;
        public GameObject deathVFXPrefab;
        public GameObject phaseTransitionVFXPrefab;
        public GameObject arenaModVFXPrefab;    // e.g. dark energy cloud for Kali

        [Header("Audio — FMOD Events")]
        public string  bossThemeFMODPath;       // e.g. "event:/Music/Boss/Ravana_Theme"
        public string  introSFXFMODPath;
        public string  deathSFXFMODPath;
        public string  phaseTransitionSFXPath;

        [Header("UI")]
        public Sprite  bossPortrait;
    }
}
