using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.Bosses
{
    /// <summary>
    /// BossManager — Singleton that orchestrates all boss spawns during a run.
    ///
    /// Responsibilities:
    ///   - Listens to TimeManager's OnMinutePassed event.
    ///   - Spawns the correct boss prefab at the correct minute (5, 10, 15, 20).
    ///   - Pauses standard enemy spawning during boss intros (via EventBus).
    ///   - Tracks which bosses have been defeated this run.
    ///   - Triggers OnVictory when Vritra (final boss, minute 20) is defeated.
    ///   - Exposes public state so UI can show boss health bars.
    ///
    /// Boss Schedule (from BossData.spawnAtMinute):
    ///   Minute 5  → Ravana
    ///   Minute 10 → Mahishasura
    ///   Minute 15 → Kali
    ///   Minute 20 → Vritra (final)
    ///
    /// Linear: FAI-10
    /// </summary>
    public class BossManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────
        public static BossManager Instance { get; private set; }

        // ── Inspector ──────────────────────────────────────────────────────
        [Header("Boss Roster (assign in order: Ravana, Mahishasura, Kali, Vritra)")]
        [SerializeField] private List<BossData> _bossRoster = new();

        [Header("Spawn Config")]
        [SerializeField] private Transform _spawnPoint;
        [Tooltip("Offset from spawn point used for boss entrance (boss walks in from off-screen)")]
        [SerializeField] private Vector2   _entranceOffset = new Vector2(8f, 0f);

        // ── State ──────────────────────────────────────────────────────────
        private BaseBoss         _activeBoss;
        private HashSet<string>  _defeatedBossIds = new();
        private bool             _bossActive      = false;

        // ── Public Accessors ───────────────────────────────────────────────
        public BaseBoss  ActiveBoss   => _activeBoss;
        public bool      IsBossActive => _bossActive;

        // ──────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.On<int>("OnMinutePassed",  OnMinutePassed);
            EventBus.On<string>("OnBossDied",   OnBossDefeated);
        }

        private void OnDisable()
        {
            EventBus.Off<int>("OnMinutePassed",  OnMinutePassed);
            EventBus.Off<string>("OnBossDied",   OnBossDefeated);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Spawn Logic

        private void OnMinutePassed(int currentMinute)
        {
            if (_bossActive) return; // never double-spawn

            BossData toSpawn = _bossRoster.Find(b => b.spawnAtMinute == currentMinute);
            if (toSpawn == null) return;
            if (_defeatedBossIds.Contains(toSpawn.bossId)) return; // already killed (shouldn't happen, but safe)

            StartCoroutine(SpawnBoss(toSpawn));
        }

        private IEnumerator SpawnBoss(BossData bossData)
        {
            if (bossData.bossPrefab == null)
            {
                Debug.LogWarning($"[BossManager] No prefab assigned for boss: {bossData.bossId}");
                yield break;
            }

            // Pause enemy spawner
            EventBus.Emit("OnBossSpawnBegin");
            _bossActive = true;

            // Spawn from pool at entrance offset
            Vector3 spawnPos = _spawnPoint != null
                ? _spawnPoint.position + (Vector3)_entranceOffset
                : (Vector3)_entranceOffset;

            GameObject bossGO = ObjectPool.Instance != null
                ? ObjectPool.Instance.Get($"Boss_{bossData.bossId}", bossData.bossPrefab,
                    spawnPos, Quaternion.identity)
                : Instantiate(bossData.bossPrefab, spawnPos, Quaternion.identity);

            _activeBoss = bossGO.GetComponent<BaseBoss>();

            if (_activeBoss == null)
            {
                Debug.LogError($"[BossManager] Boss prefab {bossData.bossId} is missing a BaseBoss component!");
                _bossActive = false;
                yield break;
            }

            // Let the intro run — boss activates itself
            _activeBoss.Activate();

            // Notify UI to show boss health bar
            EventBus.Emit<BaseBoss>("OnBossHealthBarShow", _activeBoss);

            yield return null;
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Boss Defeated

        private void OnBossDefeated(string bossId)
        {
            _defeatedBossIds.Add(bossId);
            _bossActive  = false;
            _activeBoss  = null;

            // Hide boss health bar
            EventBus.Emit("OnBossHealthBarHide");

            // Resume enemy spawning
            EventBus.Emit("OnBossSpawnEnd");

            // If Vritra (final boss) is defeated → Victory!
            BossData finalBoss = _bossRoster.Count > 0 ? _bossRoster[^1] : null;
            if (finalBoss != null && bossId == finalBoss.bossId)
            {
                StartCoroutine(TriggerVictory());
            }
        }

        private IEnumerator TriggerVictory()
        {
            yield return new WaitForSeconds(3.5f); // let sunrise sequence play
            GameManager.Instance?.TriggerVictory();
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Public API

        /// Returns true if any boss has been defeated this run.
        public bool HasDefeated(string bossId) => _defeatedBossIds.Contains(bossId);

        /// Returns count of bosses defeated this run.
        public int DefeatedCount => _defeatedBossIds.Count;

        /// Reset all state (called by GameManager on new run)
        public void ResetForNewRun()
        {
            _defeatedBossIds.Clear();
            _bossActive = false;
            _activeBoss = null;
        }

        #endregion
    }
}
