using UnityEngine;

namespace ParsaProto
{
    /// <summary>Player-facing settings, persisted across sessions.</summary>
    public static class PP_Settings
    {
        const string ShakeKey = "pp_disable_screenshake";
        const string MasterKey = "pp_master_volume";
        const string SfxKey = "pp_sfx_volume";
        const string MusicKey = "pp_music_volume";

        public static bool ScreenShakeDisabled
        {
            get => PlayerPrefs.GetInt(ShakeKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(ShakeKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Whole-game volume, applied to the audio listener.</summary>
        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(MasterKey, 1f);
            set
            {
                PlayerPrefs.SetFloat(MasterKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
                ApplyAudio();
            }
        }

        /// <summary>Multiplier on every sound effect (music unaffected).</summary>
        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxKey, 1f);
            set
            {
                PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
            }
        }

        /// <summary>Multiplier on the music track only.</summary>
        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicKey, 1f);
            set
            {
                PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
                if (PP_MusicPlayer.Instance != null)
                    PP_MusicPlayer.Instance.ApplyVolume();
            }
        }

        /// <summary>Push the saved master volume onto the listener.</summary>
        public static void ApplyAudio()
        {
            AudioListener.volume = PlayerPrefs.GetFloat(MasterKey, 1f);
        }
    }
}
