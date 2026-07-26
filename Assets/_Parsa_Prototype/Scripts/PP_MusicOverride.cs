using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Scene-local music: while this object lives, the global stem player is
    /// silenced and the AudioSource on this object plays instead (the intro's
    /// limbo track). Volume follows the settings music slider. When the scene
    /// unloads, the stems fade back in on their own.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class PP_MusicOverride : MonoBehaviour
    {
        [Range(0f, 1f)] public float volume = 0.5f;

        AudioSource _src;

        void Awake()
        {
            _src = GetComponent<AudioSource>();
        }

        void Update()
        {
            if (PP_MusicPlayer.Instance != null)
                PP_MusicPlayer.Instance.Suppress(true);
            if (_src != null)
                _src.volume = volume * PP_Settings.MusicVolume;
        }

        void OnDestroy()
        {
            if (PP_MusicPlayer.Instance != null)
                PP_MusicPlayer.Instance.Suppress(false);
        }
    }
}
