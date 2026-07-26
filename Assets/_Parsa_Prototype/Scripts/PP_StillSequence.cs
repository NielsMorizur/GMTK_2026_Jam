using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Owns the age-still chain with fully independent timing: each still
    /// fades in over Fade In, sits at full for Hold, and leaves over Fade
    /// Out. Entrances are spaced so at most two stills are ever on screen
    /// (each enters exactly half a lifetime after the previous). In edit mode
    /// every still shows at full alpha for placement.
    /// </summary>
    [ExecuteAlways]
    public class PP_StillSequence : MonoBehaviour
    {
        [Tooltip("Seconds after the section starts before the first still")]
        public float startAt = 0.3f;
        [Tooltip("THE knob: seconds between one still entering and the next")]
        public float interval = 1.2f;
        [Tooltip("Seconds each still takes to fade in")]
        public float fadeIn = 0.6f;
        [Tooltip("Seconds each still sits at full before fading out (set to 2 x Interval - Fade In to sync exits with entrances)")]
        public float hold = 1.8f;
        [Tooltip("Seconds each still takes to fade out")]
        public float fadeOut = 0.6f;
        [Range(0f, 1f)] public float maxAlpha = 1f;
        [Tooltip("Negative = the LAST still never fades (stays until you advance); otherwise it fades this many seconds after its exit slot")]
        public float lastStillExtraHold = -1f;
        [Tooltip("In order of appearance")]
        public SpriteRenderer[] stills;

        [Header("Synced text")]
        [Tooltip("Line shown as each still (same order) begins fading in")]
        public string[] stillLines;
        public PP_IntroDirector director;

        float _t;
        bool _exiting;
        bool[] _lineShown;

        /// <summary>Scene time at which the last still begins its fade-out
        /// (or its entry completes when it never fades).</summary>
        public float FinalOutTime => stills == null || stills.Length == 0
            ? startAt
            : startAt + (stills.Length + 1) * interval
              + Mathf.Max(0f, lastStillExtraHold);

        void OnEnable()
        {
            _t = 0f;
            _exiting = false;
            _lineShown = new bool[stills != null ? stills.Length : 0];
        }

        /// <summary>Director hook: fade everything down now.</summary>
        void IntroSectionExit()
        {
            _exiting = true;
        }

        void Update()
        {
            if (stills == null) return;

            // edit mode: everything visible, placement is WYSIWYG
            if (!Application.isPlaying)
            {
                foreach (var s in stills)
                    if (s != null) Apply(s, maxAlpha);
                return;
            }

            if (_exiting)
            {
                foreach (var s in stills)
                {
                    if (s == null) continue;
                    var c = s.color;
                    c.a = Mathf.MoveTowards(c.a, 0f,
                        Time.unscaledDeltaTime / Mathf.Max(0.05f, fadeOut));
                    s.color = c;
                }
                return;
            }

            _t += Time.unscaledDeltaTime;

            // baby in, teen in, baby out AS adult in, teen out AS elder in,
            // adult out one beat later - and the elder just stays
            for (int i = 0; i < stills.Length; i++)
            {
                if (stills[i] == null) continue;
                bool last = i == stills.Length - 1;

                float tIn = startAt + i * interval;
                float tOut = tIn + Mathf.Max(0.05f, fadeIn) + Mathf.Max(0f, hold)
                             + (last ? Mathf.Max(0f, lastStillExtraHold) : 0f);

                // the synced line lands exactly as this still begins
                if (_t >= tIn && _lineShown != null && i < _lineShown.Length && !_lineShown[i])
                {
                    _lineShown[i] = true;
                    if (director != null && stillLines != null && i < stillLines.Length
                        && !string.IsNullOrWhiteSpace(stillLines[i]))
                        director.ShowLine(stillLines[i]);
                }

                float rise = Mathf.Clamp01((_t - tIn) / Mathf.Max(0.05f, fadeIn));
                float fall = (last && lastStillExtraHold < 0f) ? 0f
                    : Mathf.Clamp01((_t - tOut) / Mathf.Max(0.05f, fadeOut));

                float a = Mathf.SmoothStep(0f, 1f, rise)
                          * (1f - Mathf.SmoothStep(0f, 1f, fall));
                Apply(stills[i], a * maxAlpha);
            }
        }

        void Apply(SpriteRenderer sr, float a)
        {
            var c = sr.color;
            c.a = a;
            sr.color = c;
        }
    }
}
