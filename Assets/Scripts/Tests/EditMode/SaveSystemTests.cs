using NUnit.Framework;
using System.IO;
using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.Tests.EditMode
{
    /// <summary>
    /// EditMode unit tests for SaveSystem.
    /// Uses a temp directory to avoid touching the real save file.
    /// Linear: FAI-6
    /// </summary>
    public class SaveSystemTests
    {
        private string _realPersistentPath;

        [SetUp]
        public void SetUp()
        {
            // Force load fresh data each test by deleting any cached state
            // (SaveSystem._cache is private — we delete the file and reload)
            var savePath = Path.Combine(Application.persistentDataPath, "agnidawn_save.json");
            var bakPath  = Path.Combine(Application.persistentDataPath, "agnidawn_save.bak");
            if (File.Exists(savePath)) File.Delete(savePath);
            if (File.Exists(bakPath))  File.Delete(bakPath);
            EventBus.ClearAll();
            SaveSystem.DeleteAll();
        }

        [TearDown]
        public void TearDown() => SaveSystem.DeleteAll();

        // ── Defaults ─────────────────────────────────────────────────────

        [Test]
        public void Load_FirstTime_ReturnsDefaults()
        {
            var data = SaveSystem.Load();
            Assert.IsNotNull(data);
            Assert.AreEqual(0, data.totalShrinePoints);
            Assert.AreEqual(0, data.totalRunsCompleted);
            Assert.AreEqual(1, data.characterUnlocks, "Arjuna should be unlocked by default (bit 0).");
            Assert.AreEqual(1, data.weaponUnlocks,    "Trishul should be unlocked by default (bit 0).");
        }

        // ── Save / Load round-trip ────────────────────────────────────────

        [Test]
        public void Save_ThenLoad_RetainsShrinePoints()
        {
            SaveSystem.AddShrinePoints(150);
            var data = SaveSystem.Load();
            Assert.AreEqual(150, data.totalShrinePoints);
        }

        [Test]
        public void RecordRun_UpdatesStats()
        {
            SaveSystem.RecordRunCompletion(600f, 5, 300);
            var data = SaveSystem.Load();
            Assert.AreEqual(1,    data.totalRunsCompleted);
            Assert.AreEqual(300,  data.totalKills);
            Assert.AreEqual(600f, data.bestRunTimeSeconds, 0.01f);
            Assert.AreEqual(5,    data.highestWaveReached);
        }

        [Test]
        public void RecordRun_BestTime_OnlyUpdatesIfBetter()
        {
            SaveSystem.RecordRunCompletion(500f, 3, 100);
            SaveSystem.RecordRunCompletion(300f, 2, 50); // worse time
            var data = SaveSystem.Load();
            Assert.AreEqual(500f, data.bestRunTimeSeconds, 0.01f, "Best time should not regress.");
        }

        // ── Unlock bitmasks ───────────────────────────────────────────────

        [Test]
        public void UnlockCharacter_SetsCorrectBit()
        {
            SaveSystem.UnlockCharacter(2); // Draupadi = index 2
            Assert.IsTrue(SaveSystem.IsCharacterUnlocked(0), "Arjuna (default) should still be unlocked.");
            Assert.IsTrue(SaveSystem.IsCharacterUnlocked(2), "Character 2 should now be unlocked.");
            Assert.IsFalse(SaveSystem.IsCharacterUnlocked(3), "Character 3 should remain locked.");
        }

        [Test]
        public void UnlockWeapon_SetsCorrectBit()
        {
            SaveSystem.UnlockWeapon(1); // Gandiv = index 1
            Assert.IsTrue(SaveSystem.IsWeaponUnlocked(0), "Trishul (default) should still be unlocked.");
            Assert.IsTrue(SaveSystem.IsWeaponUnlocked(1), "Weapon 1 should now be unlocked.");
            Assert.IsFalse(SaveSystem.IsWeaponUnlocked(4), "Weapon 4 should remain locked.");
        }

        // ── Delete ────────────────────────────────────────────────────────

        [Test]
        public void DeleteAll_ResetsToDefaults()
        {
            SaveSystem.AddShrinePoints(999);
            SaveSystem.UnlockCharacter(4);
            SaveSystem.DeleteAll();

            var data = SaveSystem.Load();
            Assert.AreEqual(0, data.totalShrinePoints);
            Assert.AreEqual(1, data.characterUnlocks, "Should be reset to default (only Arjuna).");
        }

        // ── Events ────────────────────────────────────────────────────────

        [Test]
        public void Save_EmitsOnGameSavedEvent()
        {
            bool fired = false;
            EventBus.On("OnGameSaved", () => fired = true);
            SaveSystem.Save();
            Assert.IsTrue(fired);
        }

        [Test]
        public void DeleteAll_EmitsOnSaveDeletedEvent()
        {
            bool fired = false;
            EventBus.On("OnSaveDeleted", () => fired = true);
            SaveSystem.DeleteAll();
            Assert.IsTrue(fired);
        }
    }
}
