using System.Collections.Generic;
using UnityEngine;
#if FMOD_ENABLED
using FMODUnity;
using FMOD.Studio;
#endif

namespace AGNIDAWN.Audio
{
    /// <summary>
    /// Central audio manager for AGNIDAWN.
    ///
    /// When FMOD_ENABLED is defined (FMOD Unity package installed + scripting define set):
    ///   - Uses FMOD Studio RuntimeManager for all playback
    ///   - Supports 3D positioning, parameters, and bus volume control
    ///
    /// When FMOD is not present:
    ///   - Falls back to pooled Unity AudioSources on a dedicated GameObject
    ///   - Same public API surface — callers never care which backend is active
    ///
    /// HOW TO ENABLE FMOD:
    ///   1. Import the FMOD for Unity package (https://fmod.com/unity)
    ///   2. Add FMOD_ENABLED to Project Settings → Player → Scripting Define Symbols
    ///   3. Assign FMOD Bank paths in FMOD Settings (Window → FMOD Studio → Edit Settings)
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static AudioManager Instance { get; private set; }

        // ── Inspector ────────────────────────────────────────────────────────────
        [Header("FMOD Banks (only used when FMOD_ENABLED)")]
        [Tooltip("List of FMOD bank names to load on startup, e.g. 'Master', 'SFX', 'Music'")]
        [SerializeField] private List<string> banksToLoad = new List<string> { "Master", "SFX", "Music" };

        [Header("Unity Fallback — AudioSource Pool")]
        [Tooltip("Number of pooled AudioSources for one-shot SFX")]
        [SerializeField] private int sfxPoolSize = 20;

        [Header("Volumes (0–1)")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float musicVolume  = 0.75f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume    = 1f;
        [SerializeField, Range(0f, 1f)] private float ambientVolume = 0.6f;

        // ── Private ──────────────────────────────────────────────────────────────
        private List<AudioSource> _sfxPool;
        private AudioSource       _musicSource;
        private AudioSource       _ambientSource;
        private int               _sfxPoolIndex;

#if FMOD_ENABLED
        private readonly Dictionary<string, EventInstance> _loopingInstances =
            new Dictionary<string, EventInstance>();
#endif

        // ── Properties ───────────────────────────────────────────────────────────
        public float MasterVolume  { get => masterVolume;  set => SetMasterVolume(value); }
        public float MusicVolume   { get => musicVolume;   set => SetBusVolume(AudioCategory.Music,   value); }
        public float SFXVolume     { get => sfxVolume;     set => SetBusVolume(AudioCategory.SFX,     value); }
        public float AmbientVolume { get => ambientVolume; set => SetBusVolume(AudioCategory.Ambient, value); }

        // ── Unity Lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadVolumePrefs();
            BuildUnityFallbackPool();

#if FMOD_ENABLED
            LoadFMODBanks();
            ApplyFMODVolumes();
#endif
        }

