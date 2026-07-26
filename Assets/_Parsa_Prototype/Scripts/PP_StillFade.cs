using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// One exposition still: waits, fades in, optionally holds and fades back
    /// out. Hold &lt; 0 means "stay up forever". Chain several with staggered
    /// delays for a one-by-one sequence.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PP_StillFade : MonoBehaviour
    {
        [Tooltip("Seconds before the fade-in starts")]
        public float startDelay = 0.5f;
        public float fadeIn = 2.5f;
        [Tooltip("Seconds fully visible before fading out; negative = stay forever")]
        public float hold = -1f;
        public float fadeOut = 2f;
        [Range(0f, 1f)] public float maxAlpha = 1f;

        [Header("Sound")]
        [Tooltip("Played once as the fade-in begins")]
        public AudioClip appearSfx;
        [Range(0f, 1f)] public float appearVolume = 0.8f;

        SpriteRenderer _sr;
        float _t;

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (Application.isPlaying) Apply(0f);
        }

        // re-activation (a director section starting) replays from the top
        void OnEnable()
        {
            if (!Application.isPlaying) return;
            _t = 0f;
            _exiting = false;
            _sfxPlayed = false;
            if (_sr != null) Apply(0f);
        }

        bool _sfxPlayed;
        AudioSource _audio;

        bool _exiting;

        /// <summary>Director hook: fade out now, whatever the timers say.</summary>
        void IntroSectionExit()
        {
            _exiting = true;
        }

        void Update()
        {
            // edit mode: always fully visible so placement is WYSIWYG
            if (!Application.isPlaying)
            {
                Apply(maxAlpha);
                return;
            }

            if (_exiting)
            {
                var ec = _sr.color;
                Apply(Mathf.MoveTowards(ec.a, 0f,
                    Time.unscaledDeltaTime / Mathf.Max(0.05f, fadeOut)));
                return;
            }

            _t += Time.unscaledDeltaTime;
            float t = _t - startDelay;

            if (t > 0f && !_sfxPlayed && appearSfx != null)
            {
                _sfxPlayed = true;
                if (_audio == null)
                {
                    _audio = gameObject.AddComponent<AudioSource>();
                    _audio.playOnAwake = false;
                    _audio.spatialBlend = 0f;
                }
                _audio.PlayOneShot(appearSfx, appearVolume * PP_Settings.SfxVolume);
            }

            float a;
            if (t <= 0f) a = 0f;
            else if (t < fadeIn) a = t / Mathf.Max(0.01f, fadeIn);
            else if (hold < 0f) a = 1f;
            else if (t < fadeIn + hold) a = 1f;
            else a = 1f - Mathf.Clamp01((t - fadeIn - hold) / Mathf.Max(0.01f, fadeOut));

            Apply(Mathf.SmoothStep(0f, 1f, a) * maxAlpha);
        }

        void Apply(float a)
        {
            var c = _sr.color;
            c.a = a;
            _sr.color = c;
        }

        /// <summary>Restart the fade from the beginning (for sequencing later).</summary>
        public void Replay()
        {
            _t = 0f;
            Apply(0f);
        }
    }
}
