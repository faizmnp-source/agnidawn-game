using UnityEngine;

namespace AGNIDAWN.Core
{
    public enum EnemyType
    {
        Asura,          // Melee charger — high aggression
        Rakshasa,       // Flanker — circles player, shape-shift burst
        Naga,           // Ranged — venom spit, keeps distance
        Pisacha,        // Swarm — tiny, fast, explodes on death
        Vetala,         // Teleporter — blinks behind player
        Yaksha,         // Tank — shield aura protects nearby enemies
        Brahmarakshasa, // Spellcaster — summons minions
        Elite           // Upgraded variant — double stats + special move
    }

    /// <summary>
    /// ScriptableObject defining a mythological enemy's stats and behaviour profile.
    /// Create via: Assets > Create > AGNIDAWN > Enemy Data
    /// Linear: FAI-8
    /// </summary>
    [CreateAssetMenu(fileName = "Enemy_New", menuName = "AGNIDAWN/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        public string    enemyId;
        public string    displayName;
        public EnemyType enemyType;
        public Sprite    sprite;
        public RuntimeAnimatorController animatorController;

        [Header("Base Stats")]
        public float maxHealth      = 30f;
        public float moveSpeed      = 3f;
        public float damage         = 10f;
        public float attackRange    = 0.8f;
        public float attackCooldown = 1.2f;
        public float xpOnDeath      = 5f;

        [Header("Aggro")]
        public float aggroRadius    = 8f;   // Distance player must be within to chase
        public float deAggroRadius  = 14f;  // If player gets this far, return to idle

        [Header("Special Behaviour")]
        public float specialCooldown    = 5f;
        public float specialRange       = 6f;
        public int   swarmGroupSize     = 1;    // Pisacha: spawn in groups
        public float teleportRange      = 3f;   // Vetala: blink distance
        public float shieldRadius       = 2.5f; // Yaksha: allies inside get damage reduction

        [Header("Elite Multipliers")]
        public float eliteHealthMult    = 2.2f;
        public float eliteDamageMult    = 1.6f;
        public float eliteSpeedMult     = 1.2f;
        public Color eliteTintColor     = new Color(1f, 0.3f, 0f);

        [Header("Visuals")]
        public GameObject deathVFXPrefab;
        public GameObject hitVFXPrefab;
        public Color      damageFlashColor = Color.red;

        [Header("Audio")]
        public string fmodDeathEvent  = "event:/Enemy/Death";
        public string fmodAttackEvent = "event:/Enemy/Attack";
        public string fmodAggroEvent  = "event:/Enemy/Aggro";

        [Header("Lore")]
        [TextArea(2, 5)]
        public string loreText;
    }
}
