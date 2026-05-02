using UnityEngine;

namespace AGNIDAWN.Core
{
    /// <summary>
    /// Biome type identifier — determines which mythological realm the current run takes place in.
    /// Biomes rotate every 4 minutes across the 20-minute session.
    /// </summary>
    public enum BiomeType
    {
        Forest,      // Dandaka Vana — dense canopy, slower enemies, denser spawns
        Desert,      // Thar Marustan — open sands, fast enemies, reduced cover
        Mountain,    // Himavat — rocky terrain, elite spawn chance boosted
        Ocean,       // Samudra — coastal, Naga-heavy waves, player slowed slightly
        Underworld   // Patala — maximum threat, all modifiers peak, darkness ambience
    }

    /// <summary>
    /// Per-biome configuration. Drives spawn modifiers, visual theming, and player/enemy stat tweaks.
    /// Assign one BiomeData asset per biome in BiomeManager.biomeSequence.
    ///
    /// Linear: FAI-12
    /// </summary>
    [CreateAssetMenu(menuName = "AGNIDAWN/Biome Data")]
    public class BiomeData : ScriptableObject
    {
        // ── Identity ───────────────────────────────────────────────────────
        [Header("Identity")]
        public string   biomeId;
        public BiomeType biomeType;
        public string   displayName;

        [TextArea(2, 4)]
        public string   description;

        // ── Spawn Modifiers ────────────────────────────────────────────────
        [Header("Spawn Modifiers")]
        [Tooltip("Multiplies SpawnManager.baseSpawnInterval — lower = more spawns")]
        [Range(0.5f, 2f)]  public float spawnIntervalMultiplier  = 1f;

        [Tooltip("Multiplies each enemy's base MoveSpeed")]
        [Range(0.5f, 2f)]  public float enemySpeedMultiplier     = 1f;

        [Tooltip("Multiplies each enemy's base Damage")]
        [Range(0.5f, 2f)]  public float enemyDamageMultiplier    = 1f;

        [Tooltip("Multiplies the elite chance roll")]
        [Range(0.5f, 3f)]  public float eliteChanceMultiplier    = 1f;

        // ── Player & Environment ───────────────────────────────────────────
        [Header("Player & Environment Modifiers")]
        [Tooltip("Multiplies PlayerController.moveSpeed during this biome")]
        [Range(0.5f, 1.5f)] public float playerSpeedMultiplier       = 1f;

        [Tooltip("Multiplies AgniKund incoming-damage (< 1 = biome protects it)")]
        [Range(0.5f, 2f)]   public float agniKundVulnerabilityMult   = 1f;

        // ── Visuals ────────────────────────────────────────────────────────
        [Header("Visuals")]
        public Color  ambientLightColor   = Color.white;
        public Color  backgroundColor     = new Color(0.05f, 0.05f, 0.05f, 1f);

        [Tooltip("ObjectPool key for ambient particle VFX spawned on transition (leave blank = none)")]
        public string ambientVFXPoolKey;

        // ── Timing ────────────────────────────────────────────────────────
        [Header("Timing")]
        [Tooltip("Crossfade duration between biomes (seconds)")]
        [Min(0.5f)] public float transitionDurationSeconds = 2f;

        [Tooltip("How long this biome stays active (minutes) — should sum to 20 across all biomes")]
        [Min(1f)]   public float activeDurationMinutes     = 4f;
    }
}
