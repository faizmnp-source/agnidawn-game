using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.VFX
{
    /// <summary>
    /// Central VFX manager for AGNIDAWN.
    ///
    /// Listens to EventBus events and spawns pooled particle effects via
    /// ObjectPool.  All VFX prefabs live in the pool under "VFX_*" keys.
    ///
    /// SCENE SETUP:
    ///   1. Create a persistent GameObject "VFXManager" in your main scene.
    ///   2. Attach this script.
    ///   3. Assign a VFXEventData SO in the Inspector.
    ///   4. Ensure ObjectPool has all "VFX_*" keys pre-warmed in the scene.
    ///      (Add an entry per VFXEventData pool key with a matching prefab.)
    ///
    /// Returned-to-pool timing: VFX prefabs should auto-return using the
    /// AutoReturnToPool helper component (disable after ParticleSystem stops).
    ///
    /// Linear: FAI-16 (Phase 11)
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        public static VFXManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [SerializeField] private VFXEventData vfxData;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (vfxData != null) vfxData.BuildLookups();
        }

        private void OnEnable()
        {
            // Astra events
            EventBus.On<string>         ("OnAstraSpecial",     OnAstraSpecial);
            EventBus.On<float, Vector2> ("OnAoEDetonation",    OnAoEDetonation);
            EventBus.On<Vector2, Vector2>("OnLightningChain",   OnLightningChain);

            // Enemy events
            EventBus.On<string>         ("OnEnemyDied",        OnEnemyDied);
            EventBus.On<float, Vector2> ("OnXPDropped",        OnXPDropped);

            // Agni events
            EventBus.On<int, int>       ("OnAgniTierChanged",  OnAgniTierChanged);

            // Boss events
            EventBus.On<string>         ("OnBossSpawned",      OnBossSpawned);
            EventBus.On<string, int>    ("OnBossPhaseChanged", OnBossPhaseChanged);
        }

        private void OnDisable()
        {
            EventBus.Off<string>         ("OnAstraSpecial",    OnAstraSpecial);
            EventBus.Off<float, Vector2> ("OnAoEDetonation",   OnAoEDetonation);
            EventBus.Off<Vector2, Vector2>("OnLightningChain", OnLightningChain);
            EventBus.Off<string>         ("OnEnemyDied",       OnEnemyDied);
            EventBus.Off<float, Vector2> ("OnXPDropped",       OnXPDropped);
            EventBus.Off<int, int>       ("OnAgniTierChanged", OnAgniTierChanged);
            EventBus.Off<string>         ("OnBossSpawned",     OnBossSpawned);
            EventBus.Off<string, int>    ("OnBossPhaseChanged",OnBossPhaseChanged);
        }

        // ── Event Handlers ────────────────────────────────────────────────────────

        private void OnAstraSpecial(string astraId)
        {
            if (vfxData == null) return;
            if (vfxData.TryGetAstraMapping(astraId, out var mapping))
            {
                // Cast VFX at player position — projectile itself handles impact VFX
                if (!string.IsNullOrEmpty(mapping.castPoolKey))
                    SpawnAtPlayer(mapping.castPoolKey);
            }
        }

        private void OnAoEDetonation(float radius, Vector2 origin)
        {
            if (vfxData == null) return;
            SpawnAt(vfxData.defaultShockwaveKey, origin, Vector3.one * (radius * 0.5f));
        }

        private void OnLightningChain(Vector2 from, Vector2 to)
        {
            // Chain VFX is handled by a dedicated LightningChainVFX component
            // on the Vajra prefab — nothing to do here centrally
        }

        private void OnEnemyDied(string enemyId)
        {
            if (vfxData == null) return;
            // Position is unknown from this event alone — individual BaseEnemy
            // components call PlayDeathVFXAt() directly for accurate positioning
        }

        private void OnXPDropped(float amount, Vector2 position)
        {
            if (vfxData == null || string.IsNullOrEmpty(vfxData.xpPickupKey)) return;
            SpawnAt(vfxData.xpPickupKey, position);
        }

        private void OnAgniTierChanged(int newTier, int prevTier)
        {
            if (vfxData == null) return;
            if (newTier > prevTier && !string.IsNullOrEmpty(vfxData.agniTierUpBurstKey))
                SpawnAtAgniKund(vfxData.agniTierUpBurstKey);
        }

        private void OnBossSpawned(string bossId)
        {
            if (vfxData == null) return;
            if (vfxData.TryGetBossPhaseMapping(bossId, out var mapping)
                && !string.IsNullOrEmpty(mapping.spawnBurstKey))
                SpawnAtCenter(mapping.spawnBurstKey);
        }

        private void OnBossPhaseChanged(string bossId, int phase)
        {
            if (vfxData == null) return;
            string key = vfxData.TryGetBossPhaseMapping(bossId, out var mapping)
                ? mapping.phaseShockwaveKey
                : vfxData.defaultShockwaveKey;
            if (!string.IsNullOrEmpty(key)) SpawnAtCenter(key);
        }

        // ── Public API (called by individual systems) ─────────────────────────────

        /// <summary>Spawns a VFX at <paramref name="worldPos"/>.</summary>
        public void PlayImpactVFX(string astraId, Vector2 worldPos)
        {
            if (vfxData == null) return;
            string key = vfxData.TryGetAstraMapping(astraId, out var m)
                ? m.impactPoolKey : vfxData.defaultImpactPoolKey;
            SpawnAt(key, worldPos);
        }

        /// <summary>Spawns enemy death VFX at <paramref name="worldPos"/>.</summary>
        public void PlayEnemyDeathVFX(string enemyId, Vector2 worldPos)
        {
            if (vfxData == null) return;
            string key = vfxData.TryGetEnemyDeathMapping(enemyId, out var m)
                ? m.deathPoolKey : vfxData.defaultDeathPoolKey;
            SpawnAt(key, worldPos);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void SpawnAt(string poolKey, Vector2 worldPos, Vector3? scale = null)
        {
            if (string.IsNullOrEmpty(poolKey)) return;
            var go = ObjectPool.Instance?.Get(poolKey);
            if (go == null) return;
            go.transform.position   = worldPos;
            go.transform.localScale = scale ?? Vector3.one;
            go.SetActive(true);
        }

        private void SpawnAtPlayer(string poolKey)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            SpawnAt(poolKey, player.transform.position);
        }

        private void SpawnAtAgniKund(string poolKey)
        {
            var agni = GameObject.FindGameObjectWithTag("AgniKund");
            Vector2 pos = agni != null ? (Vector2)agni.transform.position : Vector2.zero;
            SpawnAt(poolKey, pos);
        }

        private void SpawnAtCenter(string poolKey) => SpawnAt(poolKey, Vector2.zero);
    }
}
