using System.Collections.Generic;
using NUnit.Framework;
using AGNIDAWN.Core;
using AGNIDAWN.Gameplay.MetaProgression;

namespace AGNIDAWN.Tests.EditMode
{
    /// <summary>
    /// Edit-mode tests for Phase 8 — Meta-Progression & Shrine System.
    /// Tests use the SaveSystem in-memory cache only (no file I/O in EditMode).
    /// Linear: FAI-13
    /// </summary>
    public class MetaProgressionTests
    {
        // ─────────────────────────────────────────────────────────────────
        // Helpers

        /// Reset SaveSystem in-memory cache to a clean state before each test.
        [SetUp]
        public void SetUp()
        {
            // Force the cache to a fresh SaveData instance by accessing via reflection
            // (SaveSystem._cache is private static — we reset it by deleting the fake file
            //  in a real environment, but in EditMode we directly set via DeleteAll logic)
            SaveSystem.DeleteAll();
        }

        // ─────────────────────────────────────────────────────────────────
        // Divine Shards

        [Test]
        public void DivineShard_InitialValueIsZero()
        {
            Assert.AreEqual(0, SaveSystem.GetDivineShards());
        }

        [Test]
        public void DivineShard_AddShards_AccumulatesCorrectly()
        {
            SaveSystem.AddDivineShards(10);
            SaveSystem.AddDivineShards(25);
            Assert.AreEqual(35, SaveSystem.GetDivineShards());
        }

        [Test]
        public void DivineShard_SpendShards_DeductsCorrectly()
        {
            SaveSystem.AddDivineShards(100);
            bool spent = SaveSystem.SpendDivineShards(40);
            Assert.IsTrue(spent);
            Assert.AreEqual(60, SaveSystem.GetDivineShards());
        }

        [Test]
        public void DivineShard_SpendShards_FailsWhenInsufficient()
        {
            SaveSystem.AddDivineShards(30);
            bool spent = SaveSystem.SpendDivineShards(50);
            Assert.IsFalse(spent);
            Assert.AreEqual(30, SaveSystem.GetDivineShards(), "Shards should not change on failed spend");
        }

        [Test]
        public void DivineShard_CanAffordShards_ReturnsCorrectly()
        {
            SaveSystem.AddDivineShards(50);
            Assert.IsTrue(SaveSystem.CanAffordShards(50));
            Assert.IsTrue(SaveSystem.CanAffordShards(49));
            Assert.IsFalse(SaveSystem.CanAffordShards(51));
        }

        [Test]
        public void DivineShard_AddZero_DoesNothing()
        {
            SaveSystem.AddDivineShards(0);
            Assert.AreEqual(0, SaveSystem.GetDivineShards());
        }

        [Test]
        public void DivineShard_AddNegative_DoesNothing()
        {
            SaveSystem.AddDivineShards(10);
            SaveSystem.AddDivineShards(-5);
            Assert.AreEqual(10, SaveSystem.GetDivineShards(), "Negative add should be ignored");
        }

        // ─────────────────────────────────────────────────────────────────
        // Shrine Unlocks

        [Test]
        public void Shrine_NotUnlockedByDefault()
        {
            Assert.IsFalse(SaveSystem.IsShrineUnlocked("brahma_weapon_1"));
        }

        [Test]
        public void Shrine_UnlockShrine_PersistsCorrectly()
        {
            SaveSystem.UnlockShrine("brahma_weapon_1");
            Assert.IsTrue(SaveSystem.IsShrineUnlocked("brahma_weapon_1"));
        }

        [Test]
        public void Shrine_UnlockSameShrineTwice_NoDuplicates()
        {
            SaveSystem.UnlockShrine("vishnu_passive_1");
            SaveSystem.UnlockShrine("vishnu_passive_1");
            var ids = SaveSystem.GetUnlockedShrineIds();
            Assert.AreEqual(1, ids.Count);
        }

