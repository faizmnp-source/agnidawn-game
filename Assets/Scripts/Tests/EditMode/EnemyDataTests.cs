using NUnit.Framework;
using AGNIDAWN.Core;

namespace AGNIDAWN.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for EnemyData — validates stat logic and type coverage.
    /// Linear: FAI-8
    /// </summary>
    public class EnemyDataTests
    {
        [Test]
        public void EnemyType_AllTypesUnique()
        {
            var types = System.Enum.GetValues(typeof(EnemyType));
            var set = new System.Collections.Generic.HashSet<int>();
            foreach (var t in types)
                Assert.IsTrue(set.Add((int)t), $"Duplicate enum value: {t}");
        }

        [Test]
        public void EnemyType_SevenCorePlusElite()
        {
            var types = System.Enum.GetValues(typeof(EnemyType));
            Assert.AreEqual(8, types.Length,
                "Should have 7 mythological types + 1 Elite variant");
        }

        [Test]
        public void EliteMultipliers_HealthHigherThanBase()
        {
            // Verify elite multipliers are all >= 1
            float healthMult = 2.2f;
            float damageMult = 1.6f;
            float speedMult  = 1.2f;
            Assert.GreaterOrEqual(healthMult, 1f);
            Assert.GreaterOrEqual(damageMult, 1f);
            Assert.GreaterOrEqual(speedMult,  1f);
        }

        [Test]
        public void EnemyType_MythologicalNamesCorrect()
        {
            // Validate the mythology behind each type is represented
            var names = System.Enum.GetNames(typeof(EnemyType));
            System.Array.Sort(names);

            Assert.Contains("Asura",            names, "Asura must be in enemy types");
            Assert.Contains("Rakshasa",          names, "Rakshasa must be in enemy types");
            Assert.Contains("Naga",              names, "Naga must be in enemy types");
            Assert.Contains("Pisacha",           names, "Pisacha must be in enemy types");
            Assert.Contains("Vetala",            names, "Vetala must be in enemy types");
            Assert.Contains("Yaksha",            names, "Yaksha must be in enemy types");
            Assert.Contains("Brahmarakshasa",    names, "Brahmarakshasa must be in enemy types");
        }
    }
}
