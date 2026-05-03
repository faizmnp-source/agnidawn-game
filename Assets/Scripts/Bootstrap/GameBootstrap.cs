using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using AGNIDAWN.Core;
using AGNIDAWN.Player;

namespace AGNIDAWN.Bootstrap
{
    /// <summary>
    /// Bootstraps the Game scene at runtime with zero prefabs or ScriptableObjects.
    /// Builds a fully playable prototype:
    ///   - Camera that follows the player
    ///   - Player (orange circle) with VirtualJoystick + dash button
    ///   - AgniKund (golden pentagon) at the center
    ///   - SimpleEnemySpawner that creates red/purple circles that chase the player
    ///   - HUD: health bar, Agni Kund health bar, timer, kill counter
    ///
    /// All gameplay goes through GameManager's state machine and EventBus
    /// exactly as the production code will — just without art assets.
    ///
    /// Phase 15 — Mobile Bootstrap
    /// </summary>
    public static class GameBootstrap
    {
        // ── Shared scene refs (set during build, used by runtime components) ──
        internal static Transform PlayerTransform;
        internal static AgniKundMini AgniKund;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            // RuntimeInitializeOnLoadMethod fires only once at startup.
            // Subscribe to sceneLoaded so we catch every future Game scene load.
            SceneManager.sceneLoaded += OnSceneLoaded;

            // Handle the edge-case where Game is the very first scene.
            if (SceneManager.GetActiveScene().name == "Game")
                SetupScene();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Game") return;
            SetupScene();
        }

