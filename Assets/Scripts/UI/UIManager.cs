using System.Collections.Generic;
using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.UI
{
    /// <summary>
    /// UIManager — Singleton UI orchestrator for AGNIDAWN.
    ///
    /// Owns the screen stack. Listens to GameManager state changes
    /// and routes to the correct panel. No game system talks to panels
    /// directly — all routing goes through here.
    ///
    /// Screen stack:
    ///   Push(panel) → show panel, keep previous visible underneath
    ///   Pop()       → hide top panel, reveal previous
    ///   Switch(panel) → hide all, show only target (hard cut)
    ///
    /// Linear: FAI-14
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────
        public static UIManager Instance { get; private set; }

        // ── Panel References ───────────────────────────────────────────────
        [Header("Panels")]
        [SerializeField] private MainMenuUI         mainMenuPanel;
        [SerializeField] private HUDController      hudPanel;
        [SerializeField] private LevelUpUI          levelUpPanel;
        [SerializeField] private BossIntroOverlayUI bossIntroPanel;
        [SerializeField] private BossHealthBarUI    bossHealthBarPanel;
        [SerializeField] private PauseMenuUI        pauseMenuPanel;
        [SerializeField] private DeathScreenUI      deathScreenPanel;
        [SerializeField] private VictoryScreenUI    victoryScreenPanel;

        // ── Screen Stack ───────────────────────────────────────────────────
        private Stack<BaseUIPanel> _stack = new();

        // ──────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            EventBus.On(GameManager.EVT_GAME_START,  OnGameStart);
            EventBus.On(GameManager.EVT_GAME_OVER,   OnGameOver);
            EventBus.On(GameManager.EVT_VICTORY,     OnVictory);
            EventBus.On(GameManager.EVT_GAME_PAUSE,  OnGamePause);
            EventBus.On(GameManager.EVT_GAME_RESUME, OnGameResume);
            EventBus.On(GameManager.EVT_LEVEL_UP,    OnLevelUp);

            EventBus.On<BaseBoss>("OnBossHealthBarShow", OnBossHealthBarShow);
            EventBus.On("OnBossHealthBarHide",           OnBossHealthBarHide);
            EventBus.On<string>("OnBossSpawned",         OnBossSpawned);
        }

        private void OnDisable()
        {
            EventBus.Off(GameManager.EVT_GAME_START,  OnGameStart);
            EventBus.Off(GameManager.EVT_GAME_OVER,   OnGameOver);
            EventBus.Off(GameManager.EVT_VICTORY,     OnVictory);
            EventBus.Off(GameManager.EVT_GAME_PAUSE,  OnGamePause);
            EventBus.Off(GameManager.EVT_GAME_RESUME, OnGameResume);
            EventBus.Off(GameManager.EVT_LEVEL_UP,    OnLevelUp);

            EventBus.Off<BaseBoss>("OnBossHealthBarShow", OnBossHealthBarShow);
            EventBus.Off("OnBossHealthBarHide",           OnBossHealthBarHide);
            EventBus.Off<string>("OnBossSpawned",         OnBossSpawned);
        }

        private void Start()
        {
            // Boot into main menu
            ShowMainMenu();
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Game Event Handlers

        private void OnGameStart()
        {
            HideAll();
            hudPanel?.Show();
            _stack.Clear();
            _stack.Push(hudPanel);
        }

        private void OnGameOver()
        {
            HideAll();
            deathScreenPanel?.Show();
        }

        private void OnVictory()
        {
            HideAll();
            victoryScreenPanel?.Show();
        }

        private void OnGamePause()
        {
            Push(pauseMenuPanel);
        }

        private void OnGameResume()
        {
            if (_stack.Count > 0 && _stack.Peek() == pauseMenuPanel)
                Pop();
        }

        private void OnLevelUp()
        {
            Push(levelUpPanel);
        }

        private void OnBossSpawned(string bossId)
        {
            bossIntroPanel?.ShowForBoss(bossId);
        }

        private void OnBossHealthBarShow(BaseBoss boss)
        {
            bossHealthBarPanel?.SetBoss(boss);
            bossHealthBarPanel?.Show();
        }

        private void OnBossHealthBarHide()
        {
            bossHealthBarPanel?.Hide();
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Public Navigation API

        public void ShowMainMenu()
        {
            HideAll();
            mainMenuPanel?.Show();
            _stack.Clear();
            _stack.Push(mainMenuPanel);
        }

        public void Push(BaseUIPanel panel)
        {
            if (panel == null) return;
            if (_stack.Count > 0) _stack.Peek().canvasGroup.interactable = false;
            _stack.Push(panel);
            panel.Show();
        }

        public void Pop()
        {
            if (_stack.Count == 0) return;
            _stack.Pop().Hide();
            if (_stack.Count > 0) _stack.Peek().canvasGroup.interactable = true;
        }

        public void HideAll()
        {
            mainMenuPanel?.HideImmediate();
            hudPanel?.HideImmediate();
            levelUpPanel?.HideImmediate();
            bossIntroPanel?.HideImmediate();
            bossHealthBarPanel?.HideImmediate();
            pauseMenuPanel?.HideImmediate();
            deathScreenPanel?.HideImmediate();
            victoryScreenPanel?.HideImmediate();
            _stack.Clear();
        }

        #endregion
    }
}
