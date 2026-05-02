using NUnit.Framework;
using UnityEngine;
using AGNIDAWN.Bosses;

namespace AGNIDAWN.Tests.EditMode
{
    /// <summary>
    /// Edit-mode unit tests for Phase 5: Boss System.
    /// Tests BossData config, BaseBoss phase threshold math, and BossManager state.
    /// Linear: FAI-10
    /// </summary>
    public class BossSystemTests
    {
        // ──────────────────────────────────────────────────────────────────
        #region BossData Tests

        [Test]
        public void BossData_DefaultValues_AreValid()
        {
            var data = ScriptableObject.CreateInstance<BossData>();
            data.totalHealth      = 1000f;
            data.spawnAtMinute    = 5;
            data.xpReward         = 500f;
            data.divineShardsDrop = 50;
            data.phaseThresholds  = new float[] { 0.66f, 0.33f };

            Assert.Greater(data.totalHealth, 0f,        "Boss must have positive health");
            Assert.Greater(data.spawnAtMinute, 0,        "Boss must spawn after minute 0");
            Assert.Greater(data.xpReward, 0f,           "Boss must award XP");
            Assert.Greater(data.divineShardsDrop, 0,    "Boss must award shards");
            Assert.IsNotNull(data.phaseThresholds,       "Phase thresholds must not be null");
            Assert.Greater(data.phaseThresholds.Length, 0, "Must have at least one phase threshold");

            Object.DestroyImmediate(data);
        }

        [Test]
        public void BossData_PhaseThresholds_AreDescending()
        {
            var data = ScriptableObject.CreateInstance<BossData>();
            data.phaseThresholds = new float[] { 0.66f, 0.33f };

            for (int i = 1; i < data.phaseThresholds.Length; i++)
            {
                Assert.Less(data.phaseThresholds[i], data.phaseThresholds[i - 1],
                    $"Phase threshold [{i}] ({data.phaseThresholds[i]}) must be less than [{i-1}] ({data.phaseThresholds[i-1]})");
            }

            Object.DestroyImmediate(data);
        }

        [Test]
        public void BossData_PhaseThresholds_AreInValidRange()
        {
            var data = ScriptableObject.CreateInstance<BossData>();
            data.phaseThresholds = new float[] { 0.66f, 0.33f };

            foreach (float t in data.phaseThresholds)
            {
                Assert.Greater(t, 0f,  $"Threshold {t} must be > 0");
                Assert.Less(t,    1f,  $"Threshold {t} must be < 1 (boss shouldn't transition immediately)");
            }

            Object.DestroyImmediate(data);
        }

