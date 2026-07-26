using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ParsaProto
{
    /// <summary>
    /// Conducts the intro as SECTIONS. Space gates the sections; inside a
    /// section, text reveals word by word and the visuals run on their own
    /// tuned timers. Entering a section activates its Stage Set object and
    /// sends IntroSectionBegin to it (and the optional Notify object);
    /// leaving sends IntroSectionExit, fades the text, then deactivates the
    /// set. The last section is expected to end the scene itself.
    /// </summary>
    public class PP_IntroDirector : MonoBehaviour
    {
        [System.Serializable]
        public class Section
        {
            [TextArea] public string line;
            [Tooltip("Activated while this section plays; deactivated after it ends")]
            public GameObject stageSet;
            [Tooltip("Also gets the Begin/Exit messages without being toggled (e.g. the elder)")]
            public GameObject notify;
            [Tooltip("Seconds after the section starts before its text begins")]
            public float textDelay;
            [Tooltip("This section's stage set is never faded out or deactivated (its visuals persist)")]
            public bool keepAlive;
            [Tooltip("Seconds from this section's start until the next begins automatically; 0 = driven elsewhere (the fall's landing, or the final section)")]
            public float duration;
            [Tooltip("This section's line fades out over this many seconds when it ends; 0 = the global Text Fade Out")]
            public float textFadeOut;
        }

        public TextMeshProUGUI text;
        public Section[] sections;
        [Tooltip("Once this fall starts, Space is ignored and the finale runs itself")]
        public PP_IntroFall fall;
        [Tooltip("Seconds after the landing before the climb section starts on its own")]
        public float landToClimbDelay = 0.8f;
        [Tooltip("Seconds into the fall section before its line fades out mid-drop")]
        public float fallTextFadeDelay = 1.3f;

        [Header("Text timing")]
        [Tooltip("Seconds for each word to rise into place")]
        public float wordFadeTime = 0.3f;
        [Tooltip("Seconds between one word starting and the next")]
        public float wordGap = 0.14f;
        public float textFadeOut = 0.4f;
        [Tooltip("Seconds the outgoing stage set stays active while it fades")]
        public float exitGrace = 0.9f;
        [Tooltip("How far each word rises, in em")]
        public float wordRise = 0.35f;

        int _idx = -1;
        bool _revealing;
        bool _skip;

        void Start()
        {
            if (sections != null)
                foreach (var s in sections)
                    if (s.stageSet != null) s.stageSet.SetActive(false);
            if (text != null) text.alpha = 0f;
            Advance();
        }

        void Update()
        {
            // full cinematic: no player input, everything runs on the clock
            _sectionTime += Time.unscaledDeltaTime;

            if (fall != null && fall.FallStarted)
            {
                // the fall line fades away while he drops
                if (!_fallTextFaded && _sectionTime >= fallTextFadeDelay)
                {
                    _fallTextFaded = true;
                    if (text != null) StartCoroutine(FadeTextOut());
                }
                if (fall.Landed && !_autoAdvanced)
                {
                    _landTimer += Time.unscaledDeltaTime;
                    if (_landTimer >= landToClimbDelay)
                    {
                        _autoAdvanced = true;
                        Advance();
                    }
                }
                return;
            }

            if (_idx >= 0 && _idx < sections.Length)
            {
                var cur = sections[_idx];
                if (cur.duration > 0f && _sectionTime >= cur.duration)
                    Advance();
            }
        }

        bool _autoAdvanced;
        bool _fallTextFaded;
        float _landTimer;
        float _sectionTime;
        Coroutine _revealCo;
        Coroutine _fadeCo;

        /// <summary>Mid-section line change (the still sequence syncs its
        /// lines to still entrances through this).</summary>
        public void ShowLine(string line)
        {
            if (_revealCo != null) StopCoroutine(_revealCo);
            _revealCo = StartCoroutine(Reveal(line, 0f, waitFade: false));
        }

        void Advance()
        {
            if (sections == null || _idx + 1 >= sections.Length && _idx >= 0) return;

            if (_idx >= 0)
            {
                var old = sections[_idx];
                if (!old.keepAlive)
                {
                    Notify(old, "IntroSectionExit");
                    StartCoroutine(RetireSet(old.stageSet));
                }
                if (text != null)
                {
                    if (_fadeCo != null) StopCoroutine(_fadeCo);
                    _fadeCo = StartCoroutine(FadeTextOut(
                        old.textFadeOut > 0f ? old.textFadeOut : textFadeOut));
                }
            }

            _sectionTime = 0f;
            _idx++;
            var cur = sections[_idx];
            if (cur.stageSet != null) cur.stageSet.SetActive(true);
            Notify(cur, "IntroSectionBegin");

            // the persistent eye watches whatever this section is about
            PP_PersistentEye.Watch(cur.notify != null ? cur.notify.transform
                : (cur.stageSet != null ? cur.stageSet.transform : null));

            _revealCo = StartCoroutine(Reveal(cur.line, cur.textDelay));
        }

        void Notify(Section s, string msg)
        {
            if (s.stageSet != null) s.stageSet.BroadcastMessage(msg, SendMessageOptions.DontRequireReceiver);
            if (s.notify != null) s.notify.BroadcastMessage(msg, SendMessageOptions.DontRequireReceiver);
        }

        IEnumerator RetireSet(GameObject set)
        {
            if (set == null) yield break;
            float t = 0f;
            while (t < exitGrace) { t += Time.unscaledDeltaTime; yield return null; }
            if (set != null) set.SetActive(false);
        }

        IEnumerator FadeTextOut(float dur = -1f)
        {
            if (text == null) yield break;
            if (dur <= 0f) dur = textFadeOut;
            float start = text.alpha;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                text.alpha = Mathf.Lerp(start, 0f, t / Mathf.Max(0.01f, dur));
                yield return null;
            }
        }

        IEnumerator Reveal(string line, float delay, bool waitFade = true)
        {
            _revealing = true;
            _skip = false;
            if (text == null || string.IsNullOrWhiteSpace(line))
            {
                // nothing to say: leave the outgoing fade alone, it hides
                // the previous line at its own pace
                _revealing = false;
                yield break;
            }

            // wait out the previous line's fade (synced lines skip straight
            // in), then the section's own gap
            float wait = (waitFade ? textFadeOut : 0f) + delay;
            float w = 0f;
            while (w < wait && !_skip)
            {
                w += Time.unscaledDeltaTime;
                yield return null;
            }
            _skip = false;

            // this line owns the text box now: kill any leftover fade
            if (_fadeCo != null) { StopCoroutine(_fadeCo); _fadeCo = null; }
            text.alpha = 1f;

            string[] words = line.Split(' ');
            float[] starts = new float[words.Length];
            for (int i = 1; i < words.Length; i++)
                starts[i] = starts[i - 1] + wordGap;
            float total = starts[words.Length - 1] + wordFadeTime;

            var sb = new System.Text.StringBuilder();
            float t = 0f;
            while (t < total && !_skip)
            {
                t += Time.unscaledDeltaTime;
                sb.Length = 0;
                for (int i = 0; i < words.Length; i++)
                {
                    float a = Mathf.Clamp01((t - starts[i]) / Mathf.Max(0.01f, wordFadeTime));
                    float e = a * a * (3f - 2f * a);
                    sb.Append("<alpha=#").Append(((int)(a * 255)).ToString("X2")).Append(">");
                    sb.Append("<voffset=").Append((-wordRise * (1f - e)).ToString("F3")).Append("em>");
                    sb.Append(words[i]).Append("</voffset>");
                    if (i < words.Length - 1) sb.Append(' ');
                }
                text.text = sb.ToString();
                yield return null;
            }
            text.text = line;
            _revealing = false;
        }
    }
}
