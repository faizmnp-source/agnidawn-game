using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System;
using System.IO;

namespace AGNIDAWN.Build
{
    /// <summary>
    /// AndroidBuilder — invoked by Unity batch mode via -executeMethod.
    ///
    /// Called by:
    ///   Tools/build_android.ps1   (local builds)
    ///   .github/workflows/unity-build.yml  (CI Android job uses game-ci/unity-builder
    ///   which calls its own method, but this is available for manual batch builds)
    ///
    /// Usage (batch mode):
    ///   Unity.exe -batchmode -quit -projectPath . -executeMethod AGNIDAWN.Build.AndroidBuilder.Build
    ///             -buildTarget Android -outputPath build/Android/Agnidawn.apk
    ///
    /// Linear: FAI-17
    /// </summary>
    public static class AndroidBuilder
    {
        // ── Entry point called by -executeMethod ────────────────��────────────
        public static void Build()
        {
            var args = ParseCommandLineArgs();

            bool   isRelease    = Array.Exists(args, a => a == "-release");
            string outputPath   = GetArgValue(args, "-outputPath") ?? "build/Android/Agnidawn.apk";
            string keystorePath = GetArgValue(args, "-keystorePath");
            string keystorePass = GetArgValue(args, "-keystorePass");
            string keyaliasName = GetArgValue(args, "-keyaliasName");
            string keyaliasPass = GetArgValue(args, "-keyaliasPass");

            // Ensure output directory exists
            string outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
                Directory.CreateDirectory(outputDir);

            // ── PlayerSettings ───────────���─────────────────────────────────
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion       = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion    = AndroidSdkVersions.AndroidApiLevelAuto;

            // Signing
            if (!string.IsNullOrEmpty(keystorePath))
            {
                PlayerSettings.Android.keystoreName = keystorePath;
                PlayerSettings.Android.keystorePass = keystorePass ?? string.Empty;
                PlayerSettings.Android.keyaliasName = keyaliasName ?? string.Empty;
                PlayerSettings.Android.keyaliasPass = keyaliasPass ?? string.Empty;
                Debug.Log($"[AndroidBuilder] Using keystore: {keystorePath}");
            }
            else
            {
                Debug.Log("[AndroidBuilder] No keystore provided — using debug signing.");
            }

            // ── Build options ───────────────────────────────���──────────────
            var options = new BuildPlayerOptions
            {
                scenes           = GetBuildScenes(),
                locationPathName = outputPath,
                target           = BuildTarget.Android,
                options          = isRelease
                    ? BuildOptions.None
                    : BuildOptions.Development | BuildOptions.AllowDebugging
            };

            Debug.Log($"[AndroidBuilder] Starting Android build → {outputPath}");
            Debug.Log($"[AndroidBuilder] Scenes: {string.Join(", ", options.scenes)}");
            Debug.Log($"[AndroidBuilder] Release: {isRelease}");

            // ── Execute build ──────────────────────────────────────────────
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[AndroidBuilder] ✓ Build succeeded in {summary.totalTime.TotalSeconds:F1}s " +
                          $"({summary.totalSize / 1_000_000}MB) → {outputPath}");
            }
            else
            {
                Debug.LogError($"[AndroidBuilder] ✗ Build FAILED: {summary.result}");
                foreach (var step in report.steps)
                foreach (var msg  in step.messages)
                    if (msg.type == LogType.Error)
                        Debug.LogError($"  {msg.content}");

                EditorApplication.Exit(1);
            }
        }

        // ── PC Standalone convenience build (for local use) ──────────────────
        public static void BuildPC()
        {
            string outputPath = GetArgValue(ParseCommandLineArgs(), "-outputPath")
                             ?? "build/StandaloneWindows64/Agnidawn.exe";

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);

            var options = new BuildPlayerOptions
            {
                scenes           = GetBuildScenes(),
                locationPathName = outputPath,
                target           = BuildTarget.StandaloneWindows64,
                options          = BuildOptions.None
            };

            Debug.Log($"[AndroidBuilder] Starting PC build → {outputPath}");
            var report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("[AndroidBuilder] ✗ PC build FAILED");
                EditorApplication.Exit(1);
            }

            Debug.Log("[AndroidBuilder] ✓ PC build succeeded");
        }

        // ── Helpers ─────────────────────��──────────────────────────────────────

        /// <summary>Collect all scenes that are enabled in Build Settings.</summary>
        private static string[] GetBuildScenes()
        {
            var sceneList = new System.Collections.Generic.List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                    sceneList.Add(scene.path);
            }

            if (sceneList.Count == 0)
            {
                // Fallback: add standard scene paths if none are configured yet
                string[] defaults = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Game.unity" };
                foreach (var d in defaults)
                    if (File.Exists(d)) sceneList.Add(d);
            }

            return sceneList.ToArray();
        }

        private static string[] ParseCommandLineArgs() =>
            Environment.GetCommandLineArgs();

        private static string GetArgValue(string[] args, string key)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == key) return args[i + 1];
            return null;
        }
    }
}
