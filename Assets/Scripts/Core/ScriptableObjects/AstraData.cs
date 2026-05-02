using UnityEngine;

namespace AGNIDAWN.Core
{
    /// <summary>
    /// ScriptableObject defining a divine Astra weapon.
    /// Create via: Assets > Create > AGNIDAWN > Astra Data
    /// Linear: FAI-7
    /// </summary>
    [CreateAssetMenu(fileName = "Astra_New", menuName = "AGNIDAWN/Astra Data")]
    public class AstraData : ScriptableObject
    {
        [Header("Identity")]
        public string astraId;          // e.g. "Trishul", "Gandiv", "Chakra"
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;

        [Header("Projectile")]
        public GameObject projectilePrefab;
        public float      projectileSpeed   = 10f;
        public int        piercing          = 0;       // 0 = no pierce, 1+ = pierce count
        public bool       requiresTarget    = true;

        [Header("Damage per level (1–5)")]
        public float[] damagePerLevel       = { 10f, 14f, 19f, 25f, 32f };

        [Header("Cooldown per level (seconds, 1–5)")]
        public float[] cooldownPerLevel     = { 1.5f, 1.3f, 1.1f, 0.9f, 0.7f };

        [Header("Upgrade")]
        public int   maxLevel              = 5;

        [Header("Lore")]
        [TextArea(3, 6)]
        public string loreText;
        public string deity;            // e.g. "Shiva", "Vishnu", "Brahma"

        // ── Helpers ───────────────────────────────────────────────────────

        public float GetDamageAtLevel(int level)
        {
            int idx = Mathf.Clamp(level - 1, 0, damagePerLevel.Length - 1);
            return damagePerLevel[idx];
        }

        public float GetCooldownAtLevel(int level)
        {
            int idx = Mathf.Clamp(level - 1, 0, cooldownPerLevel.Length - 1);
            return cooldownPerLevel[idx];
        }
    }
}