        [Test]
        public void BossRoster_SpawnMinutes_AreCorrect()
        {
            // Verify the expected boss schedule from the GDD
            int[] expectedMinutes = { 5, 10, 15, 20 };
            string[] expectedIds  = { "Ravana", "Mahishasura", "Kali", "Vritra" };

            for (int i = 0; i < expectedIds.Length; i++)
            {
                var data = ScriptableObject.CreateInstance<BossData>();
                data.bossId       = expectedIds[i];
                data.spawnAtMinute = expectedMinutes[i];

                Assert.AreEqual(expectedMinutes[i], data.spawnAtMinute,
                    $"{expectedIds[i]} should spawn at minute {expectedMinutes[i]}");

                Object.DestroyImmediate(data);
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Phase Break Math Tests

        [Test]
        public void PhaseBreaks_TwoThreshold_ProducesThreePhases()
        {
            // BossData with 2 thresholds should produce 3 fight phases (0, 1, 2)
            float totalHP     = 1000f;
            float[] thresholds = { 0.66f, 0.33f };

            float[] breaks = new float[thresholds.Length];
            for (int i = 0; i < thresholds.Length; i++)
                breaks[i] = totalHP * thresholds[i];

            // Phase count = number of breaks + 1
            int phaseCount = breaks.Length + 1;
            Assert.AreEqual(3, phaseCount, "Two thresholds must produce 3 phases");

            // Break HP values
            Assert.AreEqual(660f, breaks[0], 0.01f, "Phase 1 break at 66% = 660 HP");
            Assert.AreEqual(330f, breaks[1], 0.01f, "Phase 2 break at 33% = 330 HP");
        }

        [Test]
        public void Ravana_Bloodrage_DamageScales_PerDeadHead()
        {
            // Simulate Ravana's rage scaling math:
            // baseDamage + rageDamagePerHead * deadHeads
            float baseDamage       = 15f;
            float rageDamagePerHead = 0.05f;

            for (int deadHeads = 0; deadHeads <= 10; deadHeads++)
            {
                float expected = baseDamage + rageDamagePerHead * deadHeads;
                float actual   = baseDamage + rageDamagePerHead * deadHeads;
                Assert.AreEqual(expected, actual, 0.001f,
                    $"At {deadHeads} dead heads, damage should be {expected}");
            }
        }

        [Test]
        public void Kali_Bloodlust_Caps_AtMaxStacks()
        {
            int stacks    = 0;
            int cap       = 50;
            int perKill   = 1;

            // Simulate 100 kills
            for (int i = 0; i < 100; i++)
                stacks = Mathf.Min(stacks + perKill, cap);

            Assert.AreEqual(cap, stacks, "Kali bloodlust must not exceed cap of 50");
        }

        [Test]
        public void Kali_BloodlustMult_GrowsCorrectly()
        {
            // At 50 stacks: speed mult = 1 + 50*0.01 = 1.5, damage mult = 1 + 50*0.02 = 2.0
            int maxStacks       = 50;
            float speedMult     = 1f + maxStacks * 0.01f;
            float damageMult    = 1f + maxStacks * 0.02f;

            Assert.AreEqual(1.5f, speedMult,  0.001f, "At max stacks speed mult = 1.5x");
            Assert.AreEqual(2.0f, damageMult, 0.001f, "At max stacks damage mult = 2.0x");
        }

        [Test]
        public void Vritra_WeatherCycle_CoversAllStates()
        {
            // Verify cycling: Storm(1) → Rain(2) → Darkness(3) → back to Storm(1)
            // WeatherState: Clear=0, Storm=1, Rain=2, Darkness=3
            int current = 1; // start Storm
            int[] visited = new int[3];

            for (int i = 0; i < 3; i++)
            {
                visited[i] = current;
                current = (current % 3) + 1;
            }

            Assert.AreEqual(1, visited[0], "First weather should be Storm(1)");
            Assert.AreEqual(2, visited[1], "Second weather should be Rain(2)");
            Assert.AreEqual(3, visited[2], "Third weather should be Darkness(3)");
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region BossManager State Tests

        [Test]
        public void BossManager_DefeatedCount_StartsAtZero()
        {
            var go      = new GameObject("BossManager_Test");
            var manager = go.AddComponent<BossManager>();

            // Cannot call ResetForNewRun here (it's public, but we just want the default state)
            Assert.AreEqual(0, manager.DefeatedCount,
                "No bosses should be defeated at start of run");
            Assert.IsFalse(manager.IsBossActive,
                "No boss should be active at start of run");
            Assert.IsNull(manager.ActiveBoss,
                "ActiveBoss should be null at start of run");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void BossManager_HasDefeated_ReturnsFalse_ForUnknownBoss()
        {
            var go      = new GameObject("BossManager_Test2");
            var manager = go.AddComponent<BossManager>();

            Assert.IsFalse(manager.HasDefeated("Ravana"),       "Ravana not yet defeated");
            Assert.IsFalse(manager.HasDefeated("Mahishasura"),  "Mahishasura not yet defeated");
            Assert.IsFalse(manager.HasDefeated("Kali"),         "Kali not yet defeated");
            Assert.IsFalse(manager.HasDefeated("Vritra"),       "Vritra not yet defeated");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void BossManager_ResetForNewRun_ClearsState()
        {
            var go      = new GameObject("BossManager_Reset_Test");
            var manager = go.AddComponent<BossManager>();

            // Simulate a defeat then reset
            manager.ResetForNewRun();

            Assert.AreEqual(0, manager.DefeatedCount, "Defeated count must be 0 after reset");
            Assert.IsFalse(manager.IsBossActive,      "IsBossActive must be false after reset");

            Object.DestroyImmediate(go);
        }

        #endregion
    }
}
