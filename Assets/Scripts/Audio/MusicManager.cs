using System.Collections;
using UnityEngine;
using AGNIDAWN.Core;
using AGNIDAWN.Gameplay;

namespace AGNIDAWN.Audio
{
    /// <summary>
    /// Manages music transitions in AGNIDAWN.
    ///
    /// Listens to:
    ///   OnBiomeEntered   → transition to biome-appropriate music
    ///   OnBiomeExited    → begin fadeout before next biome
    ///   EVT_GAME_START   → start main gameplay loop track
    ///   EVT_GAME_OVER    → play defeat sting and stop music
    ///   EVT_VICTORY      → play victory fanfare
    ///   OnBossSpawned    → intensify music (FMOD parameter "BossActive" = 1)
    ///   OnBossDied       → return to biome ambient (BossActive = 0)
    ///
    /// FMOD approach:
    ///   A single adaptive music event runs continuously.
    ///   Parameters "BiomeIndex" (0–4) and "BossActive" (0/1) drive
    ///   multi-layer transitions inside FMOD Studio.
    ///
    /// Unity fallback approach:
    ///   Coroutine-based crossfade between AudioClips assigned in
    ///   BiomeMusicTracks and the boss track.
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static MusicManager Instance { get; private set; }

        // ── Inspector ────────────────────────────────────────────────────────────
        [Header("FMOD Adaptive Music Event")]
        [Tooltip("Single adaptive event that handles all in-game music via parameters")]
        [SerializeField] private AudioEventData adaptiveMusicEvent;

        [Header("Biome Music (Unity fallback — one clip per biome)")]
        [SerializeField] private AudioEventData forestMusic;
        [SerializeField] private AudioEventData desertMusic;
        [SerializeField] private AudioEventData mountainMusic;
        [SerializeField] private AudioEventData oceanMusic;
        [SerializeField] private AudioEventData underworldMusic;

        [Header("State Music (Unity fallback)")]
        [SerializeField] private AudioEventData mainMenuMusic;
        [SerializeField] private AudioEventData bossMusic;
        [SerializeField] private AudioEventData victoryMusic;
        [SerializeField] private AudioEventData defeatMusic;

        [Header("Crossfade")]
        [SerializeField, Range(0.1f, 5f)] private float crossfadeDuration = 2f;

        // ── FMOD Parameter Names ──────────────────────────────────────────────────
        public const string PARAM_BIOME_INDEX  = "BiomeIndex";
        public const string PARAM_BOSS_ACTIVE  = "BossActive";
        public const string PARAM_GAME_STATE   = "GameState";   // 0=gameplay, 1=boss, 2=victory, 3=defeat

        // ── Private ──────────────────────────────────────────────────────────────
        private const string MUSIC_KEY = "adaptive_music";
        private bool _bossActive;
        private int  _currentBiomeIndex = -1;
        private Coroutine _fadeCoroutine;

