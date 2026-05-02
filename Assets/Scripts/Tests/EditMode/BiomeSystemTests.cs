using NUnit.Framework;
using AGNIDAWN.Core;

namespace AGNIDAWN.Tests.EditMode
{
    /// <summary>
    /// Edit-mode tests for the Phase 7 Biome system.
    /// Tests BiomeData ScriptableObject defaults, BiomeType enum coverage,
    /// and modifier multiplier ranges — no MonoBehaviour lifecycle required.
    ///
    /// Linear: FAI-12
    /// </summary>
    public class BiomeSystemTests
    {
        // ──────────────────────────────────────────────────────────────────
        #region BiomeData ScriptableObject

        [Test]
        public void BiomeData_DefaultSpawnIntervalMultiplier_IsOne()
        {
            var biome = UnityEngine.ScriptableObject.CreateInstance<BiomeData>();
            Assert.AreEqual(1f, biome.spawnIntervalMultiplier,
                "Default spawn interval multiplier should be 1 (no scaling)");
            UnityEngine.Object.DestroyImmediate(biome);
        }

        [Test]
        public void BiomeData_DefaultEnemySpeedMultiplier_IsOne()
        {
            var biome = UnityEngine.ScriptableObject.CreateInstance<BiomeData>();
            Assert.AreEqual(1f, biome.enemySpeedMultiplier,
                "Default enemy speed multiplier should be 1");
            UnityEngine.Object.DestroyImmediate(biome);
        }

        [Test]
        public void BiomeData_DefaultEnemyDamageMultiplier_IsOne()
        {
            var biome = UnityEngine.ScriptableObject.CreateInstance<BiomeData>();
            Assert.AreEqual(1f, biome.enemyDamageMultiplier,
                "Default enemy damage multiplier should be 1");
            UnityEngine.Object.DestroyImmediate(biome);
        }

        [Test]
        public void BiomeData_DefaultEliteChanceMultiplier_IsOne()
        {
            var biome = UnityEngine.ScriptableObject.CreateInstance<BiomeData>();
            Assert.AreEqual(1f, biome.eliteChanceMultiplier,
                "Default elite chance multiplier should be 1");
            UnityEngine.Object.DestroyImmediate(biome);
        }

        [Test]
        public void BiomeData_DefaultPlayerSpeedMultiplier_IsOne()
        {
            var biome = UnityEngine.ScriptableObject.CreateInstance<BiomeData>();
            Assert.AreEqual(1f, biome.playerSpeedMultiplier,
                "Default player speed multiplier should be 1");
            UnityEngine.Object.DestroyImmediate(biome);
        }

        [Test]
        public void BiomeData_DefaultAgniKundVulnerability_IsOne()
        {
            var biome = UnityEngine.ScriptableObject.CreateInstance<BiomeData>();
            Assert.AreEqual(1f, biome.agniKundVulnerabilityMult,
                "Default Agni Kund vulnerability multiplier should be 1");
            UnityEngine.Object.DestroyImmediate(biome);
        }

        [Test]
        public void BiomeData_DefaultActiveDurationMinutes_IsFour()
        {
            var biome = UnityEngine.ScriptableObject.CreateInstance<BiomeData>();
            Assert.AreEqual(4f, biome.activeDurationMinutes,
                "Default active duration should be 4 minutes (5 biomes × 4 min = 20 min run)");
            UnityEngine.Object.DestroyImmediate(biome);
        }

        [Test]
        public void BiomeData_DefaultTransitionDuration_IsTwo()
        {
            var biome = UnityEngine.ScriptableObject.CreateInstance<BiomeData>();
            Assert.AreEqual(2f, biome.transitionDurationSeconds,
                "Default transition duration should be 2 seconds");
            UnityEngine.Object.DestroyImmediate(biome);
        }

        [Test]
        public void BiomeData_SetModifiers_ReflectedInProperties()
        {
            var biome = UnityEngine.ScriptableObject.CreateInstance<BiomeData>();
            biome.biomeType               = BiomeType.Underworld;
            biome.displayName             = "Patala";
            biome.spawnIntervalMultiplier = 0.6f;
            biome.enemySpeedMultiplier    = 1.3f;
            biome.enemyDamageMultiplier   = 1.5f;
            biome.eliteChanceMultiplier   = 2f;

            Assert.AreEqual(BiomeType.Underworld, biome.biomeType);
            Assert.AreEqual("Patala",             biome.displayName);
            Assert.AreEqual(0.6f, biome.spawnIntervalMultiplier, 0.001f);
            Assert.AreEqual(1.3f, biome.enemySpeedMultiplier,    0.001f);
            Assert.AreEqual(1.5f, biome.enemyDamageMultiplier,   0.001f);
            Assert.AreEqual(2f,   biome.eliteChanceMultiplier,   0.001f);

            UnityEngine.Object.DestroyImmediate(biome);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region BiomeType Enum

        [Test]
        public void BiomeType_AllFiveBiomesExist()
        {
            var names = System.Enum.GetNames(typeof(BiomeType));
            Assert.AreEqual(5, names.Length,
                "There should be exactly 5 biome types: Forest, Desert, Mountain, Ocean, Underworld");
        }

        [Test]
        public void BiomeType_ContainsForest()      => Assert.IsTrue(System.Enum.IsDefined(typeof(BiomeType), "Forest"));
        [Test]
        public void BiomeType_ContainsDesert()      => Assert.IsTrue(System.Enum.IsDefined(typeof(BiomeType), "Desert"));
        [Test]
        public void BiomeType_ContainsMountain()    => Assert.IsTrue(System.Enum.IsDefined(typeof(BiomeType), "Mountain"));
        [Test]
        public void BiomeType_ContainsOcean()       => Assert.IsTrue(System.Enum.IsDefined(typeof(BiomeType), "Ocean"));
        [Test]
        public void BiomeType_ContainsUnderworld()  => Assert.IsTrue(System.Enum.IsDefined(typeof(BiomeType), "Underworld"));

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Modifier Math

        [Test]
        public void BiomeData_TotalRunDuration_FiveBiomesFourMinutesEach_IsTwenty()
        {
            // Verify that 5 biomes × 4 minutes default = 20-minute run
            float total = 0f;
            for (int i = 0; i < 5; i++)
            {
                var b = UnityEngine.ScriptableObject.CreateInstance<BiomeData>();
                total += b.activeDurationMinutes;
                UnityEngine.Object.DestroyImmediate(b);
            }
            Assert.AreEqual(20f, total, 0.001f,
                "Five default BiomeData assets should total 20 minutes");
        }

        [Test]
        public void BiomeData_UnderworldModifiers_HigherThanDefault()
        {
            var underworld = UnityEngine.ScriptableObject.CreateInstance<BiomeData>();
            underworld.enemySpeedMultiplier  = 1.4f;
            underworld.enemyDamageMultiplier = 1.6f;
            underworld.eliteChanceMultiplier = 2.5f;

            var forest = UnityEngine.ScriptableObject.CreateInstance<BiomeData>();
            // forest uses defaults (all 1.0)

            Assert.Greater(underworld.enemySpeedMultiplier,  forest.enemySpeedMultiplier);
            Assert.Greater(underworld.enemyDamageMultiplier, forest.enemyDamageMultiplier);
            Assert.Greater(underworld.eliteChanceMultiplier, forest.eliteChanceMultiplier);

            UnityEngine.Object.DestroyImmediate(underworld);
            UnityEngine.Object.DestroyImmediate(forest);
        }

        #endregion
    }
}
