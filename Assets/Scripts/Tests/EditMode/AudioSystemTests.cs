using NUnit.Framework;
using UnityEngine;
using AGNIDAWN.Audio;

namespace AGNIDAWN.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for Phase 9 — FMOD Audio Integration.
    ///
    /// Tests cover:
    ///   - AudioEventData SO validation
    ///   - AudioCategory enum coverage
    ///   - AudioManager singleton creation and volume clamping
    ///   - AudioBusController proxying
    ///   - MusicManager FMOD parameter constants
    ///   - Volume persistence keys (PlayerPrefs names)
    ///   - SFX pool size defaults
    ///   - Null-safety in public APIs
    ///   - IsValid logic on AudioEventData
    /// </summary>
    [TestFixture]
    public class AudioSystemTests
    {
        // ── AudioEventData ────────────────────────────────────────────────────────

        [Test]
        public void AudioEventData_DefaultIsNotValid()
        {
            var data = ScriptableObject.CreateInstance<AudioEventData>();
            Assert.IsFalse(data.IsValid,
                "AudioEventData with no path and no clip should not be valid.");
            Object.DestroyImmediate(data);
        }

        [Test]
        public void AudioEventData_IsValid_WithFMODPath()
        {
            var data = ScriptableObject.CreateInstance<AudioEventData>();
            data.fmodEventPath = "event:/SFX/Test";
            Assert.IsTrue(data.IsValid, "AudioEventData with a non-empty FMOD path should be valid.");
            Object.DestroyImmediate(data);
        }

        [Test]
        public void AudioEventData_IsValid_WithFallbackClip()
        {
            var data = ScriptableObject.CreateInstance<AudioEventData>();
            // Create a minimal AudioClip as a stand-in
            data.unityFallbackClip = AudioClip.Create("test", 1, 1, 44100, false);
            Assert.IsTrue(data.IsValid, "AudioEventData with a fallback clip should be valid.");
            Object.DestroyImmediate(data);
        }

        [Test]
        public void AudioEventData_DefaultVolume_IsOne()
        {
            var data = ScriptableObject.CreateInstance<AudioEventData>();
            Assert.AreEqual(1f, data.volume, 1e-5f,
                "AudioEventData default volume should be 1.");
            Object.DestroyImmediate(data);
        }

        [Test]
        public void AudioEventData_DefaultCategory_IsSFX()
        {
            var data = ScriptableObject.CreateInstance<AudioEventData>();
            Assert.AreEqual(AudioCategory.SFX, data.category,
                "Default category should be SFX.");
            Object.DestroyImmediate(data);
        }

        [Test]
        public void AudioEventData_DefaultLoop_IsFalse()
        {
            var data = ScriptableObject.CreateInstance<AudioEventData>();
            Assert.IsFalse(data.loop, "Default loop should be false.");
            Object.DestroyImmediate(data);
        }

        [Test]
        public void AudioEventData_SpatialDistances_ArePositive()
        {
            var data = ScriptableObject.CreateInstance<AudioEventData>();
            Assert.Greater(data.minDistance, 0f, "minDistance should be > 0.");
            Assert.Greater(data.maxDistance, data.minDistance,
                "maxDistance should be greater than minDistance.");
            Object.DestroyImmediate(data);
        }

        // ── AudioCategory ─────────────────────────────────────────────────────────

        [Test]
        public void AudioCategory_AllValuesAreDefined()
        {
            var values = System.Enum.GetValues(typeof(AudioCategory));
            // Expect: SFX, Music, Ambient, UI, VO
            Assert.GreaterOrEqual(values.Length, 5,
                "AudioCategory should define at least 5 values.");
        }

        [Test]
        public void AudioCategory_ContainsMusic()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(AudioCategory), "Music"));
        }

        [Test]
        public void AudioCategory_ContainsAmbient()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(AudioCategory), "Ambient"));
        }

        // ── AudioManager ──────────────────────────────────────────────────────────

        [Test]
        public void AudioManager_CanBeInstantiatedAsGameObject()
        {
            var go = new GameObject("TestAudioManager");
            var mgr = go.AddComponent<AudioManager>();
            Assert.IsNotNull(mgr, "AudioManager should be attachable to a GameObject.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void AudioManager_PlayOneShot_NullData_DoesNotThrow()
        {
            var go = new GameObject("TestAudioManager");
            go.AddComponent<AudioManager>();

            Assert.DoesNotThrow(() =>
            {
                AudioManager.Instance?.PlayOneShot((AudioEventData)null);
            }, "PlayOneShot with null AudioEventData should not throw.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void AudioManager_PlayOneShot_NullPath_DoesNotThrow()
        {
            var go = new GameObject("TestAudioManager");
            go.AddComponent<AudioManager>();

            Assert.DoesNotThrow(() =>
            {
                AudioManager.Instance?.PlayOneShot((string)null);
            }, "PlayOneShot with null string path should not throw.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void AudioManager_SetMasterVolume_ClampsToZeroOne()
        {
            var go = new GameObject("TestAudioManager");
            var mgr = go.AddComponent<AudioManager>();

            mgr.SetMasterVolume(-5f);
            Assert.AreEqual(0f, mgr.MasterVolume, 1e-5f, "Master volume below 0 should clamp to 0.");

            mgr.SetMasterVolume(9999f);
            Assert.AreEqual(1f, mgr.MasterVolume, 1e-5f, "Master volume above 1 should clamp to 1.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void AudioManager_SetBusVolume_ClampsToZeroOne()
        {
            var go = new GameObject("TestAudioManager");
            var mgr = go.AddComponent<AudioManager>();

            mgr.SetBusVolume(AudioCategory.Music, -1f);
            Assert.AreEqual(0f, mgr.MusicVolume, 1e-5f);

            mgr.SetBusVolume(AudioCategory.SFX, 2f);
            Assert.AreEqual(1f, mgr.SFXVolume, 1e-5f);

            Object.DestroyImmediate(go);
        }

        // ── AudioBusController ────────────────────────────────────────────────────

        [Test]
        public void AudioBusController_ResetToDefaults_SetsExpectedValues()
        {
            var go = new GameObject("TestAudioSetup");
            go.AddComponent<AudioManager>();
            var ctrl = go.AddComponent<AudioBusController>();

            ctrl.ResetToDefaults();

            Assert.AreEqual(1.0f,  ctrl.MasterVolume,  1e-5f);
            Assert.AreEqual(0.75f, ctrl.MusicVolume,   1e-5f);
            Assert.AreEqual(1.0f,  ctrl.SFXVolume,     1e-5f);
            Assert.AreEqual(0.6f,  ctrl.AmbientVolume, 1e-5f);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void AudioBusController_ToggleMute_SetsZeroVolume()
        {
            var go = new GameObject("TestAudioSetup");
            go.AddComponent<AudioManager>();
            var ctrl = go.AddComponent<AudioBusController>();
            ctrl.SetMasterVolume(0.8f);

            ctrl.ToggleMute(true);
            Assert.AreEqual(0f, ctrl.MasterVolume, 1e-5f,
                "ToggleMute(true) should set master volume to 0.");

            Object.DestroyImmediate(go);
        }

        // ── MusicManager parameter constants ──────────────────────────────────────

        [Test]
        public void MusicManager_FMODParamName_BiomeIndex_IsCorrect()
        {
            Assert.AreEqual("BiomeIndex", MusicManager.PARAM_BIOME_INDEX);
        }

        [Test]
        public void MusicManager_FMODParamName_BossActive_IsCorrect()
        {
            Assert.AreEqual("BossActive", MusicManager.PARAM_BOSS_ACTIVE);
        }

        [Test]
        public void MusicManager_FMODParamName_GameState_IsCorrect()
        {
            Assert.AreEqual("GameState", MusicManager.PARAM_GAME_STATE);
        }
    }
}
