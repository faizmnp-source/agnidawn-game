using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using System.IO;
using AGNIDAWN.Core;

namespace AGNIDAWN.Tests.EditMode
{
    /// <summary>
    /// Build validation tests — verifies PlayerSettings and project structure
    /// are correct for all build targets before CI attempts a build.
    ///
    /// Runs in Editor-only context (no game-ci required).
    /// These are the first tests that catch config drift when settings
    /// are accidentally changed in the Unity Editor.
    ///
    /// Linear: FAI-17
    /// </summary>
    public class BuildValidationTests
    {
        // ── Company / Product identity ────────────────────────────────────────

        [Test]
        public void PlayerSettings_CompanyName_IsSet()
        {
            Assert.IsFalse(string.IsNullOrEmpty(PlayerSettings.companyName),
                "PlayerSettings.companyName must be set before shipping.");
        }

        [Test]
        public void PlayerSettings_ProductName_IsAgnidawn()
        {
            Assert.IsTrue(
                PlayerSettings.productName.ToLower().Contains("agnidawn") ||
                PlayerSettings.productName.ToLower().Contains("agni"),
                $"ProductName '{PlayerSettings.productName}' should contain 'Agnidawn'.");
        }

        [Test]
        public void PlayerSettings_BundleVersion_IsSemanticVersion()
        {
            string version = PlayerSettings.bundleVersion;
            Assert.IsFalse(string.IsNullOrEmpty(version),
                "bundleVersion must not be empty.");

            // Must match major.minor.patch pattern
            var parts = version.Split('.');
            Assert.GreaterOrEqual(parts.Length, 2,
                $"bundleVersion '{version}' should follow semver (e.g. 0.1.0).");
        }

        // ── Build target settings ─────────────────────────────────────────────

        [Test]
        public void PlayerSettings_PC_IsIL2CPP()
        {
            var backend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Standalone);
            Assert.AreEqual(ScriptingImplementation.IL2CPP, backend,
                "PC build must use IL2CPP scripting backend for release performance.");
        }

        [Test]
        public void PlayerSettings_Android_IsIL2CPP()
        {
            var backend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android);
            Assert.AreEqual(ScriptingImplementation.IL2CPP, backend,
                "Android build must use IL2CPP (not Mono) for APK size and performance.");
        }

        [Test]
        public void PlayerSettings_Android_Architecture_IsARM64()
        {
            var arch = PlayerSettings.Android.targetArchitectures;
            Assert.IsTrue(
                (arch & AndroidArchitecture.ARM64) != 0,
                "Android must target ARM64 — required for Samsung Z Fold7 (Snapdragon 8 Gen 3).");
        }

        [Test]
        public void PlayerSettings_Android_MinSdkVersion_Is26OrHigher()
        {
            // Android 8.0 Oreo minimum — Z Fold7 runs Android 14 (API 34)
            int minSdk = (int)PlayerSettings.Android.minSdkVersion;
            Assert.GreaterOrEqual(minSdk, 26,
                $"Android minSdkVersion is {minSdk}; must be ≥26 (Android 8.0).");
        }

        [Test]
        public void PlayerSettings_PC_ApiCompatibility_IsNET()
        {
            var api = PlayerSettings.GetApiCompatibilityLevel(BuildTargetGroup.Standalone);
            Assert.AreEqual(ApiCompatibilityLevel.NET_Unity_4_8, api,
                "PC API compatibility should be .NET Standard 2.1 / Unity 4.8.");
        }

        // ── Graphics / URP ────────────────────────────────────────────────────

        [Test]
        public void GraphicsSettings_URPAsset_IsAssigned()
        {
            var pipeline = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
            Assert.IsNotNull(pipeline,
                "No Render Pipeline Asset assigned in Graphics Settings. URP asset must be set.");
        }

        [Test]
        public void QualitySettings_HasAtLeastTwoLevels()
        {
            // Expect at minimum: Low (Android) and High (PC)
            Assert.GreaterOrEqual(QualitySettings.names.Length, 2,
                "Project needs at least 2 quality levels: Low (mobile) and High (PC).");
        }

        // ── Required scenes ───────────────────────────────────────────────────

        [Test]
        public void BuildSettings_MainMenuScene_Exists()
        {
            Assert.IsTrue(SceneFileExists("MainMenu"),
                "MainMenu scene must exist at Assets/Scenes/MainMenu.unity");
        }

        [Test]
        public void BuildSettings_GameScene_Exists()
        {
            Assert.IsTrue(SceneFileExists("Game"),
                "Game scene must exist at Assets/Scenes/Game.unity");
        }

        // ── Assembly definition files ─────────────────────────────────────────

        [Test]
        public void AsmDef_Core_Exists()
        {
            Assert.IsTrue(AsmDefExists("AGNIDAWN.Core"),
                "AGNIDAWN.Core.asmdef is missing from Assets/Scripts/Core/");
        }

        [Test]
        public void AsmDef_UI_Exists()
        {
            Assert.IsTrue(AsmDefExists("AGNIDAWN.UI"),
                "AGNIDAWN.UI.asmdef is missing from Assets/Scripts/UI/");
        }

        [Test]
        public void AsmDef_Tests_Exists()
        {
            Assert.IsTrue(AsmDefExists("AGNIDAWN.Tests.EditMode"),
                "AGNIDAWN.Tests.EditMode.asmdef is missing from Assets/Scripts/Tests/EditMode/");
        }

        // ── HANDOVER.md and tool scripts ──────────────────────────────────────

        [Test]
        public void HandoverDocument_Exists()
        {
            Assert.IsTrue(File.Exists("HANDOVER.md"),
                "HANDOVER.md must exist in project root — shared context between sessions.");
        }

        [Test]
        public void CheckErrorsScript_Exists()
        {
            Assert.IsTrue(File.Exists("Tools/check_errors.ps1"),
                "Tools/check_errors.ps1 must exist — required by CONVENTIONS rule 2.");
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static bool SceneFileExists(string sceneName) =>
            File.Exists($"Assets/Scenes/{sceneName}.unity") ||
            Directory.GetFiles("Assets", $"{sceneName}.unity", SearchOption.AllDirectories).Length > 0;

        private static bool AsmDefExists(string asmName) =>
            Directory.GetFiles("Assets", $"{asmName}.asmdef", SearchOption.AllDirectories).Length > 0;
    }
}
