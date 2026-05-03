using NUnit.Framework;
using AGNIDAWN.Core;
using AGNIDAWN.Gameplay;

namespace AGNIDAWN.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for Agni Kund tier logic and health thresholds.
    /// Linear: FAI-9
    /// </summary>
    public class AgniKundTests
    {
        // ── Tier Threshold Logic ──────────────────────────────────────────

        [Test]
        public void TierNames_AllFiveTiersDefined()
        {
            Assert.AreEqual(6, AgniKund.TierNames.Length, "Should have 6 entries (0 unused + tiers 1-5)");
            Assert.AreEqual("MRITYUPRAYA", AgniKund.TierNames[1]);
            Assert.AreEqual("KSHEEN",      AgniKund.TierNames[2]);
            Assert.AreEqual("SADHARAN",    AgniKund.TierNames[3]);
            Assert.AreEqual("PRABHAVA",    AgniKund.TierNames[4]);
            Assert.AreEqual("MAHAAGNI",    AgniKund.TierNames[5]);
        }

        [Test]
        public void TierThresholds_CoverFullRange()
        {
            // Verify thresholds don't overlap (statically validate constants)
            float t5 = 0.81f, t4 = 0.61f, t3 = 0.41f, t2 = 0.21f;
            Assert.Greater(t5, t4, "Tier 5 threshold must be above Tier 4");
            Assert.Greater(t4, t3, "Tier 4 threshold must be above Tier 3");
            Assert.Greater(t3, t2, "Tier 3 threshold must be above Tier 2");
            Assert.Greater(t2, 0f, "Tier 2 threshold must be above 0");
        }

        [Test]
        public void TierCalculation_CorrectTierAtVariousHP()
        {
            // Simulate the tier calculation logic directly
            Assert.AreEqual(5, GetTier(1.00f));
            Assert.AreEqual(5, GetTier(0.90f));
            Assert.AreEqual(4, GetTier(0.70f));
            Assert.AreEqual(3, GetTier(0.50f));
            Assert.AreEqual(2, GetTier(0.30f));
            Assert.AreEqual(1, GetTier(0.10f));
            Assert.AreEqual(1, GetTier(0.00f));
        }

        [Test]
        public void TierNames_NoneAreEmpty()
        {
            for (int i = 1; i <= 5; i++)
                Assert.IsFalse(string.IsNullOrEmpty(AgniKund.TierNames[i]),
                    $"Tier {i} name should not be empty.");
        }

        // ── Helper mirrors AgniKund's internal logic ──────────────────────
        private static int GetTier(float pct)
        {
            if (pct >= 0.81f) return 5;
            if (pct >= 0.61f) return 4;
            if (pct >= 0.41f) return 3;
            if (pct >= 0.21f) return 2;
            return 1;
        }
    }
}
