using UnityEngine;

namespace AGNIDAWN.Audio
{
    /// <summary>
    /// ScriptableObject that stores an audio event definition.
    /// Supports both FMOD Studio events (primary) and Unity AudioClip fallback.
    ///
    /// FMOD path convention: "event:/Category/SubCategory/EventName"
    /// e.g.  "event:/SFX/Weapons/Trishul_Fire"
    ///       "event:/Music/Biome_Forest"
    ///       "event:/UI/Button_Click"
    /// </summary>
    [CreateAssetMenu(menuName = "AGNIDAWN/Audio/AudioEventData", fileName = "AED_New")]
    public class AudioEventData : ScriptableObject
    {
        [Header("FMOD")]
        [Tooltip("FMOD Studio event path, e.g. event:/SFX/Weapons/Trishul_Fire")]
        public string fmodEventPath = string.Empty;

        [Header("Unity Fallback (used when FMOD is not installed)")]
        [Tooltip("AudioClip played when FMOD is unavailable")]
        public AudioClip unityFallbackClip;

        [Header("Playback Settings")]
        [Range(0f, 1f)]
        public float volume = 1f;

        [Tooltip("Loop the event (Unity fallback only; FMOD events handle looping internally)")]
        public bool loop = false;

        [Tooltip("Minimum distance before volume attenuation starts (Unity fallback only)")]
        public float minDistance = 1f;

        [Tooltip("Maximum audible distance (Unity fallback only)")]
        public float maxDistance = 50f;

        [Header("Category")]
        public AudioCategory category = AudioCategory.SFX;

        /// <summary>Returns true when this event has a usable FMOD path or Unity clip.</summary>
        public bool IsValid =>
            (!string.IsNullOrEmpty(fmodEventPath)) || (unityFallbackClip != null);
    }

    public enum AudioCategory
    {
        SFX,
        Music,
        Ambient,
        UI,
        VO         // Voice-over / narration
    }
}
