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

        // ── Runtime stats ─────────────────────────────────────────────────
        public float ElapsedTime  { get; private set; }
        public int   CurrentWave  { get; private set; }
        public bool  IsRunning    => CurrentState == GameState.Playing;

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

            if (ElapsedTime >= survivalDurationSeconds)
                TriggerVictory();
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region State Transitions

        public void StartGame()
        {
            if (CurrentState != GameState.MainMenu && CurrentState != GameState.GameOver) return;

            ElapsedTime  = 0f;
            CurrentWave  = 1;
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