        private static void SetupScene()
        {
            EnsureSingletons();
            BuildCamera();
            BuildAgniKund();
            BuildPlayer();
            BuildEnemySpawner();
            BuildHUD();
            BuildPauseOverlay();

            Debug.Log("[GameBootstrap] Game scene ready.");
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
            }
            if (TimeManager.Instance == null)
            {
                var tm = new GameObject("TimeManager");
                tm.AddComponent<TimeManager>();
            }
            if (QualityManager.Instance == null)
            {
                var qm = new GameObject("QualityManager");
                qm.AddComponent<QualityManager>();
                Object.DontDestroyOnLoad(qm);
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────────
        #region Camera

        private static void BuildCamera()
        {
            Camera cam;
            if (Camera.main == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
            }
            else
            {
                cam = Camera.main;
            }

            cam.clearFlags        = CameraClearFlags.SolidColor;
            cam.backgroundColor   = new Color(0.06f, 0.04f, 0.03f, 1f);
            cam.orthographic      = true;
            cam.orthographicSize  = 9f;
            cam.transform.position = new Vector3(0, 0, -10f);

            cam.gameObject.AddComponent<CameraFollow>();
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────────
        #region Agni Kund

        private static void BuildAgniKund()
        {
            var go = new GameObject("AgniKund");
            go.transform.position = Vector3.zero;
            go.tag = "AgniKund";

            // Main body — large golden circle
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite     = SpriteFactory.CreateCircle(new Color(1f, 0.72f, 0.08f), 96);
            sr.sortingOrder = 0;
            go.transform.localScale = Vector3.one * 1.5f;

            // Ring overlay
            var ringGo = new GameObject("Ring");
            ringGo.transform.SetParent(go.transform, false);
            var ringSr = ringGo.AddComponent<SpriteRenderer>();
            ringSr.sprite = SpriteFactory.CreateRing(new Color(1f, 0.45f, 0.0f), 96, 8f);
            ringSr.sortingOrder = 1;

            // Health & logic
            var kund = go.AddComponent<AgniKundMini>();
            AgniKund = kund;

            // Pulsing animation
            go.AddComponent<AgniKundPulser>();
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────────
        #region Player

        private static void BuildPlayer()
        {
            var go = new GameObject("Player");
            go.tag = "Player";
            go.layer = LayerMask.NameToLayer("Default");
            go.transform.position = new Vector3(0, 3f, 0);

            // Sprite — divine orange/white
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.CreateCircle(new Color(1f, 0.45f, 0.05f), 48);
            sr.sortingOrder = 10;

            // Physics
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Collider
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;

            // Player systems
            var health = go.AddComponent<HealthSystem>();
            var controller = go.AddComponent<PlayerController>();

            // Glow indicator (slightly larger tinted circle behind)
            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(go.transform, false);
            var glowSr = glowGo.AddComponent<SpriteRenderer>();
            glowSr.sprite = SpriteFactory.CreateCircle(new Color(1f, 0.3f, 0f, 0.35f), 64);
            glowSr.sortingOrder = 9;
            glowGo.transform.localScale = Vector3.one * 1.6f;
            glowGo.AddComponent<GlowPulser>();

            // Cache for other systems
            PlayerTransform = go.transform;

            // Wire camera follow
            var camFollow = Object.FindAnyObjectByType<CameraFollow>();
            if (camFollow != null) camFollow.Target = go.transform;

            // Build virtual joystick UI (must come after canvas exists or build inline)
            BuildInputUI(controller);
        }

        private static void BuildInputUI(PlayerController controller)
        {
            // ── EventSystem ────────────────────────────────────────────────
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // ── Input Canvas (separate from HUD so z-order is managed) ─────
            var canvasGo = new GameObject("InputCanvas");
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight  = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // ── Joystick background ────────────────────────────────────────
            var joyBgGo = new GameObject("JoystickBG");
            joyBgGo.transform.SetParent(canvasGo.transform, false);
            var joyBgImg = joyBgGo.AddComponent<Image>();
            joyBgImg.sprite = SpriteFactory.CreateCircle(new Color(1f, 1f, 1f, 0.12f), 64);
            joyBgImg.color  = new Color(1f, 1f, 1f, 0.12f);
            joyBgImg.raycastTarget = true;

            var joyBgRect = joyBgImg.rectTransform;
            joyBgRect.anchorMin = new Vector2(0f, 0f);
            joyBgRect.anchorMax = new Vector2(0f, 0f);
            joyBgRect.pivot     = new Vector2(0.5f, 0.5f);
            joyBgRect.anchoredPosition = new Vector2(200f, 200f);
            joyBgRect.sizeDelta = new Vector2(220f, 220f);

            // ── Joystick handle ────────────────────────────────────────────
            var joyHandleGo = new GameObject("JoystickHandle");
            joyHandleGo.transform.SetParent(joyBgGo.transform, false);
            var joyHandleImg = joyHandleGo.AddComponent<Image>();
            joyHandleImg.sprite = SpriteFactory.CreateCircle(new Color(1f, 0.6f, 0.1f, 0.9f), 48);
            joyHandleImg.color  = new Color(1f, 0.6f, 0.1f, 0.9f);
            joyHandleImg.raycastTarget = false;
            var joyHandleRect = joyHandleImg.rectTransform;
            joyHandleRect.anchoredPosition = Vector2.zero;
            joyHandleRect.sizeDelta        = new Vector2(90f, 90f);

            // ── VirtualJoystick component ──────────────────────────────────
            var joystick = joyBgGo.AddComponent<VirtualJoystick>();
            joystick.Target      = controller;
            joystick.StickHandle = joyHandleRect;
            joystick.MaxRadius   = 60f;

            // ── Dash button ────────────────────────────────────────────────
            var dashGo = new GameObject("DashButton");
            dashGo.transform.SetParent(canvasGo.transform, false);
            var dashImg = dashGo.AddComponent<Image>();
            dashImg.sprite = SpriteFactory.CreateCircle(new Color(0.15f, 0.5f, 1f, 0.85f), 48);
            dashImg.color  = new Color(0.15f, 0.5f, 1f, 0.85f);

            var dashRect = dashImg.rectTransform;
            dashRect.anchorMin = new Vector2(1f, 0f);
            dashRect.anchorMax = new Vector2(1f, 0f);
            dashRect.pivot     = new Vector2(0.5f, 0.5f);
            dashRect.anchoredPosition = new Vector2(-180f, 200f);
            dashRect.sizeDelta        = new Vector2(130f, 130f);

            var dashBtn = dashGo.AddComponent<Button>();
            dashBtn.targetGraphic = dashImg;
            dashBtn.onClick.AddListener(() => controller.TriggerDash());

            // Dash label
            var dashLblGo = new GameObject("Label");
            dashLblGo.transform.SetParent(dashGo.transform, false);
            var dashLbl = dashLblGo.AddComponent<Text>();
            dashLbl.text      = "DASH";
            dashLbl.font      = GetFont();
            dashLbl.fontSize  = 28;
            dashLbl.fontStyle = FontStyle.Bold;
            dashLbl.alignment = TextAnchor.MiddleCenter;
            dashLbl.color     = Color.white;
            dashLbl.raycastTarget = false;
            var dRect = dashLbl.rectTransform;
            dRect.anchorMin = Vector2.zero;
            dRect.anchorMax = Vector2.one;
            dRect.offsetMin = Vector2.zero;
            dRect.offsetMax = Vector2.zero;
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────────
        #region Enemy Spawner

        private static void BuildEnemySpawner()
        {
            var go = new GameObject("SimpleEnemySpawner");
            go.AddComponent<SimpleEnemySpawner>();
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────────
        #region HUD

        private static void BuildHUD()
        {
            var canvasGo = new GameObject("HUDCanvas");
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight  = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Top panel background
            MakeHUDPanel(canvasGo.transform, "TopBar",
                new Vector2(0f, 0.88f), new Vector2(1f, 1f),
                new Color(0f, 0f, 0f, 0.5f));

            // ── Player health bar ──────────────────────────────────────────
            MakeLabel(canvasGo.transform, "HPLabel", "HP",
                new Vector2(0.02f, 0.92f), new Vector2(0.12f, 0.98f), 28, TextAnchor.MiddleLeft);
            var hpBg = MakeBarBG(canvasGo.transform, "HPBarBG",
                new Vector2(0.12f, 0.93f), new Vector2(0.45f, 0.975f),
                new Color(0.2f, 0.05f, 0.05f, 1f));
            var hpFill = MakeBarFill(hpBg.transform, "HPFill", new Color(0.9f, 0.15f, 0.05f, 1f));

            // ── Agni Kund health bar ───────────────────────────────────────
            MakeLabel(canvasGo.transform, "AKLabel", "AK",
                new Vector2(0.02f, 0.89f), new Vector2(0.12f, 0.94f), 26, TextAnchor.MiddleLeft);
            var akBg = MakeBarBG(canvasGo.transform, "AgniBarBG",
                new Vector2(0.12f, 0.895f), new Vector2(0.45f, 0.935f),
                new Color(0.15f, 0.08f, 0.02f, 1f));
            var akFill = MakeBarFill(akBg.transform, "AgniFill", new Color(1f, 0.6f, 0.05f, 1f));

            // ── Timer (top center) ─────────────────────────────────────────
            var timerLbl = MakeLabel(canvasGo.transform, "TimerLabel", "20:00",
                new Vector2(0.35f, 0.9f), new Vector2(0.65f, 1f), 44, TextAnchor.MiddleCenter);
            timerLbl.color = new Color(1f, 0.85f, 0.3f, 1f);

            // ── Kill counter (top right) ───────────────────────────────────
            MakeLabel(canvasGo.transform, "KillsIcon", "Kills",
                new Vector2(0.78f, 0.92f), new Vector2(0.88f, 0.99f), 22, TextAnchor.MiddleCenter);
            var killsLbl = MakeLabel(canvasGo.transform, "KillsLabel", "0",
                new Vector2(0.86f, 0.92f), new Vector2(0.99f, 0.99f), 38, TextAnchor.MiddleLeft);
            killsLbl.color = Color.white;

            // ── HUD updater ────────────────────────────────────────────────
            var updater = canvasGo.AddComponent<HUDUpdater>();
            updater.HPFill    = hpFill.rectTransform;
            updater.AKFill    = akFill.rectTransform;
            updater.TimerText = timerLbl;
            updater.KillsText = killsLbl;
        }

        private static void BuildPauseOverlay()
        {
            var canvasGo = new GameObject("PauseCanvas");
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var overlay = MakeHUDPanel(canvasGo.transform, "Overlay",
                Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.85f));
            overlay.gameObject.SetActive(false);

            // Pause button (top right corner, small)
            var pauseCanvas2 = new GameObject("PauseButtonCanvas");
            var c2 = pauseCanvas2.AddComponent<Canvas>();
            c2.renderMode   = RenderMode.ScreenSpaceOverlay;
            c2.sortingOrder = 12;
            pauseCanvas2.AddComponent<CanvasScaler>();
            pauseCanvas2.AddComponent<GraphicRaycaster>();

            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            var pauseBtnGo = new GameObject("PauseBtn");
            pauseBtnGo.transform.SetParent(pauseCanvas2.transform, false);
            var pauseImg = pauseBtnGo.AddComponent<Image>();
            pauseImg.color = new Color(1f, 1f, 1f, 0.15f);
            pauseImg.sprite = SpriteFactory.CreateSquare(Color.white, 32, 4f);
            var pauseRect = pauseImg.rectTransform;
            pauseRect.anchorMin = new Vector2(1f, 1f);
            pauseRect.anchorMax = new Vector2(1f, 1f);
            pauseRect.pivot     = new Vector2(1f, 1f);
            pauseRect.anchoredPosition = new Vector2(-20f, -20f);
            pauseRect.sizeDelta        = new Vector2(80f, 60f);

            var pauseBtn = pauseBtnGo.AddComponent<Button>();
            var pauseLbl = new GameObject("Lbl");
            pauseLbl.transform.SetParent(pauseBtnGo.transform, false);
            var pl = pauseLbl.AddComponent<Text>();
            pl.text      = "||";
            pl.font      = GetFont();
            pl.fontSize  = 26;
            pl.alignment = TextAnchor.MiddleCenter;
            pl.raycastTarget = false;
            var plRect = pl.rectTransform;
            plRect.anchorMin = Vector2.zero;
            plRect.anchorMax = Vector2.one;
            plRect.offsetMin = Vector2.zero;
            plRect.offsetMax = Vector2.zero;

            pauseBtn.onClick.AddListener(() =>
            {
                if (GameManager.Instance == null) return;
                if (GameManager.Instance.CurrentState == GameManager.GameState.Playing)
                    GameManager.Instance.PauseGame();
                else if (GameManager.Instance.CurrentState == GameManager.GameState.Paused)
                    GameManager.Instance.ResumeGame();
            });
        }

        // ── HUD element factories ──────────────────────────────────────────────

        private static Image MakeHUDPanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go   = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img  = go.AddComponent<Image>();
            img.color = color;
            var rect = img.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return img;
        }

        // Returns Unity's built-in Arial so text renders in stripped IL2CPP builds.
        private static Font GetFont() =>
            Resources.GetBuiltinResource<Font>("Arial.ttf");

        private static Text MakeLabel(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, float fontSize, TextAnchor align)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(parent, false);
            var lbl = go.AddComponent<Text>();
            lbl.text      = text;
            lbl.font      = GetFont();
            lbl.fontSize  = (int)fontSize;
            lbl.alignment = align;
            lbl.color     = Color.white;
            var rect = lbl.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return lbl;
        }

        private static Image MakeBarBG(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Color bg)
        {
            var go   = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img  = go.AddComponent<Image>();
            img.color = bg;
            var rect = img.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return img;
        }

        private static Image MakeBarFill(Transform parent, string name, Color fillColor)
        {
            var go   = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img  = go.AddComponent<Image>();
            img.color = fillColor;
            img.type  = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillAmount = 1f;
            img.sprite = SpriteFactory.CreateWhitePixel();
            var rect = img.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(2f, 2f);
            rect.offsetMax = new Vector2(-2f, -2f);
            return img;
        }

        #endregion
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Helper MonoBehaviours (placed in Bootstrap namespace, Bootstrap assembly)
    // ══════════════════════════════════════════════════════════════════════════

    // ── Camera follow ──────────────────────────────────────────────────────────
    public class CameraFollow : MonoBehaviour
    {
        public Transform Target;
        [SerializeField] private float smoothTime = 0.15f;
        private Vector3 _vel;

        private void LateUpdate()
        {
            if (Target == null) return;
            var dest = new Vector3(Target.position.x, Target.position.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, dest, ref _vel, smoothTime);
        }
    }

    // ── Agni Kund (simple, no ScriptableObjects) ───────────────────────────────
    public class AgniKundMini : MonoBehaviour
    {
        public float MaxHP  = 500f;
        public float HP     { get; private set; }
        public float HPPct  => HP / MaxHP;

        private void Awake() { HP = MaxHP; }

        public void TakeDamage(float dmg)
        {
            HP = Mathf.Max(0f, HP - dmg);
            EventBus.Emit<float>("OnAgniKundDamaged", dmg);
            if (HP <= 0f) GameManager.Instance?.TriggerGameOver();
        }
    }

    // ── Agni Kund visual pulser ────────────────────────────────────────────────
    public class AgniKundPulser : MonoBehaviour
    {
        private float _t;
        private Vector3 _base;

        private void Start() { _base = transform.localScale; }

        private void Update()
        {
            _t += Time.deltaTime * 1.8f;
            float s = 1f + Mathf.Sin(_t) * 0.06f;
            transform.localScale = _base * s;

            // Colour shift orange↔yellow
            float c = (Mathf.Sin(_t * 0.8f) + 1f) * 0.5f;
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = Color.Lerp(new Color(1f, 0.55f, 0.0f), new Color(1f, 0.85f, 0.1f), c);
        }
    }

    // ── Player glow pulser ─────────────────────────────────────────────────────
    public class GlowPulser : MonoBehaviour
    {
        private float _t;
        private void Update()
        {
            _t += Time.deltaTime * 3f;
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                float a = 0.2f + Mathf.Sin(_t) * 0.12f;
                sr.color = new Color(1f, 0.3f, 0f, a);
            }
        }
    }

    // ── HUD updater ────────────────────────────────────────────────────────────
    public class HUDUpdater : MonoBehaviour
    {
        public RectTransform HPFill;
        public RectTransform AKFill;
        public Text          TimerText;
        public Text          KillsText;

        private HealthSystem  _playerHP;
        private AgniKundMini  _agniHP;
        private Image         _hpImg;
        private Image         _akImg;

        private void Start()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _playerHP = player.GetComponent<HealthSystem>();

            var kund = GameObject.FindGameObjectWithTag("AgniKund");
            if (kund != null) _agniHP = kund.GetComponent<AgniKundMini>();

            if (HPFill != null) _hpImg = HPFill.GetComponent<Image>();
            if (AKFill != null) _akImg = AKFill.GetComponent<Image>();
        }

        private void Update()
        {
            // Player health
            if (_hpImg != null && _playerHP != null)
                _hpImg.fillAmount = _playerHP.HealthPercent;

            // Agni Kund health
            if (_akImg != null && _agniHP != null)
                _akImg.fillAmount = _agniHP.HPPct;

            // Timer countdown
            if (TimerText != null && GameManager.Instance != null)
            {
                float remaining = Mathf.Max(0f, 1200f - GameManager.Instance.ElapsedTime);
                int   m = (int)(remaining / 60f);
                int   s = (int)(remaining % 60f);
                TimerText.text = $"{m:00}:{s:00}";

                // Flash red in last 60s
                TimerText.color = remaining < 60f
                    ? Color.Lerp(Color.red, new Color(1f, 0.85f, 0.3f),
                        Mathf.PingPong(Time.unscaledTime * 2f, 1f))
                    : new Color(1f, 0.85f, 0.3f, 1f);
            }

            // Kills
            if (KillsText != null && GameManager.Instance != null)
                KillsText.text = GameManager.Instance.TotalKills.ToString();
        }
    }