        private void OnDestroy()
        {
#if FMOD_ENABLED
            foreach (var kv in _loopingInstances)
            {
                if (kv.Value.isValid())
                {
                    kv.Value.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                    kv.Value.release();
                }
            }
            _loopingInstances.Clear();
#endif
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Play a one-shot sound at world position (3D).</summary>
        public void PlayOneShot(AudioEventData data, Vector3 position = default)
        {
            if (data == null || !data.IsValid) return;

#if FMOD_ENABLED
            if (!string.IsNullOrEmpty(data.fmodEventPath))
            {
                RuntimeManager.PlayOneShot(data.fmodEventPath, position);
                return;
            }
#endif
            PlayFallbackOneShot(data, position);
        }

        /// <summary>Play a one-shot FMOD event by path (no SO needed).</summary>
        public void PlayOneShot(string fmodPath, Vector3 position = default)
        {
            if (string.IsNullOrEmpty(fmodPath)) return;

#if FMOD_ENABLED
            RuntimeManager.PlayOneShot(fmodPath, position);
#endif
            // No-op in fallback mode — callers should use AudioEventData overload for full fallback support
        }

        /// <summary>Start a looping/ambient event. Key is used to stop it later.</summary>
        public void PlayLooping(string key, AudioEventData data, Vector3 position = default)
        {
            if (data == null || !data.IsValid) return;

#if FMOD_ENABLED
            if (!string.IsNullOrEmpty(data.fmodEventPath))
            {
                StopLooping(key);   // stop previous instance under same key
                var instance = RuntimeManager.CreateInstance(data.fmodEventPath);
                instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
                instance.start();
                _loopingInstances[key] = instance;
                return;
            }
#endif
            PlayFallbackLooping(key, data);
        }

        /// <summary>Stop a looping event by key.</summary>
        public void StopLooping(string key, bool immediate = false)
        {
#if FMOD_ENABLED
            if (_loopingInstances.TryGetValue(key, out var inst) && inst.isValid())
            {
                inst.stop(immediate
                    ? FMOD.Studio.STOP_MODE.IMMEDIATE
                    : FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                inst.release();
                _loopingInstances.Remove(key);
                return;
            }
#endif
            StopFallbackLooping(key);
        }

        /// <summary>Set an FMOD global parameter (e.g. "BiomeIndex", "Intensity").</summary>
        public void SetGlobalParameter(string paramName, float value)
        {
#if FMOD_ENABLED
            RuntimeManager.StudioSystem.setParameterByName(paramName, value);
#endif
        }

        /// <summary>Set volume for an FMOD bus or Unity audio category.</summary>
        public void SetBusVolume(AudioCategory category, float volume)
        {
            volume = Mathf.Clamp01(volume);
            switch (category)
            {
                case AudioCategory.Music:
                    musicVolume = volume;
                    if (_musicSource) _musicSource.volume = musicVolume * masterVolume;
#if FMOD_ENABLED
                    SetFMODBusVolume("bus:/Music", musicVolume * masterVolume);
#endif
                    break;

                case AudioCategory.SFX:
                case AudioCategory.UI:
                    sfxVolume = volume;
#if FMOD_ENABLED
                    SetFMODBusVolume("bus:/SFX", sfxVolume * masterVolume);
#endif
                    break;

                case AudioCategory.Ambient:
                    ambientVolume = volume;
                    if (_ambientSource) _ambientSource.volume = ambientVolume * masterVolume;
#if FMOD_ENABLED
                    SetFMODBusVolume("bus:/Ambient", ambientVolume * masterVolume);
#endif
                    break;
            }
            SaveVolumePrefs();
        }

        /// <summary>Set master volume — scales all other volumes.</summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            if (_musicSource)   _musicSource.volume   = musicVolume   * masterVolume;
            if (_ambientSource) _ambientSource.volume = ambientVolume * masterVolume;
#if FMOD_ENABLED
            SetFMODBusVolume("bus:/", masterVolume);
#endif
            SaveVolumePrefs();
        }

        // ── Private — Unity Fallback ──────────────────────────────────────────────
        private void BuildUnityFallbackPool()
        {
            _sfxPool = new List<AudioSource>(sfxPoolSize);
            for (int i = 0; i < sfxPoolSize; i++)
            {
                var go = new GameObject($"SFXSource_{i}");
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 1f;   // 3D
                _sfxPool.Add(src);
            }

            var musicGO = new GameObject("MusicSource");
            musicGO.transform.SetParent(transform);
            _musicSource = musicGO.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.spatialBlend = 0f;  // 2D
            _musicSource.volume = musicVolume * masterVolume;

            var ambientGO = new GameObject("AmbientSource");
            ambientGO.transform.SetParent(transform);
            _ambientSource = ambientGO.AddComponent<AudioSource>();
            _ambientSource.playOnAwake = false;
            _ambientSource.loop = true;
            _ambientSource.spatialBlend = 0f;
            _ambientSource.volume = ambientVolume * masterVolume;
        }

        private void PlayFallbackOneShot(AudioEventData data, Vector3 position)
        {
            if (data.unityFallbackClip == null) return;

            var src = GetNextPooledSource();
            src.transform.position = position;
            src.clip = data.unityFallbackClip;
            src.volume = data.volume * sfxVolume * masterVolume;
            src.minDistance = data.minDistance;
            src.maxDistance = data.maxDistance;
            src.loop = false;
            src.Play();
        }

        private readonly Dictionary<string, AudioSource> _loopingFallback =
            new Dictionary<string, AudioSource>();

        private void PlayFallbackLooping(string key, AudioEventData data)
        {
            if (data.unityFallbackClip == null) return;
            StopFallbackLooping(key);

            AudioSource src = data.category == AudioCategory.Music ? _musicSource : _ambientSource;
            src.clip = data.unityFallbackClip;
            src.volume = data.volume *
                (data.category == AudioCategory.Music ? musicVolume : ambientVolume) * masterVolume;
            src.loop = true;
            src.Play();
            _loopingFallback[key] = src;
        }

        private void StopFallbackLooping(string key)
        {
            if (_loopingFallback.TryGetValue(key, out var src))
            {
                src.Stop();
                _loopingFallback.Remove(key);
            }
        }

        private AudioSource GetNextPooledSource()
        {
            // Round-robin — skip sources that are still playing if possible
            int start = _sfxPoolIndex;
            for (int i = 0; i < _sfxPool.Count; i++)
            {
                int idx = (start + i) % _sfxPool.Count;
                if (!_sfxPool[idx].isPlaying)
                {
                    _sfxPoolIndex = (idx + 1) % _sfxPool.Count;
                    return _sfxPool[idx];
                }
            }
            // All busy — evict oldest (current index)
            var evicted = _sfxPool[_sfxPoolIndex];
            evicted.Stop();
            _sfxPoolIndex = (_sfxPoolIndex + 1) % _sfxPool.Count;
            return evicted;
        }

        // ── Private — FMOD Helpers ────────────────────────────────────────────────
#if FMOD_ENABLED
        private void LoadFMODBanks()
        {
            foreach (var bank in banksToLoad)
            {
                try { RuntimeManager.LoadBank(bank, loadSamples: true); }
                catch (BankLoadException ex)
                { Debug.LogWarning($"[AudioManager] Could not load FMOD bank '{bank}': {ex.Message}"); }
            }
        }

        private void ApplyFMODVolumes()
        {
            SetFMODBusVolume("bus:/",       masterVolume);
            SetFMODBusVolume("bus:/Music",  musicVolume  * masterVolume);
            SetFMODBusVolume("bus:/SFX",    sfxVolume    * masterVolume);
            SetFMODBusVolume("bus:/Ambient",ambientVolume * masterVolume);
        }

        private static void SetFMODBusVolume(string busPath, float volume)
        {
            var result = RuntimeManager.StudioSystem.getBus(busPath, out Bus bus);
            if (result == FMOD.RESULT.OK) bus.setVolume(volume);
        }
#endif

        // ── Private — Persistence ─────────────────────────────────────────────────
        private const string PREF_MASTER  = "AGNIDAWN_Vol_Master";
        private const string PREF_MUSIC   = "AGNIDAWN_Vol_Music";
        private const string PREF_SFX     = "AGNIDAWN_Vol_SFX";
        private const string PREF_AMBIENT = "AGNIDAWN_Vol_Ambient";

        private void LoadVolumePrefs()
        {
            masterVolume  = PlayerPrefs.GetFloat(PREF_MASTER,  1.0f);
            musicVolume   = PlayerPrefs.GetFloat(PREF_MUSIC,   0.75f);
            sfxVolume     = PlayerPrefs.GetFloat(PREF_SFX,     1.0f);
            ambientVolume = PlayerPrefs.GetFloat(PREF_AMBIENT, 0.6f);
        }

        private void SaveVolumePrefs()
        {
            PlayerPrefs.SetFloat(PREF_MASTER,  masterVolume);
            PlayerPrefs.SetFloat(PREF_MUSIC,   musicVolume);
            PlayerPrefs.SetFloat(PREF_SFX,     sfxVolume);
            PlayerPrefs.SetFloat(PREF_AMBIENT, ambientVolume);
            PlayerPrefs.Save();
        }
    }
}
