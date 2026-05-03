using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using AGNIDAWN.Core;

namespace AGNIDAWN.Bootstrap
{
    /// <summary>
    /// Bootstraps the MainMenu scene at runtime with no prefabs.
    /// Creates: GameManager / ObjectPool / TimeManager singletons,
    /// then a full-screen Canvas title screen with Play / Quit buttons.
    ///
    /// Phase 15 — Mobile Bootstrap
    /// </summary>
    public static class MainMenuBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            if (SceneManager.GetActiveScene().name != "MainMenu") return;

            EnsureSingletons();
            BuildTitleScreen();

            Debug.Log("[MainMenuBootstrap] Title screen built.");
        }

        // ──────────────────────────────────────────────────────────────────────
        #region Singletons

        private static void EnsureSingletons()
        {
            // GameManager — DontDestroyOnLoad, persists to Game scene
            if (GameManager.Instance == null)
            {
                var gm = new GameObject("GameManager");
                gm.AddComponent<GameManager>();
                Object.DontDestroyOnLoad(gm);
            }

            // ObjectPool
            if (ObjectPool.Instance == null)
            {
                var op = new GameObject("ObjectPool");
                op.AddComponent<ObjectPool>();
                Object.DontDestroyOnLoad(op);
            }

            // TimeManager
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
                cam.backgroundColor = new Color(0.05f, 0.03f, 0.02f, 1f); // near-black deep red
                cam.orthographic    = true;
            }

            // ── EventSystem ────────────────────────────────────────────────
            EnsureEventSystem();

            // ── Canvas ─────────────────────────────────────────────────────
            var canvasGo = new GameObject("MenuCanvas");
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight  = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // ── Background gradient panel ──────────────────────────────────
            var bg = MakePanel(canvasGo.transform, "Background",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0.08f, 0.04f, 0.02f, 1f));

            // ── Title: AGNIDAWN ────────────────────────────────────────────
            var titleGo = new GameObject("TitleText");
            titleGo.transform.SetParent(canvasGo.transform, false);
            var title = titleGo.AddComponent<TextMeshProUGUI>();
            title.text      = "AGNIDAWN";
            title.fontSize  = 96;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color     = new Color(1f, 0.75f, 0.1f, 1f); // divine gold

            var titleRect = title.rectTransform;
            titleRect.anchorMin  = new Vector2(0f, 0.6f);
            titleRect.anchorMax  = new Vector2(1f, 0.8f);
            titleRect.offsetMin  = Vector2.zero;
            titleRect.offsetMax  = Vector2.zero;

            // ── Subtitle (Devanagari) ──────────────────────────────────────
            var subGo = new GameObject("SubtitleText");
            subGo.transform.SetParent(canvasGo.transform, false);
            var sub = subGo.AddComponent<TextMeshProUGUI>();
            sub.text      = "Survive the Demon Tide";
            sub.fontSize  = 36;
            sub.alignment = TextAlignmentOptions.Center;
            sub.color     = new Color(0.9f, 0.6f, 0.2f, 0.8f);

            var subRect = sub.rectTransform;
            subRect.anchorMin = new Vector2(0f, 0.52f);
            subRect.anchorMax = new Vector2(1f, 0.62f);
            subRect.offsetMin = Vector2.zero;
            subRect.offsetMax = Vector2.zero;

            // ── Play Button ────────────────────────────────────────────────
            var playBtn = MakeButton(canvasGo.transform, "PlayButton", "▶  PLAY",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-200f, -40f), new Vector2(200f, 40f),
                new Color(0.85f, 0.25f, 0.05f, 1f));
            playBtn.onClick.AddListener(() => GameManager.Instance?.StartGame());

            // ── Quit Button ────────────────────────────────────────────────
            var quitBtn = MakeButton(canvasGo.transform, "QuitButton", "QUIT",
                new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.38f),
                new Vector2(-140f, -30f), new Vector2(140f, 30f),
                new Color(0.15f, 0.15f, 0.15f, 1f));
            quitBtn.onClick.AddListener(() =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });

            // ── Version label ──────────────────────────────────────────────
            var verGo = new GameObject("VersionText");
            verGo.transform.SetParent(canvasGo.transform, false);
            var ver = verGo.AddComponent<TextMeshProUGUI>();
            ver.text      = "Phase 15 Build — v0.15.0";
            ver.fontSize  = 22;
            ver.alignment = TextAlignmentOptions.BottomRight;
            ver.color     = new Color(1f, 1f, 1f, 0.3f);
            var verRect = ver.rectTransform;
            verRect.anchorMin = new Vector2(0f, 0f);
            verRect.anchorMax = new Vector2(1f, 0.08f);
            verRect.offsetMin = new Vector2(0f, 10f);
            verRect.offsetMax = new Vector2(-20f, 0f);

            // ── Pulse animator on title ────────────────────────────────────
            var pulser = titleGo.AddComponent<TitlePulser>();
            pulser.TargetText = title;
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────────
        #region Helpers

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
            Vector2 sizeDeltaMin, Vector2 sizeDeltaMax, Color color)
        {
            var go    = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img   = go.AddComponent<Image>();
            img.color = color;
            var rect  = img.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = sizeDeltaMin;
            rect.offsetMax = sizeDeltaMax;
            return img;
        }

        private static Button MakeButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color bgColor)
        {
            var go   = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img  = go.AddComponent<Image>();
            img.color = bgColor;
            img.sprite = SpriteFactory.CreateSquare(Color.white, 64, 12f);
            img.type   = Image.Type.Simple;
            var btn  = go.AddComponent<Button>();

            var rect = img.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            // Button label
            var txtGo = new GameObject("Label");
            txtGo.transform.SetParent(go.transform, false);
            var txt = txtGo.AddComponent<TextMeshProUGUI>();
            txt.text      = label;
            txt.fontSize  = 42;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color     = Color.white;
            var tr = txt.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;

            // Color block
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
        public TextMeshProUGUI TargetText;
        private float _t;
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
