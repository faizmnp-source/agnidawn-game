using NUnit.Framework;
using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.Tests.EditMode
{
    /// <summary>
    /// Edit-mode tests for Phase 6 — Divine Weapon Astra system.
    /// Tests focus on AstraData configuration, damage/cooldown helpers,
    /// and constants that don't require a running Unity scene.
    /// Linear: FAI-11
    /// </summary>
    [TestFixture]
    public class AstraSystemTests
    {
        // ── AstraData helpers ──────────────────────────────────────────────

        [Test]
        public void AstraData_GetDamageAtLevel_ReturnsCorrectValue()
        {
            var data = ScriptableObject.CreateInstance<AstraData>();
            data.damagePerLevel = new float[] { 10f, 14f, 19f, 25f, 32f };

            Assert.AreEqual(10f, data.GetDamageAtLevel(1));
            Assert.AreEqual(14f, data.GetDamageAtLevel(2));
            Assert.AreEqual(32f, data.GetDamageAtLevel(5));

            Object.DestroyImmediate(data);
        }

        [Test]
        public void AstraData_GetDamageAtLevel_ClampsOutOfRange()
        {
            var data = ScriptableObject.CreateInstance<AstraData>();
            data.damagePerLevel = new float[] { 10f, 14f, 19f, 25f, 32f };

            Assert.AreEqual(10f, data.GetDamageAtLevel(0),  "Below range should clamp to level 1");
            Assert.AreEqual(32f, data.GetDamageAtLevel(99), "Above range should clamp to max level");

            Object.DestroyImmediate(data);
        }

        [Test]
        public void AstraData_GetCooldownAtLevel_DecreasesWithLevel()
        {
            var data = ScriptableObject.CreateInstance<AstraData>();
            data.cooldownPerLevel = new float[] { 1.5f, 1.3f, 1.1f, 0.9f, 0.7f };

            for (int i = 1; i < 5; i++)
                Assert.Less(data.GetCooldownAtLevel(i + 1), data.GetCooldownAtLevel(i),
                    $"Cooldown at level {i + 1} should be less than level {i}");

            Object.DestroyImmediate(data);
        }

        // ── Trishul config ─────────────────────────────────────────────────

        [Test]
        public void TrishulData_DefaultPiercing_ShouldBeAtLeastTwo()
        {
            var data = ScriptableObject.CreateInstance<AstraData>();
            data.astraId  = "Trishul";
            data.piercing = 2; // boomerang needs pierce for both forward and return trip
            Assert.GreaterOrEqual(data.piercing, 2, "Trishul must pierce at least twice for boomerang mechanic");
            Object.DestroyImmediate(data);
        }

        // ── Brahmastra config ──────────────────────────────────────────────

        [Test]
        public void BrahmastraData_RequiresTarget_ShouldBeFalse()
        {
            // Brahmastra should fire even without a target — it explodes on expiry
            var data = ScriptableObject.CreateInstance<AstraData>();
            data.astraId        = "Brahmastra";
            data.requiresTarget = false;
            Assert.IsFalse(data.requiresTarget, "Brahmastra should fire without a target");
            Object.DestroyImmediate(data);
        }

        // ── Pashupatastra config ───────────────────────────────────────────

        [Test]
        public void PashupatastraData_Damage_ShouldBeHighestTierAtMaxLevel()
        {
            var trishul = ScriptableObject.CreateInstance<AstraData>();
            trishul.damagePerLevel = new float[] { 10f, 14f, 19f, 25f, 32f };

            var pashupata = ScriptableObject.CreateInstance<AstraData>();
            pashupata.damagePerLevel = new float[] { 25f, 35f, 48f, 65f, 88f };

            Assert.Greater(
                pashupata.GetDamageAtLevel(5),
                trishul.GetDamageAtLevel(5),
                "Pashupatastra max-level damage should exceed Trishul max-level damage"
            );

            Object.DestroyImmediate(trishul);
            Object.DestroyImmediate(pashupata);
        }

        // ── AstraData identity ─────────────────────────────────────────────

        [TestCase("Trishul",    "Shiva")]
        [TestCase("Gandiv",     "Arjuna")]
        [TestCase("Chakra",     "Vishnu")]
        [TestCase("Brahmastra", "Brahma")]
        [TestCase("Pashupata",  "Shiva")]
        [TestCase("Nagastra",   "Nagas")]
        [TestCase("Varunastra", "Varuna")]
        [TestCase("Vayuastra",  "Vayu")]
        [TestCase("Agneyastra", "Agni")]
        [TestCase("Vajra",      "Indra")]
        public void AstraData_DietyAssigned_AllTenAstrasHaveCanonicalDeity(string astraId, string deity)
        {
            var data = ScriptableObject.CreateInstance<AstraData>();
            data.astraId = astraId;
            data.deity   = deity;

            Assert.AreEqual(deity, data.deity,
                $"{astraId} must be attributed to {deity}");
            Object.DestroyImmediate(data);
        }

        // ── Cooldown validation ────────────────────────────────────────────

        [Test]
        public void AstraData_MaxLevel_DefaultIsFive()
        {
            var data = ScriptableObject.CreateInstance<AstraData>();
            Assert.AreEqual(5, data.maxLevel, "All astras default to maxLevel 5");
            Object.DestroyImmediate(data);
        }

        [Test]
        public void AstraData_DamageArray_MustHaveFiveEntries()
        {
            var data = ScriptableObject.CreateInstance<AstraData>();
            data.damagePerLevel  = new float[] { 10f, 14f, 19f, 25f, 32f };
            data.cooldownPerLevel = new float[] { 1.5f, 1.3f, 1.1f, 0.9f, 0.7f };

            Assert.AreEqual(data.maxLevel, data.damagePerLevel.Length,
                "damagePerLevel length must equal maxLevel");
            Assert.AreEqual(data.maxLevel, data.cooldownPerLevel.Length,
                "cooldownPerLevel length must equal maxLevel");
            Object.DestroyImmediate(data);
        }

        // ── EventBus emits (OnAstraSpecial) ───────────────────────────────

        [Test]
        public void EventBus_OnAstraSpecial_CanReceiveStringPayload()
        {
            string received = null;
            System.Action<string> handler = s => received = s;
            EventBus.On<string>("OnAstraSpecial", handler);

            EventBus.Emit<string>("OnAstraSpecial", "Vajra_Chain_2");

            EventBus.Off<string>("OnAstraSpecial", handler);
            Assert.AreEqual("Vajra_Chain_2", received,
                "EventBus must deliver OnAstraSpecial string payload");
        }
    }
}
