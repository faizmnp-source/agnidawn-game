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
            EventBus.Subscribe<AstraData>(GameManager.OnAstraFired, OnAstraFired);
            EventBus.Subscribe<float, Vector2>(GameManager.OnAoEDetonation, OnAoEDetonation);
            EventBus.Subscribe<Vector2, Vector2>(GameManager.OnLightningChain, OnLightningChain);
            EventBus.Subscribe<GameObject, float, float>(GameManager.OnSlowApplied, OnSlowApplied);
            EventBus.Subscribe<GameObject, Vector2, float>(GameManager.OnKnockbackApplied, OnKnockbackApplied);

            // Bosses
            EventBus.Subscribe<string>(GameManager.OnBossSpawned, OnBossSpawned);
            EventBus.Subscribe<string, int>(GameManager.OnBossPhaseChanged, OnBossPhaseChanged);
            EventBus.Subscribe<string>(GameManager.OnBossDied, OnBossDied);

            // Enemies
            EventBus.Subscribe<string>(GameManager.OnEnemyDied, OnEnemyDied);

            // Meta
            EventBus.Subscribe<int>(GameManager.OnShardCollected, OnShardCollected);
            EventBus.Subscribe<string>(GameManager.OnShrineUnlocked, OnShrineUnlocked);
            EventBus.Subscribe<string>(GameManager.OnLoreCollected, OnLoreCollected);

            // Agni Kund
            EventBus.Subscribe<float>(GameManager.OnAgniKundDamaged, OnAgniKundDamaged);
            EventBus.Subscribe<int, int>(GameManager.OnAgniTierChanged, OnAgniTierChanged);

            // Player
            EventBus.Subscribe(GameManager.EVT_LEVEL_UP, OnLevelUp);

            // Game state
            EventBus.Subscribe(GameManager.EVT_GAME_OVER, OnGameOver);
            EventBus.Subscribe(GameManager.EVT_VICTORY,   OnVictory);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<AstraData>(GameManager.OnAstraFired, OnAstraFired);
            EventBus.Unsubscribe<float, Vector2>(GameManager.OnAoEDetonation, OnAoEDetonation);
            EventBus.Unsubscribe<Vector2, Vector2>(GameManager.OnLightningChain, OnLightningChain);
            EventBus.Unsubscribe<GameObject, float, float>(GameManager.OnSlowApplied, OnSlowApplied);
            EventBus.Unsubscribe<GameObject, Vector2, float>(GameManager.OnKnockbackApplied, OnKnockbackApplied);
            EventBus.Unsubscribe<string>(GameManager.OnBossSpawned, OnBossSpawned);
            EventBus.Unsubscribe<string, int>(GameManager.OnBossPhaseChanged, OnBossPhaseChanged);
            EventBus.Unsubscribe<string>(GameManager.OnBossDied, OnBossDied);
            EventBus.Unsubscribe<string>(GameManager.OnEnemyDied, OnEnemyDied);
            EventBus.Unsubscribe<int>(GameManager.OnShardCollected, OnShardCollected);
            EventBus.Unsubscribe<string>(GameManager.OnShrineUnlocked, OnShrineUnlocked);
            EventBus.Unsubscribe<string>(GameManager.OnLoreCollected, OnLoreCollected);
            EventBus.Unsubscribe<float>(GameManager.OnAgniKundDamaged, OnAgniKundDamaged);
            EventBus.Unsubscribe<int, int>(GameManager.OnAgniTierChanged, OnAgniTierChanged);
            EventBus.Unsubscribe(GameManager.EVT_LEVEL_UP, OnLevelUp);
            EventBus.Unsubscribe(GameManager.EVT_GAME_OVER, OnGameOver);
            EventBus.Unsubscribe(GameManager.EVT_VICTORY,   OnVictory);
        }

        // ── Weapon Handlers ───────────────────────────────────────────────────────
        private void OnAstraFired(AstraData astra)
        {
            if (astra == null) return;

            // Per-astra SFX lookup — fall back to generic if no specific clip
            var data = astra.astraName switch
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
