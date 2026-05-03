using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using AGNIDAWN.Core;

namespace AGNIDAWN.Bootstrap
{
    /// <summary>
    /// Bootstraps the MainMenu scene at runtime with no prefabs.
    /// Creates: GameManager / ObjectPool / TimeManager singletons,
    /// then a full-screen Canvas title screen with Play / Quit buttons.
    ///
    /// Uses legacy UI.Text (no TMP asset dependency).
    /// Phase 15 — Mobile Bootstrap
    /// </summary>
    public static class MainMenuBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            // Subscribe to all future scene loads (RuntimeInitializeOnLoadMethod fires once).
            SceneManager.sceneLoaded += OnSceneLoaded;

            // Handle startup case where MainMenu is the first scene.
            if (SceneManager.GetActiveScene().name == "MainMenu")
                SetupScene();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "MainMenu") return;
            SetupScene();
        }

        private static void SetupScene()
        {
            EnsureSingletons();
            BuildTitleScreen();

            Debug.Log("[MainMenuBootstrap] Title screen built.");
        }

        // ──────────────────────────────────────────────────────────────────────
        #region Singletons

        private static void EnsureSingletons()
        {
            if (GameManager.Instance == null)
            {
                var gm = new GameObject("GameManager");
                gm.AddComponent<GameManager>();
                Object.DontDestroyOnLoad(gm);
            }

            if (ObjectPool.Instance == null)
            {
                var op = new GameObject("ObjectPool");
                op.AddComponent<ObjectPool>();
                Object.DontDestroyOnLoad(op);
            }

            if (TimeManager.Instance == null)
            {
                var tm = new GameObject("TimeManager");
                tm.AddComponent<TimeManager>();
                Object.DontDestroyOnLoad(tm);
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────────
        #region Title Screen

        private static void BuildTitleScreen()
        {
            // ── Camera ─────────────────────────────────────────────────────
            if (Camera.main == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags      = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.05f, 0.03f, 0.02f, 1f);
                cam.orthographic    = true;
            }

            // ── EventSystem ────────────────────────────────────────────────
            EnsureEventSystem();

            // ── Canvas ─────────────────────────────────────────────────────
            var canvasGo = new GameObject("MenuCanvas");
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight  = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // ── Background ─────────────────────────────────────────────────
            MakePanel(canvasGo.transform, "Background",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0.08f, 0.04f, 0.02f, 1f));

            // ── Title ──────────────────────────────────────────────────────
            var titleGo  = new GameObject("TitleText");
            titleGo.transform.SetParent(canvasGo.transform, false);
            var titleTxt = titleGo.AddComponent<Text>();
            titleTxt.text      = "AGNIDAWN";
            titleTxt.font      = GetFont();
            titleTxt.fontSize  = 96;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color     = new Color(1f, 0.75f, 0.1f, 1f);
            titleTxt.resizeTextForBestFit = false;
            var titleRect = titleTxt.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 0.6f);
            titleRect.anchorMax = new Vector2(1f, 0.8f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            // ── Subtitle ───────────────────────────────────────────────────
            var subGo  = new GameObject("SubtitleText");
            subGo.transform.SetParent(canvasGo.transform, false);
            var subTxt = subGo.AddComponent<Text>();
            subTxt.text      = "Survive the Demon Tide";
            subTxt.font      = GetFont();
            subTxt.fontSize  = 36;
            subTxt.alignment = TextAnchor.MiddleCenter;
            subTxt.color     = new Color(0.9f, 0.6f, 0.2f, 0.8f);
            var subRect = subTxt.rectTransform;
            subRect.anchorMin = new Vector2(0f, 0.52f);
            subRect.anchorMax = new Vector2(1f, 0.62f);
            subRect.offsetMin = Vector2.zero;
            subRect.offsetMax = Vector2.zero;

            // ── Play Button ────────────────────────────────────────────────
            var playBtn = MakeButton(canvasGo.transform, "PlayButton", "PLAY",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-200f, -40f), new Vector2(200f, 40f),
                new Color(0.85f, 0.25f, 0.05f, 1f));
            playBtn.onClick.AddListener(() => GameManager.Instance?.StartGame());

            // ── Quit Button ────────────────────────────────────────────────
            var quitBtn = MakeButton(canvasGo.transform, "QuitButton", "QUIT",
                new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.38f),
                new Vector2(-140f, -30f), new Vector2(140f, 30f),
                new Color(0.25f, 0.25f, 0.25f, 1f));
            quitBtn.onClick.AddListener(() =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });

            // ── Version label ──────────────────────────────────────────────
            var verGo  = new GameObject("VersionText");
            verGo.transform.SetParent(canvasGo.transform, false);
            var verTxt = verGo.AddComponent<Text>();
            verTxt.text      = "v0.15.0";
            verTxt.font      = GetFont();
            verTxt.fontSize  = 22;
            verTxt.alignment = TextAnchor.LowerRight;
            verTxt.color     = new Color(1f, 1f, 1f, 0.3f);
            var verRect = verTxt.rectTransform;
            verRect.anchorMin = new Vector2(0f, 0f);
            verRect.anchorMax = new Vector2(1f, 0.08f);
            verRect.offsetMin = new Vector2(0f, 10f);
            verRect.offsetMax = new Vector2(-20f, 0f);

            // ── Pulse animator on title ────────────────────────────────────
            var pulser = titleGo.AddComponent<TitlePulser>();
            pulser.TargetText = titleTxt;
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────────
        #region Helpers

        // Returns Unity's built-in Arial so text renders in stripped IL2CPP builds.
        private static Font GetFont() =>
            Resources.GetBuiltinResource<Font>("Arial.ttf");

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
                return;
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        private static Image MakePanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var go   = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img  = go.AddComponent<Image>();
            img.color = color;
            var rect = img.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return img;
        }

        private static Button MakeButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color bgColor)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color  = bgColor;
            img.sprite = SpriteFactory.CreateSquare(Color.white, 64, 12f);
            img.type   = Image.Type.Simple;
            var btn = go.AddComponent<Button>();

            var rect = img.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            // Label
            var txtGo = new GameObject("Label");
            txtGo.transform.SetParent(go.transform, false);
            var txt = txtGo.AddComponent<Text>();
            txt.text      = label;
            txt.font      = GetFont();
            txt.fontSize  = 42;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color     = Color.white;
            var tr = txt.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;

            var cb = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            cb.pressedColor     = new Color(0.7f, 0.7f, 0.7f, 1f);
            btn.colors = cb;

            return btn;
        }

        #endregion
    }

    // ──────────────────────────────────────────────────────────────────────────
    /// <summary>Pulses the title text scale so it breathes gently.</summary>
    public class TitlePulser : MonoBehaviour
    {
        public Text TargetText;
        private float  _t;
        private Vector3 _baseScale;

        private void Start()
        {
            if (TargetText != null) _baseScale = TargetText.transform.localScale;
        }

        private void Update()
        {
            if (TargetText == null) return;
            _t += Time.unscaledDeltaTime * 1.2f;
            float s = 1f + Mathf.Sin(_t) * 0.04f;
            TargetText.transform.localScale = _baseScale * s;
        }
    }
}
