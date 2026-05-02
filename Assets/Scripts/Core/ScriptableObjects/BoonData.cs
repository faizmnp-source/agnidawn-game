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

        public void Apply(GameObject player)
        {
            if (player == null) return;

            if (player.TryGetComponent<Player.PlayerController>(out var pc))
            {
                pc.MoveSpeedMult        += movespeedBonus;
                pc.DashCooldownMult     = Mathf.Max(0.1f, pc.DashCooldownMult - dashCooldownBonus);
            }

            if (player.TryGetComponent<HealthSystem>(out var hp))
            {
                hp.DamageReductionMult  = Mathf.Max(0f, hp.DamageReductionMult - damageMultBonus);
                hp.HealingMult          += healingMultBonus;
                if (maxHealthBonus > 0f) hp.AddMaxHealth(maxHealthBonus);
            }

            if (player.TryGetComponent<Player.AstraController>(out var ac))
            {
                ac.GlobalDamageMult     += damageMultBonus;
                ac.GlobalCooldownMult   = Mathf.Max(0.1f, ac.GlobalCooldownMult - cooldownMultBonus);
            }

            // Special effects handled by dedicated systems via EventBus
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
