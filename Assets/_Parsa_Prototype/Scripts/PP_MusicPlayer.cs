using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Persistent music: placed in the hub, survives every scene load, and
    /// destroys any duplicate of itself when the hub is revisited.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class PP_MusicPlayer : MonoBehaviour
    {
        public static PP_MusicPlayer Instance { get; private set; }

        AudioSource _source;
        float _baseVolume = 1f;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _source = GetComponent<AudioSource>();
            if (_source != null) _baseVolume = _source.volume;
            ApplyVolume();
            PP_Settings.ApplyAudio();   // saved master volume applies from the first note

            // persists only when standalone; as a child (e.g. on the player)
            // it simply lives and dies with its parent
            if (transform.parent == null)
                DontDestroyOnLoad(gameObject);
        }

        /// <summary>Authored volume x saved music setting.</summary>
        public void ApplyVolume()
        {
            if (_source != null)
                _source.volume = _baseVolume * PP_Settings.MusicVolume;
        }
    }
}
