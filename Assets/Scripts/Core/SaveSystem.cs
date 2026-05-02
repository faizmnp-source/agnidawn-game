using System;
using System.IO;
using UnityEngine;

namespace AGNIDAWN.Core
{
    /// <summary>
    /// JSON-based persistence for meta-progression (Shrine points, unlocks,
    /// run history, settings). In-run state is NOT saved here — only cross-run data.
    ///
    /// Data file: Application.persistentDataPath/agnidawn_save.json
    /// Linear: FAI-6
    /// </summary>
    public static class SaveSystem
    {
        // ── File paths ────────────────────────────────────────────────────
        private static string SavePath
            => Path.Combine(Application.persistentDataPath, "agnidawn_save.json");

        private static string BackupPath
            => Path.Combine(Application.persistentDataPath, "agnidawn_save.bak");

        // ── In-memory cache ───────────────────────────────────────────────
        private static SaveData _cache;

        // ──────────────────────────────────────────────────────────────────
        #region Save Data Model

        [Serializable]
        public class SaveData
        {
            // Meta-progression
            public int    totalShrinePoints     = 0;
            public int    totalRunsCompleted     = 0;
            public int    totalKills             = 0;
            public float  bestRunTimeSeconds     = 0f;
            public int    highestWaveReached     = 0;

            // Shrine unlocks — bitmask per category (expandable)
            public int    characterUnlocks       = 1;  // Arjuna unlocked by default (bit 0)
            public int    weaponUnlocks          = 1;  // Trishul unlocked by default (bit 0)

            // Settings
            public float  masterVolume           = 1f;
            public float  musicVolume            = 0.8f;
            public float  sfxVolume              = 1f;
            public bool   screenShake            = true;
            public bool   reducedVFX             = false;

            // Analytics / telemetry flags
            public string lastPlayedVersion      = "";
            public long   lastSaveTimestamp      = 0;
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Public API

        /// Load data from disk (or return defaults on first run).
        public static SaveData Load()
        {
            if (_cache != null) return _cache;

            if (File.Exists(SavePath))
            {
                try
                {
                    string json = File.ReadAllText(SavePath);
                    _cache = JsonUtility.FromJson<SaveData>(json);
                    Debug.Log("[SaveSystem] Save loaded.");
                    return _cache;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SaveSystem] Corrupt save, trying backup: {e.Message}");
                    return TryLoadBackup();
                }
            }

            Debug.Log("[SaveSystem] No save found — using defaults.");
            _cache = new SaveData();
            return _cache;
        }

        /// Persist current data to disk with backup.
        public static void Save(SaveData data = null)
        {
            if (data != null) _cache = data;
            if (_cache == null) _cache = new SaveData();

            _cache.lastSaveTimestamp  = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _cache.lastPlayedVersion  = Application.version;

            try
            {
                // Rotate backup
                if (File.Exists(SavePath))
                    File.Copy(SavePath, BackupPath, overwrite: true);

                string json = JsonUtility.ToJson(_cache, prettyPrint: true);
                File.WriteAllText(SavePath, json);
                EventBus.Emit("OnGameSaved");
                Debug.Log("[SaveSystem] Save written.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Save failed: {e}");
            }
        }

        /// Delete all save data (used by "Reset Progress" in settings).
        public static void DeleteAll()
        {
            _cache = new SaveData();
            if (File.Exists(SavePath))    File.Delete(SavePath);
            if (File.Exists(BackupPath))  File.Delete(BackupPath);
            EventBus.Emit("OnSaveDeleted");
            Debug.Log("[SaveSystem] Save deleted.");
        }

        // ── Convenience helpers ──────────────────────────────────────────

        public static bool IsCharacterUnlocked(int index)
            => (Load().characterUnlocks & (1 << index)) != 0;

        public static bool IsWeaponUnlocked(int index)
            => (Load().weaponUnlocks & (1 << index)) != 0;

        public static void UnlockCharacter(int index)
        {
            Load().characterUnlocks |= (1 << index);
            Save();
        }

        public static void UnlockWeapon(int index)
        {
            Load().weaponUnlocks |= (1 << index);
            Save();
        }

        public static void AddShrinePoints(int points)
        {
            Load().totalShrinePoints += points;
            Save();
        }

        public static void RecordRunCompletion(float time, int wave, int kills)
        {
            var d = Load();
            d.totalRunsCompleted++;
            d.totalKills += kills;
            if (time > d.bestRunTimeSeconds)  d.bestRunTimeSeconds  = time;
            if (wave > d.highestWaveReached)  d.highestWaveReached  = wave;
            Save(d);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Private

        private static SaveData TryLoadBackup()
        {
            if (!File.Exists(BackupPath))
            {
                _cache = new SaveData();
                return _cache;
            }

            try
            {
                string json = File.ReadAllText(BackupPath);
                _cache = JsonUtility.FromJson<SaveData>(json);
                Debug.Log("[SaveSystem] Backup loaded.");
                return _cache;
            }
            catch
            {
                _cache = new SaveData();
                return _cache;
            }
        }

        #endregion
    }
}
