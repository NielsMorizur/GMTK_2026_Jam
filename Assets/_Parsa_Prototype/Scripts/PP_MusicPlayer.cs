using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Layered music: four stems of the same song start together sample-locked
    /// and loop forever; the player's form decides how many are audible.
    /// Elder = melody, Adult adds drums, Teen adds bass, Baby adds hi-hat.
    /// Because every stem is always playing, unmuting is seamless at ANY
    /// moment - single steps, skips, and resets all stay on the beat.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class PP_MusicPlayer : MonoBehaviour
    {
        public static PP_MusicPlayer Instance { get; private set; }

        [Tooltip("Stems in age order: Elder (melody), Adult (drums), Teen (bass), Baby (hi-hat). Same length, same tempo.")]
        public AudioClip[] layers = new AudioClip[4];
        [Tooltip("Seconds for a stem to bloom in (or duck out) on a form change")]
        public float fadeTime = 1.2f;

        AudioSource[] _sources;
        float[] _level;
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

            var first = GetComponent<AudioSource>();
            _baseVolume = first != null ? first.volume : 1f;
            PP_Settings.ApplyAudio();   // saved master volume applies from the first note

            int n = layers != null ? layers.Length : 0;
            _sources = new AudioSource[n];
            _level = new float[n];
            double startAt = AudioSettings.dspTime + 0.15;
            for (int i = 0; i < n; i++)
            {
                var src = (i == 0 && first != null)
                    ? first : gameObject.AddComponent<AudioSource>();
                src.clip = layers[i];
                src.loop = true;
                src.playOnAwake = false;
                src.pitch = 1f;
                src.spatialBlend = 0f;
                src.volume = 0f;
                _sources[i] = src;
                _level[i] = i == 0 ? 1f : 0f;   // the melody is always there
                if (layers[i] != null) src.PlayScheduled(startAt);   // sample-locked
            }

            // whoever wakes first becomes THE music player and survives every
            // scene change (detached if it was a child, e.g. on the player);
            // all later copies destroy themselves in the guard above. The
            // stems never stop, so no transition ever restarts the song.
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>Kept for the settings slider; volumes recompute every frame.</summary>
        public void ApplyVolume() { }

        void Update()
        {
            if (_sources == null) return;

            if (_watched == null)
            {
                _refindTimer -= Time.unscaledDeltaTime;
                if (_refindTimer <= 0f)
                {
                    _watched = FindAnyObjectByType<Player>();
                    _refindTimer = 0.5f;
                }
            }

            // the melody is the clock: mp3 stems never decode to exactly the
            // same sample count, so each loop wrap would let the others creep
            // off-beat. Snap any stem back once it strays past ~35ms.
            var master = _sources[0];
            if (master != null && master.isPlaying)
            {
                for (int i = 1; i < _sources.Length; i++)
                {
                    var s = _sources[i];
                    if (s == null || s.clip == null || !s.isPlaying) continue;
                    if (Mathf.Abs(s.time - master.time) > 0.035f)
                        s.time = master.time;
                }
            }

            // menus (no player) keep just the melody; each younger form
            // unmutes its stem, and aging back down mutes them again
            int audible = _watched != null ? (int)_watched.CurrentAge + 1 : 1;

            float step = Time.unscaledDeltaTime / Mathf.Max(0.05f, fadeTime);
            for (int i = 0; i < _sources.Length; i++)
            {
                if (_sources[i] == null) continue;
                _level[i] = Mathf.MoveTowards(_level[i], i < audible ? 1f : 0f, step);
                _sources[i].volume = _level[i] * _baseVolume * PP_Settings.MusicVolume;
            }
        }
    }
}