        [Test]
        public void Shrine_MultipleUnlocks_AllPresent()
        {
            SaveSystem.UnlockShrine("brahma_weapon_1");
            SaveSystem.UnlockShrine("shiva_character_1");
            SaveSystem.UnlockShrine("lakshmi_shard_1");

            Assert.IsTrue(SaveSystem.IsShrineUnlocked("brahma_weapon_1"));
            Assert.IsTrue(SaveSystem.IsShrineUnlocked("shiva_character_1"));
            Assert.IsTrue(SaveSystem.IsShrineUnlocked("lakshmi_shard_1"));
            Assert.AreEqual(3, SaveSystem.GetUnlockedShrineIds().Count);
        }

        [Test]
        public void Shrine_PurchaseFlow_DeductsShards()
        {
            // Simulate a shrine purchase: check → spend → unlock
            SaveSystem.AddDivineShards(100);
            bool affordable = SaveSystem.CanAffordShards(50);
            Assert.IsTrue(affordable);

            bool spent = SaveSystem.SpendDivineShards(50);
            Assert.IsTrue(spent);
            Assert.AreEqual(50, SaveSystem.GetDivineShards());

            SaveSystem.UnlockShrine("brahma_weapon_2");
            Assert.IsTrue(SaveSystem.IsShrineUnlocked("brahma_weapon_2"));
        }

        // ─────────────────────────────────────────────────────────────────
        // Lore Fragments

        [Test]
        public void Lore_NotCollectedByDefault()
        {
            Assert.IsFalse(SaveSystem.IsLoreCollected("ravana_shloka_1"));
        }

        [Test]
        public void Lore_CollectFragment_PersistsCorrectly()
        {
            SaveSystem.CollectLore("ravana_shloka_1");
            Assert.IsTrue(SaveSystem.IsLoreCollected("ravana_shloka_1"));
        }

        [Test]
        public void Lore_CollectSameFragmentTwice_NoDuplicates()
        {
            SaveSystem.CollectLore("mahishasura_shloka_1");
            SaveSystem.CollectLore("mahishasura_shloka_1");
            var ids = SaveSystem.GetCollectedLoreIds();
            Assert.AreEqual(1, ids.Count);
        }

        [Test]
        public void Lore_MultipleFragments_AllPresent()
        {
            SaveSystem.CollectLore("ravana_shloka_1");
            SaveSystem.CollectLore("kali_shloka_1");
            SaveSystem.CollectLore("vritra_shloka_1");

            Assert.AreEqual(3, SaveSystem.GetCollectedLoreIds().Count);
            Assert.IsTrue(SaveSystem.IsLoreCollected("kali_shloka_1"));
        }

        // ─────────────────────────────────────────────────────────────────
        // Difficulty Persistence

        [Test]
        public void Difficulty_DefaultIsNormal()
        {
            Assert.AreEqual("Normal", SaveSystem.GetSelectedDifficultyId());
        }

        [Test]
        public void Difficulty_SetDifficulty_Persists()
        {
            SaveSystem.SetSelectedDifficulty("Tandav");
            Assert.AreEqual("Tandav", SaveSystem.GetSelectedDifficultyId());
        }

        [Test]
        public void Difficulty_OverwriteDifficulty_UpdatesCorrectly()
        {
            SaveSystem.SetSelectedDifficulty("Tandav");
            SaveSystem.SetSelectedDifficulty("Pralaya");
            Assert.AreEqual("Pralaya", SaveSystem.GetSelectedDifficultyId());
        }

        // ─────────────────────────────────────────────────────────────────
        // Combined: Shard + Shrine + Lore reset on DeleteAll

        [Test]
        public void DeleteAll_ClearsAllPhase8Data()
        {
            SaveSystem.AddDivineShards(200);
            SaveSystem.UnlockShrine("brahma_weapon_1");
            SaveSystem.CollectLore("ravana_shloka_1");
            SaveSystem.SetSelectedDifficulty("Pralaya");

            SaveSystem.DeleteAll();

            Assert.AreEqual(0, SaveSystem.GetDivineShards());
            Assert.IsFalse(SaveSystem.IsShrineUnlocked("brahma_weapon_1"));
            Assert.IsFalse(SaveSystem.IsLoreCollected("ravana_shloka_1"));
            Assert.AreEqual("Normal", SaveSystem.GetSelectedDifficultyId());
        }
    }
}
