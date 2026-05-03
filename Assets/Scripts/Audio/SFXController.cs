using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.Audio
{
    /// <summary>
    /// Maps AGNIDAWN EventBus events to AudioManager SFX calls.
    ///
    /// This is the single place that connects game events to audio.
    /// Every sound effect that occurs in response to gameplay is triggered here.
    ///
    /// Assign AudioEventData SOs in the Inspector (or via an AudioConfig SO).
    /// If an event data field is left null, the sound is silently skipped —
    /// no errors, easy to stub out during development.
    /// </summary>
    public class SFXController : MonoBehaviour
    {
        // ── Inspector — Weapon / Astra ────────────────────────────────────────────
        [Header("Weapons / Astras")]
        [SerializeField] private AudioEventData trishulFire;
        [SerializeField] private AudioEventData brahmastraCharge;
        [SerializeField] private AudioEventData brahmastraDetonate;
        [SerializeField] private AudioEventData sudarshanaSpinLoop;
        [SerializeField] private AudioEventData vajraLightningChain;
        [SerializeField] private AudioEventData varunastraSlowSplash;
        [SerializeField] private AudioEventData vayuastraKnockback;
        [SerializeField] private AudioEventData genericAstraFire;

        // ── Inspector — Boss ──────────────────────────────────────────────────────
        [Header("Boss")]
        [SerializeField] private AudioEventData bossSpawnSting;
        [SerializeField] private AudioEventData bossPhaseChange;
        [SerializeField] private AudioEventData bossDeathExplosion;

        // ── Inspector — Enemy ─────────────────────────────────────────────────────
        [Header("Enemies")]
        [SerializeField] private AudioEventData enemyDeath;
        [SerializeField] private AudioEventData enemyHit;

        // ── Inspector — Meta / UI ─────────────────────────────────────────────────
        [Header("Meta-Progression")]
        [SerializeField] private AudioEventData shardCollected;
        [SerializeField] private AudioEventData shrineUnlocked;
        [SerializeField] private AudioEventData loreCollected;

        [Header("Agni Kund")]
        [SerializeField] private AudioEventData agniKundHit;
        [SerializeField] private AudioEventData agniTierUp;

        [Header("Player")]
        [SerializeField] private AudioEventData levelUp;

        [Header("Game State")]
        [SerializeField] private AudioEventData gameOverSting;
        [SerializeField] private AudioEventData victorySting;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────
        private void OnEnable()
        {
            // Weapons
            EventBus.On<AstraData>("OnAstraFired", OnAstraFired);
            EventBus.On<float, Vector2>("OnAoEDetonation", OnAoEDetonation);
            EventBus.On<Vector2, Vector2>("OnLightningChain", OnLightningChain);
            EventBus.On<GameObject, float, float>("OnSlowApplied", OnSlowApplied);
            EventBus.On<GameObject, Vector2, float>("OnKnockbackApplied", OnKnockbackApplied);

            // Bosses
            EventBus.On<string>("OnBossSpawned", OnBossSpawned);
            EventBus.On<string, int>("OnBossPhaseChanged", OnBossPhaseChanged);
            EventBus.On<string>("OnBossDied", OnBossDied);

            // Enemies
            EventBus.On<string>("OnEnemyDied", OnEnemyDied);

            // Meta
            EventBus.On<int>("OnShardCollected", OnShardCollected);
            EventBus.On<string>("OnShrineUnlocked", OnShrineUnlocked);
            EventBus.On<string>("OnLoreCollected", OnLoreCollected);

            // Agni Kund
            EventBus.On<float>("OnAgniKundDamaged", OnAgniKundDamaged);
            EventBus.On<int, int>("OnAgniTierChanged", OnAgniTierChanged);

            // Player
            EventBus.On(GameManager.EVT_LEVEL_UP, OnLevelUp);

            // Game state
            EventBus.On(GameManager.EVT_GAME_OVER, OnGameOver);
            EventBus.On(GameManager.EVT_VICTORY,   OnVictory);
        }

        private void OnDisable()
        {
            EventBus.Off<AstraData>("OnAstraFired", OnAstraFired);
            EventBus.Off<float, Vector2>("OnAoEDetonation", OnAoEDetonation);
            EventBus.Off<Vector2, Vector2>("OnLightningChain", OnLightningChain);
            EventBus.Off<GameObject, float, float>("OnSlowApplied", OnSlowApplied);
            EventBus.Off<GameObject, Vector2, float>("OnKnockbackApplied", OnKnockbackApplied);
            EventBus.Off<string>("OnBossSpawned", OnBossSpawned);
            EventBus.Off<string, int>("OnBossPhaseChanged", OnBossPhaseChanged);
            EventBus.Off<string>("OnBossDied", OnBossDied);
            EventBus.Off<string>("OnEnemyDied", OnEnemyDied);
            EventBus.Off<int>("OnShardCollected", OnShardCollected);
            EventBus.Off<string>("OnShrineUnlocked", OnShrineUnlocked);
            EventBus.Off<string>("OnLoreCollected", OnLoreCollected);
            EventBus.Off<float>("OnAgniKundDamaged", OnAgniKundDamaged);
            EventBus.Off<int, int>("OnAgniTierChanged", OnAgniTierChanged);
            EventBus.Off(GameManager.EVT_LEVEL_UP, OnLevelUp);
            EventBus.Off(GameManager.EVT_GAME_OVER, OnGameOver);
            EventBus.Off(GameManager.EVT_VICTORY,   OnVictory);
        }

        // ── Weapon Handlers ───────────────────────────────────────────────────────
        private void OnAstraFired(AstraData astra)
        {
            if (astra == null) return;

            // Per-astra SFX lookup — fall back to generic if no specific clip
            var data = astra.astraId switch
            {
                "Trishul"      => trishulFire,
                "Brahmastra"   => brahmastraCharge,
                _              => genericAstraFire
            };
            PlaySFX(data, Vector3.zero);
        }

        private void OnAoEDetonation(float radius, Vector2 position)
        {
            PlaySFX(brahmastraDetonate, position);
        }

        private void OnLightningChain(Vector2 from, Vector2 to)
        {
            PlaySFX(vajraLightningChain, from);
        }

        private void OnSlowApplied(GameObject target, float speedMult, float duration)
        {
            if (target == null) return;
            PlaySFX(varunastraSlowSplash, target.transform.position);
        }

        private void OnKnockbackApplied(GameObject target, Vector2 force, float duration)
        {
            if (target == null) return;
            PlaySFX(vayuastraKnockback, target.transform.position);
        }

        // ── Boss Handlers ─────────────────────────────────────────────────────────
        private void OnBossSpawned(string bossId)   => PlaySFX(bossSpawnSting,    Vector3.zero);
        private void OnBossPhaseChanged(string bossId, int phase)
                                                    => PlaySFX(bossPhaseChange,   Vector3.zero);
        private void OnBossDied(string bossId)      => PlaySFX(bossDeathExplosion, Vector3.zero);

        // ── Enemy Handlers ────────────────────────────────────────────────────────
        private void OnEnemyDied(string enemyId)    => PlaySFX(enemyDeath, Vector3.zero);

        // ── Meta Handlers ─────────────────────────────────────────────────────────
        private void OnShardCollected(int count)    => PlaySFX(shardCollected, Vector3.zero);
        private void OnShrineUnlocked(string id)    => PlaySFX(shrineUnlocked, Vector3.zero);
        private void OnLoreCollected(string id)     => PlaySFX(loreCollected,  Vector3.zero);

        // ── Agni Kund ─────────────────────────────────────────────────────────────
        private void OnAgniKundDamaged(float dmg)          => PlaySFX(agniKundHit,  Vector3.zero);
        private void OnAgniTierChanged(int newTier, int old) => PlaySFX(agniTierUp, Vector3.zero);

        // ── Player / Game State ───────────────────────────────────────────────────
        private void OnLevelUp()  => PlaySFX(levelUp,      Vector3.zero);
        private void OnGameOver() => PlaySFX(gameOverSting, Vector3.zero);
        private void OnVictory()  => PlaySFX(victorySting,  Vector3.zero);

        // ── Utility ───────────────────────────────────────────────────────────────
        private static void PlaySFX(AudioEventData data, Vector3 pos)
        {
            if (data == null || !data.IsValid) return;
            AudioManager.Instance?.PlayOneShot(data, pos);
        }

        private static void PlaySFX(AudioEventData data, Vector2 pos)
            => PlaySFX(data, new Vector3(pos.x, pos.y, 0f));
    }
}
