using UnityEngine;

namespace AGNIDAWN.Audio
{
    /// <summary>
    /// Exposes master / music / SFX / ambient volume controls to the Settings UI.
    ///
    /// Volumes persist via PlayerPrefs (managed by AudioManager).
    /// This component is a thin proxy — all real work is in AudioManager.
    ///
    /// Attach to any persistent GameObject (e.g. the AudioManager's GO).
    /// Wire UI sliders' onValueChanged → the Set* methods here.
    /// </summary>
    public class AudioBusController : MonoBehaviour
    {
        // ── Public Accessors ──────────────────────────────────────────────────────
        public float MasterVolume
        {
            get => AudioManager.Instance != null ? AudioManager.Instance.MasterVolume : 1f;
            set { if (AudioManager.Instance) AudioManager.Instance.MasterVolume = value; }
        }

        public float MusicVolume
        {
            get => AudioManager.Instance != null ? AudioManager.Instance.MusicVolume : 0.75f;
            set { if (AudioManager.Instance) AudioManager.Instance.MusicVolume = value; }
        }

        public float SFXVolume
        {
            get => AudioManager.Instance != null ? AudioManager.Instance.SFXVolume : 1f;
            set { if (AudioManager.Instance) AudioManager.Instance.SFXVolume = value; }
        }

        public float AmbientVolume
        {
            get => AudioManager.Instance != null ? AudioManager.Instance.AmbientVolume : 0.6f;
            set { if (AudioManager.Instance) AudioManager.Instance.AmbientVolume = value; }
        }

        // ── UI-facing Methods (hookable from UnityEvents / Inspector) ─────────────
        /// <summary>Called by a Settings UI slider for master volume.</summary>
        public void SetMasterVolume(float value)  => MasterVolume  = value;

        /// <summary>Called by a Settings UI slider for music volume.</summary>
        public void SetMusicVolume(float value)   => MusicVolume   = value;

        /// <summary>Called by a Settings UI slider for SFX volume.</summary>
        public void SetSFXVolume(float value)     => SFXVolume     = value;

        /// <summary>Called by a Settings UI slider for ambient volume.</summary>
        public void SetAmbientVolume(float value) => AmbientVolume = value;

        /// <summary>Mute / unmute all audio.</summary>
        public void ToggleMute(bool muted)
        {
            if (AudioManager.Instance == null) return;
            AudioManager.Instance.SetMasterVolume(muted ? 0f : MasterVolume);
        }

        /// <summary>Reset all volumes to defaults and persist.</summary>
        public void ResetToDefaults()
        {
            MasterVolume  = 1.0f;
            MusicVolume   = 0.75f;
            SFXVolume     = 1.0f;
            AmbientVolume = 0.6f;
        }
    }
}
