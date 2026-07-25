using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Owns the whole age-still chain, overlapping-pair rhythm: the first
    /// fades in, then the second fades in, then the first fades out, then the
    /// third fades in, and so on - never more than two on screen. Interval is
    /// THE pacing knob (seconds per beat). In edit mode every still shows at
    /// full alpha for placement.
    /// </summary>
    [ExecuteAlways]
    public class PP_StillSequence : MonoBehaviour
    {
        [Tooltip("Seconds before the first still starts")]
        public float startAt = 8.5f;
        [Tooltip("THE knob: seconds per beat of the chain")]
        public float interval = 2.5f;
        [Tooltip("Seconds each fade takes (keep at or below Interval)")]
        public float fadeTime = 1.8f;
        [Range(0f, 1f)] public float maxAlpha = 1f;
        [Tooltip("Extra seconds the LAST still stays up before fading out; negative = it never fades")]
        public float lastStillExtraHold = 3f;
        [Tooltip("In order of appearance")]
        public SpriteRenderer[] stills;

        float _t;

        /// <summary>Scene time at which the last still begins its fade-out.
        /// The intro fall cues off this.</summary>
        public float FinalOutTime => stills == null || stills.Length == 0
            ? startAt
            : startAt + (2 * (stills.Length - 1) + 2) * interval
              + Mathf.Max(0f, lastStillExtraHold);

        void OnEnable()
        {
            _t = 0f;
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

            _t += Time.unscaledDeltaTime;

            // beats: s1 in, s2 in, s1 out, s3 in, s2 out, s4 in, s3 out, s4 out
            for (int i = 0; i < stills.Length; i++)
            {
                if (stills[i] == null) continue;
                int inBeat = Mathf.Max(0, 2 * i - 1);
                int outBeat = 2 * i + 2;
                bool last = i == stills.Length - 1;

                float rise = Ramp(_t - (startAt + inBeat * interval));
                float fall = (last && lastStillExtraHold < 0f) ? 0f
                    : Ramp(_t - (startAt + outBeat * interval
                                 + (last ? lastStillExtraHold : 0f)));
                float a = Mathf.SmoothStep(0f, 1f, rise) * (1f - Mathf.SmoothStep(0f, 1f, fall));
                Apply(stills[i], a * maxAlpha);
            }
        }

        float Ramp(float x) => Mathf.Clamp01(x / Mathf.Max(0.01f, fadeTime));

        void Apply(SpriteRenderer sr, float a)
        {
            var c = sr.color;
            c.a = a;
            sr.color = c;
        }
    }
}
