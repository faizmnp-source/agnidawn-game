using System;
using System.Collections;
using UnityEngine;

namespace AGNIDAWN.Core
{
    /// <summary>
    /// Central game state machine. Singleton — persists across scenes.
    /// Manages transitions: MainMenu → Playing → Paused → GameOver → Victory.
    /// Linear: FAI-6
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────
        public static GameManager Instance { get; private set; }

        // ── State ──────────────────────────────────────────────────────────
        public enum GameState
        {
            MainMenu,
            Loading,
            Playing,
            Paused,
            LevelUp,
            GameOver,
            Victory
        }

        public GameState CurrentState { get; private set; } = GameState.MainMenu;

        // ── Events (via EventBus) ──────────────────────────────────────────
        public static readonly string EVT_GAME_START    = "OnGameStart";
        public static readonly string EVT_GAME_PAUSE    = "OnGamePause";
        public static readonly string EVT_GAME_RESUME   = "OnGameResume";
        public static readonly string EVT_GAME_OVER     = "OnGameOver";
        public static readonly string EVT_VICTORY       = "OnVictory";
        public static readonly string EVT_LEVEL_UP      = "OnLevelUp";
        /// <summary>Fired once per game minute. Arg: current minute (1, 2, 3…).
        /// BossManager listens to this to trigger boss spawns at minutes 5/10/15/20.</summary>
        public static readonly string EVT_MINUTE_PASSED = "OnMinutePassed";

        // ── Runtime stats ─────────────────────────────────────────────────
        public float ElapsedTime  { get; private set; }
        public int   CurrentWave  { get; private set; }
        public int   TotalKills   { get; private set; }
        public int   CurrentLevel { get; private set; } = 1;
        public bool  IsRunning    => CurrentState == GameState.Playing;

        // ── Minute tracking (drives OnMinutePassed) ───────────────────────
        private int _lastMinuteMark;

        // ── Config ────────────────────────────────────────────────────────
        [Header("Session Config")]
        [SerializeField] private float survivalDurationSeconds = 1200f; // 20 minutes
        [SerializeField] private string gameSceneName = "Game";
        [SerializeField] private string menuSceneName = "MainMenu";

        // ──────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (CurrentState != GameState.Playing) return;

            ElapsedTime += Time.deltaTime;

            // Emit OnMinutePassed once per elapsed minute — drives boss spawning
            int currentMinute = (int)(ElapsedTime / 60f);
            if (currentMinute > _lastMinuteMark)
            {
                _lastMinuteMark = currentMinute;
                EventBus.Emit<int>(EVT_MINUTE_PASSED, currentMinute);
                Debug.Log($"[GameManager] Minute {currentMinute} passed.");
            }

            if (ElapsedTime >= survivalDurationSeconds)
                TriggerVictory();
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region State Transitions

        public void StartGame()
        {
            if (CurrentState != GameState.MainMenu && CurrentState != GameState.GameOver) return;

            ElapsedTime     = 0f;
            CurrentWave     = 1;
            TotalKills      = 0;
            CurrentLevel    = 1;
            _lastMinuteMark = 0;
            SetState(GameState.Loading);

            StartCoroutine(LoadGameScene());
        }

        public void PauseGame()
        {
            if (CurrentState != GameState.Playing) return;
            SetState(GameState.Paused);
            TimeManager.Instance?.SetPaused(true);
            EventBus.Emit(EVT_GAME_PAUSE);
        }

        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused) return;
            SetState(GameState.Playing);
            TimeManager.Instance?.SetPaused(false);
            EventBus.Emit(EVT_GAME_RESUME);
        }

        public void TriggerGameOver()
        {
            if (CurrentState == GameState.GameOver) return;
            SetState(GameState.GameOver);
            TimeManager.Instance?.SetTimeScale(0f);
            EventBus.Emit(EVT_GAME_OVER);
        }

        public void TriggerVictory()
        {
            if (CurrentState == GameState.Victory) return;
            SetState(GameState.Victory);
            TimeManager.Instance?.SetTimeScale(0f);
            EventBus.Emit(EVT_VICTORY);
        }

        public void TriggerLevelUp()
        {
            if (CurrentState != GameState.Playing) return;
            SetState(GameState.LevelUp);
            TimeManager.Instance?.SetTimeScale(0f);
            EventBus.Emit(EVT_LEVEL_UP);
        }

        public void ResumefromLevelUp()
        {
            if (CurrentState != GameState.LevelUp) return;
            SetState(GameState.Playing);
            TimeManager.Instance?.SetTimeScale(1f);
        }

        /// <summary>Restart the current run from scratch (wave 1, stats zeroed).</summary>
        public void RestartRun()
        {
            ElapsedTime     = 0f;
            CurrentWave     = 1;
            TotalKills      = 0;
            CurrentLevel    = 1;
            _lastMinuteMark = 0;
            SetState(GameState.Loading);
            StartCoroutine(LoadGameScene());
        }

        /// <summary>Return to the main menu scene (alias used by UI layer).</summary>
        public void ReturnToMainMenu() => ReturnToMenu();

        /// <summary>Called by EnemyBase / KillSystem to register a confirmed kill.</summary>
        public void RegisterKill()
        {
            TotalKills++;
            EventBus.Emit("OnKillRegistered", TotalKills);
        }

        /// <summary>Called by LevelSystem when the player gains a level.</summary>
        public void SetLevel(int level)
        {
            CurrentLevel = level;
        }

        public void IncrementWave()
        {
            CurrentWave++;
            EventBus.Emit("OnWaveChanged", CurrentWave);
        }

        public void ReturnToMenu()
        {
            SetState(GameState.MainMenu);
            TimeManager.Instance?.SetTimeScale(1f);
            UnityEngine.SceneManagement.SceneManager.LoadScene(menuSceneName);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Private Helpers

        private void SetState(GameState newState)
        {
            var prev = CurrentState;
            CurrentState = newState;
            EventBus.Emit("OnGameStateChanged", newState, prev);
            Debug.Log($"[GameManager] {prev} → {newState}");
        }

        private IEnumerator LoadGameScene()
        {
            var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(gameSceneName);
            while (!op.isDone) yield return null;

            SetState(GameState.Playing);
            TimeManager.Instance?.SetTimeScale(1f);
            EventBus.Emit(EVT_GAME_START);
        }

        #endregion
    }
}