        // ── Lifecycle ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.On<BiomeData>("OnBiomeEntered", OnBiomeEntered);
            EventBus.On<BiomeData>("OnBiomeExited",  OnBiomeExited);
            EventBus.On(GameManager.EVT_GAME_START, OnGameStart);
            EventBus.On(GameManager.EVT_GAME_OVER,  OnGameOver);
            EventBus.On(GameManager.EVT_VICTORY,    OnVictory);
            EventBus.On<string>("OnBossSpawned", OnBossSpawned);
            EventBus.On<string>("OnBossDied",    OnBossDied);
        }

        private void OnDisable()
        {
            EventBus.Off<BiomeData>("OnBiomeEntered", OnBiomeEntered);
            EventBus.Off<BiomeData>("OnBiomeExited",  OnBiomeExited);
            EventBus.Off(GameManager.EVT_GAME_START, OnGameStart);
            EventBus.Off(GameManager.EVT_GAME_OVER,  OnGameOver);
            EventBus.Off(GameManager.EVT_VICTORY,    OnVictory);
            EventBus.Off<string>("OnBossSpawned", OnBossSpawned);
            EventBus.Off<string>("OnBossDied",    OnBossDied);
        }

        // ── Event Handlers ────────────────────────────────────────────────────────
        private void OnGameStart()
        {
            StartAdaptiveMusic();
            SetGameStateParam(0);
        }

        private void OnGameOver()
        {
            StopAdaptiveMusic();
            PlayStateMusic(defeatMusic);
            SetGameStateParam(3);
        }

        private void OnVictory()
        {
            StopAdaptiveMusic();
            PlayStateMusic(victoryMusic);
            SetGameStateParam(2);
        }

        private void OnBiomeEntered(BiomeData biome)
        {
            if (biome == null) return;
            int idx = BiomeIndexOf(biome.biomeId);
            if (idx == _currentBiomeIndex) return;
            _currentBiomeIndex = idx;

            if (!_bossActive)
            {
                AudioManager.Instance?.SetGlobalParameter(PARAM_BIOME_INDEX, idx);
                TransitionBiomeMusicFallback(biome.biomeId);
            }
        }

        private void OnBiomeExited(BiomeData biome)
        {
            // FMOD handles the transition automatically via BiomeIndex parameter change.
            // Unity fallback: the fade is triggered when the next biome is entered.
        }

        private void OnBossSpawned(string bossId)
        {
            _bossActive = true;
            AudioManager.Instance?.SetGlobalParameter(PARAM_BOSS_ACTIVE, 1f);
            AudioManager.Instance?.SetGlobalParameter(PARAM_GAME_STATE,  1f);

            // Unity fallback — crossfade to boss track
            TransitionFallback(bossMusic);
        }

        private void OnBossDied(string bossId)
        {
            _bossActive = false;
            AudioManager.Instance?.SetGlobalParameter(PARAM_BOSS_ACTIVE, 0f);
            AudioManager.Instance?.SetGlobalParameter(PARAM_GAME_STATE,  0f);

            // Return to biome music
            AudioManager.Instance?.SetGlobalParameter(PARAM_BIOME_INDEX, _currentBiomeIndex);
            TransitionBiomeMusicFallback(BiomeIdFromIndex(_currentBiomeIndex));
        }

        // ── Private Helpers ───────────────────────────────────────────────────────
        private void StartAdaptiveMusic()
        {
            if (AudioManager.Instance == null) return;
            AudioManager.Instance.PlayLooping(MUSIC_KEY, adaptiveMusicEvent);
        }

        private void StopAdaptiveMusic()
        {
            AudioManager.Instance?.StopLooping(MUSIC_KEY);
        }

        private void PlayStateMusic(AudioEventData data)
        {
            if (data == null || !data.IsValid) return;
            AudioManager.Instance?.PlayLooping(MUSIC_KEY, data);
        }

        private void TransitionFallback(AudioEventData data)
        {
            if (data == null || AudioManager.Instance == null) return;
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            // The AudioManager's PlayLooping will swap the clip on the music source
            AudioManager.Instance.PlayLooping(MUSIC_KEY, data);
        }

        private void TransitionBiomeMusicFallback(string biomeId)
        {
            var data = BiomeMusicFor(biomeId);
            TransitionFallback(data);
        }

        private AudioEventData BiomeMusicFor(string biomeId) => biomeId switch
        {
            "Forest"     => forestMusic,
            "Desert"     => desertMusic,
            "Mountain"   => mountainMusic,
            "Ocean"      => oceanMusic,
            "Underworld" => underworldMusic,
            _            => forestMusic
        };

        private static int BiomeIndexOf(string biomeId) => biomeId switch
        {
            "Forest"     => 0,
            "Desert"     => 1,
            "Mountain"   => 2,
            "Ocean"      => 3,
            "Underworld" => 4,
            _            => 0
        };

        private static string BiomeIdFromIndex(int idx) => idx switch
        {
            0 => "Forest",
            1 => "Desert",
            2 => "Mountain",
            3 => "Ocean",
            4 => "Underworld",
            _ => "Forest"
        };

        private void SetGameStateParam(float value)
        {
            AudioManager.Instance?.SetGlobalParameter(PARAM_GAME_STATE, value);
        }
    }
}
