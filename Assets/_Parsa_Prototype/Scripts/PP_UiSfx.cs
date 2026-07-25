using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Menu sound plumbing: one instance per menu scene holds the hover and
    /// click clips; the button juice calls the static hooks. Pitch is jittered
    /// per play and volume follows the settings SFX slider.
    /// </summary>
    public class PP_UiSfx : MonoBehaviour
    {
        public AudioClip hover;
        public AudioClip click;
        [Range(0f, 1f)] public float volume = 0.7f;
        [Tooltip("Random pitch spread per play, +/- this much")]
        public float pitchJitter = 0.08f;

        static PP_UiSfx _instance;
        AudioSource _src;

        void Awake()
        {
            _instance = this;
            _src = gameObject.AddComponent<AudioSource>();
            _src.playOnAwake = false;
            _src.spatialBlend = 0f;
        }

        public static void Hover() { if (_instance != null) _instance.Play(_instance.hover); }
        public static void Click() { if (_instance != null) _instance.Play(_instance.click); }

        void Play(AudioClip clip)
        {
            if (clip == null || _src == null) return;
            _src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            _src.PlayOneShot(clip, volume * PP_Settings.SfxVolume);
        }
    }
}
