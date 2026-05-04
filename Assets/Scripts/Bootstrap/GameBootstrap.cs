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
        internal static Transform    PlayerTransform;
        internal static AgniKundMini AgniKund;
        private  static CameraFollow _cameraFollow;

        // ── Ground / side-view layout constants ───────────────────────────────
        // GROUND_PLATFORM_Y is the world-Y of the TOP SURFACE of the ground collider.
        // Adjust this if Agni floats above or sinks into the visible stone floor.
        // With orthoSize=6 the viewport runs from y=-6 to y=+6.
        // Default -3.5 = roughly 80 % down the arena, matching a typical front-stage floor.
        private const float GROUND_PLATFORM_Y  = -3.5f;
        // AgniRiggedCharacter origin == feet.  Player root spawns AT ground surface.
        // Tiny +0.10 gap lets physics settle without tunnelling on first frame.
        private const float PLAYER_SPAWN_Y     = GROUND_PLATFORM_Y + 0.10f;
        // Shadow sits just above ground surface (avoids z-fight with collider).
        private const float SHADOW_Y           = GROUND_PLATFORM_Y + 0.04f;

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
            BuildArena();
            BuildGroundPlatform();   // ← must be before BuildPlayer so collider exists on first frame
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
            cam.backgroundColor   = new Color(0.04f, 0.02f, 0.02f, 1f);
            cam.orthographic      = true;
            cam.orthographicSize  = 6f;
            cam.transform.position = new Vector3(0f, 0f, -10f);

            var follow = cam.gameObject.AddComponent<CameraFollow>();
            // Store ref so BuildPlayer can wire the target directly
            _cameraFollow = follow;
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────────
        #region Arena Background

        private static void BuildArena()
        {
            // Try Arena1 first (user-supplied), fall back to Arena if not found.
            var arenaSprite = Resources.Load<Sprite>("Arena1")
                           ?? Resources.Load<Sprite>("Arena");
            if (arenaSprite == null)
            {
                Debug.LogWarning("[GameBootstrap] Arena1.png / Arena.png not found in Resources — using solid background.");
                return;
            }
            Debug.Log($"[GameBootstrap] Arena image loaded: {arenaSprite.name}");

            var go = new GameObject("ArenaBackground");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = arenaSprite;
            sr.sortingOrder = -100;

            // Parent to camera so the background always fills the screen
            var cam = Camera.main ?? Object.FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                go.transform.SetParent(cam.transform, false);
                go.transform.localPosition = new Vector3(0f, 0f, 11f); // world z≈1, in front of camera
            }
            else
            {
                go.transform.position = new Vector3(0f, 0f, 0f);
            }

            // Defer scale to first frame — Screen.width/height are not reliable at bootstrap time on Android
            go.AddComponent<ArenaAutoScale>();
            Debug.Log("[GameBootstrap] Arena sprite loaded — scale deferred to first frame.");
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────────
        #region Agni Kund

        private static void BuildAgniKund()
        {
            var go = new GameObject("AgniKund");
            go.transform.position = Vector3.zero;
            go.tag = "AgniKund";

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 2;

            // Try to use the painted Agni Kund sprite
            var kundSprite = Resources.Load<Sprite>("AgniKund");
            if (kundSprite != null)
            {
                Debug.Log("[GameBootstrap] AgniKund sprite loaded OK");
                sr.sprite = kundSprite;
                sr.color  = Color.white;
                go.transform.localScale = Vector3.one;
            }
            else
            {
                Debug.LogWarning("[GameBootstrap] AgniKund.png not found in Resources — using procedural circle fallback");
                // Fallback: procedural golden circle
                sr.sprite = SpriteFactory.CreateCircle(new Color(1f, 0.72f, 0.08f), 96);
                go.transform.localScale = Vector3.one * 0.4f;  // was 1.5f — caused massive on-screen circle

                var ringGo = new GameObject("Ring");
                ringGo.transform.SetParent(go.transform, false);
                var ringSr = ringGo.AddComponent<SpriteRenderer>();
                ringSr.sprite = SpriteFactory.CreateRing(new Color(1f, 0.45f, 0.0f), 96, 8f);
                ringSr.sortingOrder = 3;
            }

            // Health & logic
            var kund = go.AddComponent<AgniKundMini>();
            AgniKund = kund;

            // Pulsing animation
            go.AddComponent<AgniKundPulser>();

            // Physics: so enemies can hit it
            var col = go.AddComponent<CircleCollider2D>();
            col.radius    = 0.8f;
            col.isTrigger = true;
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────────
        #region Player

        private static void BuildPlayer()
        {
            var go = new GameObject("Player");
            go.tag   = "Player";
            go.layer = LayerMask.NameToLayer("Default");
            // Spawn with feet flush on the ground platform surface.
            // Origin = feet. PLAYER_SPAWN_Y = GROUND_PLATFORM_Y + 0.10 (tiny gap for physics settle).
            go.transform.position = new Vector3(0f, PLAYER_SPAWN_Y, 0f);

            // Sprite root — rigged 20-part Agni character.
            // The flat SpriteRenderer on the root GO is kept but hidden;
            // AgniRiggedCharacter creates all part sprites as child GameObjects.
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite  = null;
            sr.enabled = false;

            var rigGo = new GameObject("AgniVisual");
            rigGo.transform.SetParent(go.transform, false);
            rigGo.transform.localPosition = Vector3.zero;
            rigGo.AddComponent<AgniRiggedCharacter>();

            // ── Physics ────────────────────────────────────────────────────
            // gravityScale is set by PlayerController.Awake (serialized field default = 3).
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale           = 3f;    // side-view gravity
            rb.freezeRotation         = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            // Prevent jitter: keep linear drag low (gravity does the braking)
            rb.linearDamping          = 0f;

            // ── Collider: CapsuleCollider2D origin=feet ────────────────────
            // AgniRiggedCharacter.HIP_Y=3.77 places boot bottoms at AgniVisual Y=0.
            // AgniVisual is at Player localPos (0,0), so boots == Player origin.
            // Capsule: offset=(0, 0.80) → bottom=0 (feet), top=1.60 (upper chest).
            var cap = go.AddComponent<CapsuleCollider2D>();
            cap.direction  = CapsuleDirection2D.Vertical;
            cap.size       = new Vector2(0.45f, 1.55f);
            cap.offset     = new Vector2(0f, 0.775f);    // bottom = 0.775-0.775 = 0  (feet)
            var noFriction = new PhysicsMaterial2D("AgniNoFriction");
            noFriction.friction   = 0f;
            noFriction.bounciness = 0f;
            cap.sharedMaterial    = noFriction;

            // ── GroundCheck child GO (position = feet = Player origin) ─────
            var gcGo = new GameObject("GroundCheck");
            gcGo.transform.SetParent(go.transform, false);
            gcGo.transform.localPosition = new Vector3(0f, -0.05f, 0f);

            // ── Player systems ──────────────────────────────────────────────
            var health     = go.AddComponent<HealthSystem>();
            var controller = go.AddComponent<PlayerController>();

            // ── Cache for other systems ─────────────────────────────────────
            PlayerTransform = go.transform;

            // ── Wire camera to follow Agni (X+Y) ───────────────────────────
            if (_cameraFollow != null) _cameraFollow.Target = go.transform;

            // ── Shadow ─────────────────────────────────────────────────────
            BuildPlayerShadow(go.transform);

            // ── Virtual joystick + dash button ─────────────────────────────
            BuildInputUI(controller);
        }

        // ── Arena_GroundCollider ──────────────────────────────────────────────
        private static void BuildGroundPlatform()
        {
            var go = new GameObject("Arena_GroundCollider");
            go.layer = LayerMask.NameToLayer("Default");   // same layer for OverlapCircle
            // Centre the collider at GROUND_PLATFORM_Y.
            // The top surface = GROUND_PLATFORM_Y + (height/2) = GROUND_PLATFORM_Y + 0.05.
            go.transform.position = new Vector3(0f, GROUND_PLATFORM_Y, 0f);

            var box        = go.AddComponent<BoxCollider2D>();
            box.size       = new Vector2(30f, 0.10f);   // 30 u wide — covers full arena + margins
            box.offset     = Vector2.zero;

            // Frictionless so Agni doesn't snag on invisible edges
            var mat        = new PhysicsMaterial2D("GroundNoFriction");
            mat.friction   = 0f;
            mat.bounciness = 0f;
            box.sharedMaterial = mat;

            // Prevent enemies (gravityScale=0, top-down) from being blocked by this collider.
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
                Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Default"), enemyLayer, true);

            Debug.Log($"[GameBootstrap] Arena_GroundCollider created at Y={GROUND_PLATFORM_Y:F2}  top={GROUND_PLATFORM_Y + 0.05f:F2}");
        }

        // ── PlayerShadow ─────────────────────────────────────────────────────
        private static void BuildPlayerShadow(Transform playerTransform)
        {
            var go = new GameObject("PlayerShadow");

            // Soft dark oval
            var sr         = go.AddComponent<SpriteRenderer>();
            sr.sprite      = SpriteFactory.CreateCircle(new Color(0f, 0f, 0f, MAX_SHADOW_ALPHA), 64);
            sr.color       = new Color(0f, 0f, 0f, MAX_SHADOW_ALPHA);
            sr.sortingOrder = 3;     // above Background (0), above ground layer (~2), below player (10+)

            // Oval shape: wide and flat
            go.transform.localScale = new Vector3(0.80f, 0.18f, 1f);
            // Initial position flush on ground
            go.transform.position   = new Vector3(playerTransform.position.x, SHADOW_Y, 0f);

            var shadow = go.AddComponent<AGNIDAWN.Player.ShadowFollow>();
            shadow.Init(playerTransform, SHADOW_Y);

            Debug.Log($"[GameBootstrap] PlayerShadow created at Y={SHADOW_Y:F2}");
        }

        private const float MAX_SHADOW_ALPHA = 0.32f;

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
            pauseImg.color = new Color(0f, 0f, 0f, 0.45f);  // dark semi-transparent, no white box
            pauseImg.sprite = null;
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

        // Enemy variant colours — kept for HP bar tinting
        private static readonly Color[] Tints =
        {
            new Color(0.9f, 0.1f, 0.1f),   // Asura — red
            new Color(0.6f, 0.1f, 0.8f),   // Rakshasa — purple
            new Color(0.1f, 0.7f, 0.3f),   // Naga — green
            new Color(0.9f, 0.5f, 0.1f),   // Pisacha — orange
            new Color(0.2f, 0.6f, 0.9f),   // Vetala — blue
        };

        // Character art names matching Tints order
        private static readonly string[] EnemyChars =
        {
            "Asura", "Rakshasa", "Naga", "Pisacha", "Vetala",
        };

        private void Start()
        {
            // Build pool
            _pool = new List<GameObject>(_poolSize);
            for (int i = 0; i < _poolSize; i++)
            {
                var go  = CreateEnemyGO(Tints[i % Tints.Length], EnemyChars[i % EnemyChars.Length]);
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

        private static GameObject CreateEnemyGO(Color tint, string charName = "Asura")
        {
            var go = new GameObject("SimpleEnemy");
            go.tag   = "Enemy";
            var _eLayer = LayerMask.NameToLayer("Enemy");
            go.layer = (_eLayer >= 0 && _eLayer <= 31) ? _eLayer : 0;

            // Place offscreen at start so inactive pool members are invisible
            go.transform.position = new Vector3(-9999f, -9999f, 0f);

            // Painted character sprite — fallback to tinted circle if load fails
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;
            var enemyAnim = CharacterSpriteFactory.Setup(go, charName, sortingOrder: 5);
            if (enemyAnim == null)
            {
                sr.sprite = SpriteFactory.CreateCircle(tint, 32);
                sr.color  = tint;
            }

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius    = 0.5f;
            col.isTrigger = true;

            var se = go.AddComponent<SimpleEnemy>();
            se.TintColor = tint;

            // NOTE: no debug HP bars — removed per visual requirements.
            // HP feedback comes from the HUD bar, not per-enemy rectangles.

            return go;
        }
    }

    // ── Arena background auto-scaler (runs on first frame so Screen dims are valid) ──────
    public class ArenaAutoScale : MonoBehaviour
    {
        private void Start()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) { Destroy(this); return; }

            // Prefer cam.aspect since it uses the actual render target size
            var cam = GetComponentInParent<Camera>() ?? Camera.main ?? Object.FindAnyObjectByType<Camera>();
            float camH   = cam != null ? cam.orthographicSize * 2f : 12f;
            float aspect = cam != null && cam.aspect > 0f
                           ? cam.aspect
                           : (Screen.height > 0 ? (float)Screen.width / Screen.height : 0.56f);
            float camW   = camH * aspect;
            float sw     = sr.sprite.bounds.size.x;
            float sh     = sr.sprite.bounds.size.y;
            if (sw <= 0f || sh <= 0f) { Destroy(this); return; }
            float scale  = Mathf.Max(camW / sw, camH / sh) * 1.02f;
            transform.localScale = new Vector3(scale, scale, 1f);
            Debug.Log($"[ArenaAutoScale] scale={scale:F3}  cam={camW:F2}x{camH:F2}  sprite={sw:F2}x{sh:F2}");
            Destroy(this); // one-shot
        }
    }

    // __ Enemy HP bar updater
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
