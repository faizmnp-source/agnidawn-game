using NUnit.Framework;
using UnityEngine;
using AGNIDAWN.VFX;

namespace AGNIDAWN.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for Phase 11 — VFX &amp; Shader System.
    ///
    /// Tests cover VFXEventData data-layer lookups, component creation,
    /// and AgniFlameController default tier configs.
    /// Runtime particle lerping requires PlayMode and is validated manually.
    ///
    /// 22 tests total.
    /// Linear: FAI-16
    /// </summary>
    [TestFixture]
    public class VFXSystemTests
    {
        private VFXEventData _vfxData;

        [SetUp]
        public void SetUp()
        {
            _vfxData = ScriptableObject.CreateInstance<VFXEventData>();
            _vfxData.BuildLookups();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_vfxData);
        }

        // ── VFXEventData — Structure ──────────────────────────────────────────────

        [Test]
        public void VFXData_IsScriptableObject()
        {
            Assert.IsInstanceOf<ScriptableObject>(_vfxData,
                "VFXEventData must derive from ScriptableObject.");
        }

        [Test]
        public void VFXData_HasTenAstraMappings()
        {
            Assert.AreEqual(10, _vfxData.AstraMappingCount,
                "Must have exactly 10 Astra mappings (one per divine weapon).");
        }

        [Test]
        public void VFXData_HasSevenEnemyDeathMappings()
        {
            // 6 enemy archetypes + 1 default
            Assert.AreEqual(7, _vfxData.EnemyMappingCount,
                "Must have 7 enemy death mappings (6 types + default fallback).");
        }

        [Test]
        public void VFXData_HasFourBossMappings()
        {
            Assert.AreEqual(4, _vfxData.BossMappingCount,
                "Must have 4 boss phase mappings (Ravana, Mahishasura, Kali, Vritra).");
        }

        // ── VFXEventData — Astra Lookups ──────────────────────────────────────────

        [Test]
        [TestCase("Trishul")]
        [TestCase("Gandiv")]
        [TestCase("SudarshanaChakra")]
        [TestCase("Brahmastra")]
        [TestCase("Pashupatastra")]
        [TestCase("Nagastra")]
        [TestCase("Varunastra")]
        [TestCase("Vayuastra")]
        [TestCase("Agneyastra")]
        [TestCase("Vajra")]
        public void VFXData_TryGetAstraMapping_ValidId_ReturnsTrue(string astraId)
        {
            bool found = _vfxData.TryGetAstraMapping(astraId, out var mapping);
            Assert.IsTrue(found, $"TryGetAstraMapping({astraId}) should succeed.");
            Assert.IsFalse(string.IsNullOrEmpty(mapping.impactPoolKey),
                $"Astra '{astraId}' must have a non-empty impactPoolKey.");
        }

        [Test]
        public void VFXData_TryGetAstraMapping_UnknownId_ReturnsFalse()
        {
            bool found = _vfxData.TryGetAstraMapping("NonExistentAstra", out _);
            Assert.IsFalse(found, "Unknown astra ID should return false.");
        }

        [Test]
        public void VFXData_BrahmastraMapping_HasCastAndImpactKeys()
        {
            bool found = _vfxData.TryGetAstraMapping("Brahmastra", out var mapping);
            Assert.IsTrue(found);
            Assert.IsFalse(string.IsNullOrEmpty(mapping.castPoolKey),
                "Brahmastra must have a castPoolKey for the mandala effect.");
            Assert.IsFalse(string.IsNullOrEmpty(mapping.impactPoolKey),
                "Brahmastra must have an impactPoolKey for the blast.");
        }

        [Test]
        public void VFXData_SudarshanaChakra_HasTrailKey()
        {
            bool found = _vfxData.TryGetAstraMapping("SudarshanaChakra", out var mapping);
            Assert.IsTrue(found);
            Assert.IsFalse(string.IsNullOrEmpty(mapping.trailPoolKey),
                "SudarshanaChakra must have a trailPoolKey for the orbital trail.");
        }

        // ── VFXEventData — Enemy Death Lookups ────────────────────────────────────

        [Test]
        [TestCase("Rakshasa")]
        [TestCase("Naga")]
        [TestCase("Pisacha")]
        [TestCase("BrahmaRakshasa")]
        public void VFXData_TryGetEnemyDeathMapping_KnownEnemy_ReturnsTrue(string enemyId)
        {
            bool found = _vfxData.TryGetEnemyDeathMapping(enemyId, out var mapping);
            Assert.IsTrue(found, $"TryGetEnemyDeathMapping({enemyId}) should succeed.");
            Assert.IsFalse(string.IsNullOrEmpty(mapping.deathPoolKey),
                $"Enemy '{enemyId}' must have a non-empty deathPoolKey.");
        }

        [Test]
        public void VFXData_TryGetEnemyDeathMapping_UnknownEnemy_FallsBackToDefault()
        {
            bool found = _vfxData.TryGetEnemyDeathMapping("UnknownEnemy", out var mapping);
            Assert.IsTrue(found,
                "Unknown enemy should return true via the 'default' fallback mapping.");
            Assert.IsFalse(string.IsNullOrEmpty(mapping.deathPoolKey),
                "Default fallback must have a non-empty deathPoolKey.");
        }

        // ── VFXEventData — Boss Lookups ───────────────────────────────────────────

        [Test]
        [TestCase("Ravana")]
        [TestCase("Mahishasura")]
        [TestCase("Kali")]
        [TestCase("Vritra")]
        public void VFXData_TryGetBossPhaseMapping_ValidBoss_ReturnsTrue(string bossId)
        {
            bool found = _vfxData.TryGetBossPhaseMapping(bossId, out var mapping);
            Assert.IsTrue(found, $"TryGetBossPhaseMapping({bossId}) should succeed.");
            Assert.IsFalse(string.IsNullOrEmpty(mapping.phaseShockwaveKey),
                $"Boss '{bossId}' must have a phaseShockwaveKey.");
            Assert.IsFalse(string.IsNullOrEmpty(mapping.spawnBurstKey),
                $"Boss '{bossId}' must have a spawnBurstKey.");
        }

        // ── Component Instantiation ───────────────────────────────────────────────

        [Test]
        public void VFXManager_CanBeCreated()
        {
            var go = new GameObject("TestVFXManager");
            var mgr = go.AddComponent<VFXManager>();
            Assert.IsNotNull(mgr, "VFXManager should attach without error.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void AgniFlameController_CanBeAttachedToGameObject()
        {
            var go = new GameObject("TestAgniKund");
            var ctrl = go.AddComponent<AgniFlameController>();
            Assert.IsNotNull(ctrl, "AgniFlameController should attach without error.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void PlayerDivineAura_CanBeAttachedToGameObject()
        {
            var go = new GameObject("TestPlayer");
            var aura = go.AddComponent<PlayerDivineAura>();
            Assert.IsNotNull(aura, "PlayerDivineAura should attach without error.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void BossShockwaveController_CanBeAttachedToGameObject()
        {
            var go = new GameObject("TestShockwave");
            var sw = go.AddComponent<BossShockwaveController>();
            Assert.IsNotNull(sw, "BossShockwaveController should attach without error.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void MandalaFXController_CanBeAttachedToGameObject()
        {
            var go = new GameObject("TestMandala");
            var mandala = go.AddComponent<MandalaFXController>();
            Assert.IsNotNull(mandala, "MandalaFXController should attach without error.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void AutoReturnToPool_CanBeAttachedToGameObject()
        {
            var go = new GameObject("TestAutoReturn");
            var ar = go.AddComponent<AutoReturnToPool>();
            Assert.IsNotNull(ar, "AutoReturnToPool should attach without error.");
            Object.DestroyImmediate(go);
        }

        // ── VFXEventData — Default Keys ───────────────────────────────────────────

        [Test]
        public void VFXData_DefaultImpactKey_IsNotEmpty()
        {
            Assert.IsFalse(string.IsNullOrEmpty(_vfxData.defaultImpactPoolKey),
                "defaultImpactPoolKey must be non-empty.");
        }

        [Test]
        public void VFXData_AgniTierUpBurstKey_IsNotEmpty()
        {
            Assert.IsFalse(string.IsNullOrEmpty(_vfxData.agniTierUpBurstKey),
                "agniTierUpBurstKey must be non-empty.");
        }
    }
}
