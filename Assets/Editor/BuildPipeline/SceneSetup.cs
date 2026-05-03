using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using System.IO;

namespace AGNIDAWN.Build
{
    /// <summary>
    /// Creates the MainMenu and Game scenes if they don't exist, wires them into
    /// Build Settings, and switches the active platform to Android.
    ///
    /// Run via batch mode: -executeMethod AGNIDAWN.Build.SceneSetup.CreateAndConfigure
    /// Linear: FAI-19
    /// </summary>
    public static class SceneSetup
    {
        public static void CreateAndConfigure()
        {
            Directory.CreateDirectory("Assets/Scenes");

            EnsureScene("Assets/Scenes/MainMenu.unity", CreateMainMenuScene);
            EnsureScene("Assets/Scenes/Game.unity",     CreateGameScene);

            ConfigureBuildSettings();
            SwitchToAndroid();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SceneSetup] Done — MainMenu + Game scenes ready, platform = Android.");
        }

        // ── Scene creation ─────────────────────────────────────────────────────

        private static void EnsureScene(string path, System.Action<string> create)
        {
            if (!File.Exists(path))
            {
                create(path);
                Debug.Log($"[SceneSetup] Created {path}");
            }
            else
            {
                Debug.Log($"[SceneSetup] Already exists: {path}");
            }
        }

        private static void CreateMainMenuScene(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Camera
            var camGo = GameObject.Find("Main Camera") ?? new GameObject("Main Camera");
            var cam   = camGo.GetOrAddComponent<Camera>();
            cam.clearFlags       = CameraClearFlags.SolidColor;
            cam.backgroundColor  = new Color(0.05f, 0.03f, 0.01f); // deep night
            cam.orthographic     = false;
            cam.fieldOfView      = 60f;
            camGo.tag            = "MainCamera";

            // Canvas + EventSystem for UI
            var canvasGo = new GameObject("Canvas");
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // Placeholder title text
            var titleGo   = new GameObject("TitleText");
            titleGo.transform.SetParent(canvasGo.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin        = new Vector2(0.5f, 0.6f);
            titleRect.anchorMax        = new Vector2(0.5f, 0.6f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta        = new Vector2(600f, 120f);
            var titleText              = titleGo.AddComponent<UnityEngine.UI.Text>();
            titleText.text             = "AGNIDAWN";
            titleText.alignment        = TextAnchor.MiddleCenter;
            titleText.fontSize         = 72;
            titleText.color            = new Color(1f, 0.6f, 0.1f);

            EditorSceneManager.SaveScene(scene, path);
        }

        private static void CreateGameScene(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Camera (2D orthographic for the roguelite arena)
            var camGo = GameObject.Find("Main Camera") ?? new GameObject("Main Camera");
            var cam   = camGo.GetOrAddComponent<Camera>();
            cam.clearFlags       = CameraClearFlags.SolidColor;
            cam.backgroundColor  = new Color(0.03f, 0.02f, 0.05f); // midnight purple
            cam.orthographic     = true;
            cam.orthographicSize = 6f;
            camGo.tag            = "MainCamera";

            // Placeholder arena floor
            var floor      = GameObject.CreatePrimitive(PrimitiveType.Quad);
            floor.name     = "ArenaFloor";
            floor.transform.localScale    = new Vector3(20f, 20f, 1f);
            floor.transform.localRotation = Quaternion.Euler(0, 0, 0);

            // Canvas + EventSystem for HUD
            var canvasGo = new GameObject("HUD_Canvas");
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            EditorSceneManager.SaveScene(scene, path);
        }

        // ── Build Settings ─────────────────────────────────────────────────────

        private static void ConfigureBuildSettings()
        {
            var scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Game.unity",     true),
            };
            EditorBuildSettings.scenes = scenes;
            Debug.Log("[SceneSetup] Build Settings configured with 2 scenes.");
        }

        private static void SwitchToAndroid()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Android, BuildTarget.Android);
                Debug.Log("[SceneSetup] Switched active build target to Android.");
            }
            else
            {
                Debug.Log("[SceneSetup] Already targeting Android.");
            }
        }
    }

    internal static class ComponentExtensions
    {
        internal static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }
    }
}
