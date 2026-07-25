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

        [Tooltip("Pitch and speed per form, in age order: Elderly, Adult, Teen, Baby. Semitone steps by default so each form owns a key.")]
        public float[] formPitch = { 1f, 1.06f, 1.122f, 1.189f };
        [Tooltip("How fast the pitch glides between forms, per second")]
        public float pitchGlide = 2f;

        AudioSource _source;
        float _baseVolume = 1f;
        Player _watched;
        float _refindTimer;

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

        void Update()
        {
            if (_source == null) return;

            // the track follows the player's current form: same pitch step
            // every time, so each form keeps its own key. No player in the
            // scene (menus) = default pitch. Resets land back on the elder,
            // which brings the music home too.
            if (_watched == null)
            {
                _refindTimer -= Time.unscaledDeltaTime;
                if (_refindTimer <= 0f)
                {
                    _watched = FindAnyObjectByType<Player>();
                    _refindTimer = 0.5f;
                }
            }

            float target = 1f;
            if (_watched != null && formPitch != null)
            {
                int i = (int)_watched.CurrentAge;
                if (i >= 0 && i < formPitch.Length) target = formPitch[i];
            }

            _source.pitch = Mathf.MoveTowards(_source.pitch, target,
                pitchGlide * Time.unscaledDeltaTime);
        }
    }
}