    // ── Simple enemy (no EnemyData SO required) ────────────────────────────────
    public class SimpleEnemy : MonoBehaviour
    {
        public float HP       = 30f;
        public float Speed    = 2.8f;
        public float Damage   = 8f;
        public Color TintColor = Color.red;

        private Transform  _player;
        private Rigidbody2D _rb;
        private SpriteRenderer _sr;
        private float _attackCooldown;
        private bool  _dead;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _sr = GetComponent<SpriteRenderer>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
        }

        private void OnEnable()
        {
            _dead = false;
            HP    = 30f + (GameManager.Instance != null ? GameManager.Instance.ElapsedTime * 0.1f : 0f);
        }

        private void Update()
        {
            if (_dead) return;
            if (GameManager.Instance == null || !GameManager.Instance.IsRunning) { _rb.linearVelocity = Vector2.zero; return; }

            if (_player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) _player = p.transform;
            }

            if (_attackCooldown > 0f) _attackCooldown -= Time.deltaTime;
        }

        private void FixedUpdate()
        {
            if (_dead || _player == null) return;
            if (GameManager.Instance == null || !GameManager.Instance.IsRunning) return;

            Vector2 dir = ((Vector2)(_player.position - transform.position)).normalized;
            _rb.linearVelocity = dir * Speed;

            // Flip
            if (_sr != null && dir.x != 0)
                _sr.flipX = dir.x < 0;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_dead) return;

            // Hit player
            if (other.CompareTag("Player") && _attackCooldown <= 0f)
            {
                var hp = other.GetComponent<HealthSystem>();
                hp?.TakeDamage(Damage);
                _attackCooldown = 0.8f;
            }

            // Hit Agni Kund
            if (other.CompareTag("AgniKund") && _attackCooldown <= 0f)
            {
                GameBootstrap.AgniKund?.TakeDamage(Damage * 2f);
                _attackCooldown = 1.5f;
            }
        }

        public void TakeDamage(float dmg)
        {
            if (_dead) return;
            HP -= dmg;

            // Red flash
            StartCoroutine(HitFlash());

            if (HP <= 0f) Die();
        }

        private System.Collections.IEnumerator HitFlash()
        {
            if (_sr != null) _sr.color = Color.white;
            yield return new WaitForSeconds(0.08f);
            if (_sr != null) _sr.color = TintColor;
        }

        private void Die()
        {
            _dead = true;
            _rb.linearVelocity = Vector2.zero;
            EventBus.Emit<GameObject>("OnEnemyDied", gameObject);
            GameManager.Instance?.RegisterKill();
            gameObject.SetActive(false);
        }
    }

    // ── Simple enemy spawner ───────────────────────────────────────────────────
    public class SimpleEnemySpawner : MonoBehaviour
    {
        private Transform   _player;
        private float       _timer;
        private float       _spawnInterval = 3f;
        private int         _poolSize      = 30;
        private List<GameObject> _pool;

        // Enemy variant colours (Asura, Rakshasa, Naga, Pisacha, Vetala)
        private static readonly Color[] Tints =
        {
            new Color(0.9f, 0.1f, 0.1f),   // Asura — red
            new Color(0.6f, 0.1f, 0.8f),   // Rakshasa — purple
            new Color(0.1f, 0.7f, 0.3f),   // Naga — green
            new Color(0.9f, 0.5f, 0.1f),   // Pisacha — orange
            new Color(0.2f, 0.6f, 0.9f),   // Vetala — blue
        };

        private void Start()
        {
            // Build pool
            _pool = new List<GameObject>(_poolSize);
            for (int i = 0; i < _poolSize; i++)
            {
                var go  = CreateEnemyGO(Tints[i % Tints.Length]);
                go.SetActive(false);
                _pool.Add(go);
            }

            EventBus.On(GameManager.EVT_GAME_START, OnGameStart);
            EventBus.On(GameManager.EVT_GAME_OVER,  OnStop);
            EventBus.On(GameManager.EVT_VICTORY,    OnStop);
        }

        private void OnDestroy()
        {
            EventBus.Off(GameManager.EVT_GAME_START, OnGameStart);
            EventBus.Off(GameManager.EVT_GAME_OVER,  OnStop);
            EventBus.Off(GameManager.EVT_VICTORY,    OnStop);
        }

        private void OnGameStart() { _timer = 1f; } // first spawn 1 s after game starts
        private void OnStop()      { DeactivateAll(); }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsRunning) return;

            if (_player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) _player = p.transform;
            }

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                // Dynamic interval — faster as time progresses
                float t        = Mathf.Clamp01(GameManager.Instance.ElapsedTime / 1200f);
                _spawnInterval = Mathf.Lerp(3f, 0.5f, t);
                _timer         = _spawnInterval;

                int count = Mathf.RoundToInt(Mathf.Lerp(2f, 8f, t));
                for (int i = 0; i < count; i++) SpawnOne();
            }
        }

        private void SpawnOne()
        {
            if (_player == null) return;

            // Find inactive enemy from pool
            GameObject go = null;
            foreach (var e in _pool)
            {
                if (!e.activeSelf) { go = e; break; }
            }
            if (go == null) return; // pool exhausted

            // Position at random angle 10-14 units from player
            float angle  = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = Random.Range(10f, 14f);
            go.transform.position = (Vector2)_player.position
                + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);

            // Scale up speed with elapsed time
            float t = Mathf.Clamp01(GameManager.Instance.ElapsedTime / 1200f);
            var se  = go.GetComponent<SimpleEnemy>();
            if (se != null)
            {
                se.HP    = Mathf.Lerp(20f, 120f, t);
                se.Speed = Mathf.Lerp(2.5f, 5.5f, t);
            }

            go.SetActive(true);
        }

        private void DeactivateAll()
        {
            if (_pool == null) return;
            foreach (var e in _pool) e.SetActive(false);
        }

        private static GameObject CreateEnemyGO(Color tint)
        {
            var go = new GameObject("SimpleEnemy");
            go.tag   = "Enemy";
            go.layer = 8; // Enemy layer (defined in ProjectSettings/TagManager.asset)

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.CreateCircle(tint, 32);
            sr.color  = tint;
            sr.sortingOrder = 5;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius    = 0.5f;
            col.isTrigger = true;

            var se = go.AddComponent<SimpleEnemy>();
            se.TintColor = tint;

            // HP bar (tiny, above enemy)
            var hpGo  = new GameObject("HPBar");
            hpGo.transform.SetParent(go.transform, false);
            hpGo.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            hpGo.transform.localScale    = new Vector3(1.4f, 0.18f, 1f);
            var hpSr  = hpGo.AddComponent<SpriteRenderer>();
            hpSr.sprite      = SpriteFactory.CreateSquare(Color.white, 32, 2f);
            hpSr.color       = new Color(0.15f, 0.9f, 0.15f);
            hpSr.sortingOrder = 6;
            hpGo.AddComponent<EnemyHPBar>().Enemy = se;

            return go;
        }
    }

    // ── Enemy HP bar updater ───────────────────────────────────────────────────
    public class EnemyHPBar : MonoBehaviour
    {
        public SimpleEnemy Enemy;
        private Vector3 _fullScale;
        private float   _maxHP;

        private void Start()
        {
            _fullScale = transform.localScale;
            if (Enemy != null) _maxHP = Enemy.HP;
        }

        private void Update()
        {
            if (Enemy == null || _maxHP <= 0f) return;
            float pct = Enemy.HP / _maxHP;
            transform.localScale = new Vector3(_fullScale.x * pct, _fullScale.y, _fullScale.z);

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = Color.Lerp(Color.red, new Color(0.15f, 0.9f, 0.15f), pct);
        }
    }
}
