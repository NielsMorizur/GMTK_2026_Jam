using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ParsaProto
{
    /// <summary>
    /// Fade-and-rise text exposition: each line fades in while drifting up,
    /// holds, fades away, then the next. Any key or click skips the current
    /// line. Loads the next scene when the last line has played. Lines and
    /// timing are edited on the component.
    /// </summary>
    public class PP_Exposition : MonoBehaviour
    {
        [TextArea] public string[] lines;
        public TextMeshProUGUI text;
        public string nextScene = "MainRoom";
        [Tooltip("TESTING ONLY: loop the lines forever instead of loading the next scene")]
        public bool loopForTesting = false;

        [Header("Timing")]
        [Tooltip("Seconds for each word to rise into place")]
        public float wordFadeTime = 0.3f;
        [Tooltip("Seconds between one word starting and the next (they may overlap)")]
        public float wordGap = 0.14f;
        [Tooltip("Longer pause used after the special words below")]
        public float specialWordGap = 0.6f;
        [Tooltip("Words that get the longer pause after them (punctuation ignored)")]
        public string[] pauseAfterWords = { "Ah", "Regrets" };
        public float fadeOut = 0.5f;
        public float gap = 0.25f;
        [Tooltip("How far each word rises into place, in em")]
        public float wordRise = 0.35f;

        [Header("Idle float")]
        [Tooltip("Gentle bob of the landed sentence, pixels")]
        public float floatAmplitude = 6f;
        public float floatPeriod = 3.5f;

        RectTransform _rt;
        Vector2 _home;
        bool _advance;

        IEnumerator Start()
        {
            if (text == null || lines == null || lines.Length == 0)
            {
                SceneManager.LoadScene(nextScene);
                yield break;
            }

            _rt = text.rectTransform;
            _home = _rt.anchoredPosition;
            text.alpha = 0f;

            var sb = new System.Text.StringBuilder();
            do
            {
            foreach (var line in lines)
            {
                _advance = false;
                text.alpha = 1f;

                // each word starts on the word-gap beat (rises may overlap);
                // special words hold the next one back for a dramatic pause
                string[] words = line.Split(' ');
                float[] starts = new float[words.Length];
                for (int i = 1; i < words.Length; i++)
                    starts[i] = starts[i - 1]
                        + (IsSpecial(words[i - 1]) ? specialWordGap : wordGap);
                float revealTotal = starts[words.Length - 1] + wordFadeTime;

                float t = 0f;
                while (t < revealTotal && !_advance)
                {
                    t += Time.unscaledDeltaTime;
                    _rt.anchoredPosition = _home;

                    // every word fades AND rises on its own clock; earlier
                    // words sit still while the new one floats up into place
                    sb.Length = 0;
                    for (int i = 0; i < words.Length; i++)
                    {
                        float a = Mathf.Clamp01((t - starts[i]) / Mathf.Max(0.01f, wordFadeTime));
                        float e = a * a * (3f - 2f * a);
                        float off = -wordRise * (1f - e);

                        sb.Append("<alpha=#").Append(((int)(a * 255)).ToString("X2")).Append(">");
                        sb.Append("<voffset=").Append(off.ToString("F3")).Append("em>");
                        sb.Append(words[i]);
                        sb.Append("</voffset>");
                        if (i < words.Length - 1) sb.Append(' ');
                    }
                    text.text = sb.ToString();
                    yield return null;
                }
                text.text = line;

                // the landed sentence floats gently until space moves it
                // along. The bob clock starts at THIS moment, so it begins at
                // exactly zero offset and eases in - no snap.
                float w = Mathf.PI * 2f / Mathf.Max(0.5f, floatPeriod);
                float bobStart = Time.unscaledTime;
                _advance = false;
                while (!_advance)
                {
                    _rt.anchoredPosition = _home + new Vector2(0f,
                        Mathf.Sin((Time.unscaledTime - bobStart) * w) * floatAmplitude);
                    yield return null;
                }
                _advance = false;

                // fade in place, same bob clock so the motion never jumps
                t = 0f;
                while (t < fadeOut && !_advance)
                {
                    t += Time.unscaledDeltaTime;
                    text.alpha = 1f - Mathf.Clamp01(t / fadeOut);
                    _rt.anchoredPosition = _home + new Vector2(0f,
                        Mathf.Sin((Time.unscaledTime - bobStart) * w) * floatAmplitude);
                    yield return null;
                }
                text.alpha = 0f;

                t = 0f;
                while (t < gap && !_advance)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            } while (loopForTesting);

            SceneManager.LoadScene(nextScene);
        }

        bool IsSpecial(string word)
        {
            if (pauseAfterWords == null) return false;
            string w = word.TrimEnd(',', '.', '?', '!', ':', ';').ToLowerInvariant();
            foreach (var s in pauseAfterWords)
            {
                if (string.IsNullOrEmpty(s)) continue;
                if (w == s.TrimEnd(',', '.', '?', '!', ':', ';').ToLowerInvariant())
                    return true;
            }
            return false;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame)
                _advance = true;
        }
    }
}
