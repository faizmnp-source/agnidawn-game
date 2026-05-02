using UnityEngine;

namespace AGNIDAWN.Core
{
    /// <summary>
    /// ScriptableObject defining a single Divine Boon offered on level-up.
    /// Each boon applies stat modifiers or special effects via Apply().
    /// Create via: Assets > Create > AGNIDAWN > Boon Data
    /// Linear: FAI-7
    /// </summary>
    [CreateAssetMenu(fileName = "Boon_New", menuName = "AGNIDAWN/Boon Data")]
    public class BoonData : ScriptableObject
    {
        [Header("Identity")]
        public string boonId;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        public BoonRarity rarity = BoonRarity.Common;

        [Header("Unlock")]
        public int   minPlayerLevel     = 1;
        public bool  isUnique           = false;    // can only appear once per run

        [Header("Deity Theme")]
        public string deity;
        public Color  deityColor        = Color.white;

        [Header("Modifiers — leave 0 if not used")]
        public float movespeedBonus     = 0f;   // +flat
        public float damageMultBonus    = 0f;   // +fractional (0.1 = +10%)
        public float cooldownMultBonus  = 0f;   // -fractional (0.1 = -10% CD)
        public float maxHealthBonus     = 0f;   // +flat HP
        public float healingMultBonus   = 0f;   // +fractional
        public float dashCooldownBonus  = 0f;   // -fractional

        [Header("Special")]
        public BoonSpecialEffect specialEffect = BoonSpecialEffect.None;
        public float             specialValue  = 0f;

        // ── Apply ─────────────────────────────────────────────────────────
        // BoonData lives in Core — it cannot reference Player types directly.
        // Apply() emits a single EventBus event; BoonApplier (Player assembly)
        // listens and applies the actual stat changes. Clean separation.

        public void Apply(GameObject player)
        {
            if (player == null) return;

            // BoonApplier on the player listens to this and applies all modifiers
            EventBus.Emit<BoonData, GameObject>("OnBoonApplyRequest", this, player);

            // Special effects handled by dedicated systems
            if (specialEffect != BoonSpecialEffect.None)
                EventBus.Emit<BoonData>("OnBoonSpecialEffect", this);
        }
    }

    public enum BoonRarity { Common, Rare, Epic, Divine }

    public enum BoonSpecialEffect
    {
        None,
        AgniKundRestore,    // Restore Agni Kund HP
        DashOnKill,         // Proc a dash on kill
        ExplosiveProjectile,// Bullets explode on hit
        ChainLightning,     // Hits chain to nearby enemies
        PhoenixRevive,      // One-time death prevention
        DivineFrenzy        // Temporary damage boost
    }
}
