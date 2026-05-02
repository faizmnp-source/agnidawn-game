using NUnit.Framework;
using UnityEngine;
using AGNIDAWN.Visuals;

namespace AGNIDAWN.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for Phase 10 — URP Post-Processing / Visual System.
    ///
    /// Tests cover the data layer (AgniTierVisualData) and component
    /// instantiation.  Runtime URP Volume lerping requires PlayMode and is
    /// validated manually in the editor.
    ///
    /// 18 tests total.
    /// Linear: FAI-15
    /// </summary>
    [TestFixture]
    public class VisualSystemTests
    {
        private AgniTierVisualData _visualData;

        [SetUp]
        public void SetUp()
        {
            _visualData = ScriptableObject.CreateInstance<AgniTierVisualData>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_visualData);
        }

        // ── AgniTierVisualData — Structure ────────────────────────────────────────

        [Test]
        public void VisualData_IsScriptableObject()
        {
            Assert.IsInstanceOf<ScriptableObject>(_visualData,
                "AgniTierVisualData must derive from ScriptableObject.");
        }

        [Test]
        public void VisualData_TierCount_IsFive()
        {
            Assert.AreEqual(5, _visualData.TierCount,
                "AgniTierVisualData must define exactly 5 tier configs (one per Agni tier).");
        }

        [Test]
        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void VisualData_TryGetConfig_ValidTier_ReturnsTrue(int tier)
        {
            bool found = _visualData.TryGetConfig(tier, out _);
            Assert.IsTrue(found, $"TryGetConfig({tier}) should succeed for a valid tier.");
        }

        [Test]
        [TestCase(0)] [TestCase(6)] [TestCase(-1)] [TestCase(99)]
        public void VisualData_TryGetConfig_OutOfRange_ReturnsFalse(int tier)
        {
            bool found = _visualData.TryGetConfig(tier, out _);
            Assert.IsFalse(found,
                $"TryGetConfig({tier}) should return false for an out-of-range tier.");
        }

        // ── AgniTierVisualData — Bloom ────────────────────────────────────────────

        [Test]
        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void VisualData_BloomIntensity_IsNonNegative(int tier)
        {
            _visualData.TryGetConfig(tier, out var cfg);
            Assert.GreaterOrEqual(cfg.bloomIntensity, 0f,
                $"Tier {tier} bloomIntensity must be >= 0.");
        }

        [Test]
        public void VisualData_Tier5_HasMoreBloomThanTier1()
        {
            _visualData.TryGetConfig(1, out var t1);
            _visualData.TryGetConfig(5, out var t5);
            Assert.Greater(t5.bloomIntensity, t1.bloomIntensity,
                "Blazing Agni (tier 5) should have more bloom intensity than dying Agni (tier 1).");
        }

        // ── AgniTierVisualData — Vignette ─────────────────────────────────────────

        [Test]
        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void VisualData_VignetteIntensity_IsInRange(int tier)
        {
            _visualData.TryGetConfig(tier, out var cfg);
            Assert.GreaterOrEqual(cfg.vignetteIntensity, 0f,
                $"Tier {tier} vignetteIntensity must be >= 0.");
            Assert.LessOrEqual(cfg.vignetteIntensity, 1f,
                $"Tier {tier} vignetteIntensity must be <= 1.");
        }

        [Test]
        public void VisualData_Tier1_HasHigherVignetteThanTier5()
        {
            _visualData.TryGetConfig(1, out var t1);
            _visualData.TryGetConfig(5, out var t5);
            Assert.Greater(t1.vignetteIntensity, t5.vignetteIntensity,
                "Dying Agni (tier 1) should have heavier vignette than blazing Agni (tier 5).");
        }

        // ── AgniTierVisualData — Chromatic Aberration ─────────────────────────────

        [Test]
        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void VisualData_ChromaticAberration_IsInRange(int tier)
        {
            _visualData.TryGetConfig(tier, out var cfg);
            Assert.GreaterOrEqual(cfg.chromaticAberration, 0f,
                $"Tier {tier} chromaticAberration must be >= 0.");
            Assert.LessOrEqual(cfg.chromaticAberration, 1f,
                $"Tier {tier} chromaticAberration must be <= 1.");
        }

        // ── AgniTierVisualData — Saturation ───────────────────────────────────────

        [Test]
        public void VisualData_Tier5_IsMoreSaturatedThanTier1()
        {
            _visualData.TryGetConfig(1, out var t1);
            _visualData.TryGetConfig(5, out var t5);
            Assert.Greater(t5.saturation, t1.saturation,
                "Blazing Agni (tier 5) should be more saturated than dying Agni (tier 1).");
        }

        // ── AgniTierVisualData — Transition Duration ──────────────────────────────

        [Test]
        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void VisualData_TransitionDuration_IsPositive(int tier)
        {
            _visualData.TryGetConfig(tier, out var cfg);
            Assert.Greater(cfg.transitionDuration, 0f,
                $"Tier {tier} transitionDuration must be > 0.");
        }

        [Test]
        public void VisualData_Tier5_TransitionsFasterThanTier1()
        {
            _visualData.TryGetConfig(1, out var t1);
            _visualData.TryGetConfig(5, out var t5);
            Assert.Less(t5.transitionDuration, t1.transitionDuration,
                "Tier 5 (blazing) transitions should be snappier than tier 1 (dying) — urgency increases.");
        }

        // ── Component Instantiation ───────────────────────────────────────────────

        [Test]
        public void CameraShakeController_CanBeAttachedToCamera()
        {
            var go   = new GameObject("TestCamera");
            go.AddComponent<Camera>();
            var ctrl = go.AddComponent<CameraShakeController>();
            Assert.IsNotNull(ctrl, "CameraShakeController should add to a Camera GameObject without error.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ScreenFlashController_CanBeAttachedToGameObject()
        {
            var go   = new GameObject("TestFlash");
            var ctrl = go.AddComponent<ScreenFlashController>();
            Assert.IsNotNull(ctrl, "ScreenFlashController should add to a GameObject without error.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void AgniVisualStateManager_RequiresVolume()
        {
            var go = new GameObject("TestVSM");
            // AgniVisualStateManager has [RequireComponent(typeof(Volume))]
            // Adding it should also add a Volume component automatically
            var vsm = go.AddComponent<AgniVisualStateManager>();
            Assert.IsNotNull(vsm);
            // Volume (from UnityEngine.Rendering) should be present due to RequireComponent
            var vol = go.GetComponent<UnityEngine.Rendering.Volume>();
            Assert.IsNotNull(vol, "AgniVisualStateManager [RequireComponent] should auto-add Volume.");
            Object.DestroyImmediate(go);
        }
    }
}
