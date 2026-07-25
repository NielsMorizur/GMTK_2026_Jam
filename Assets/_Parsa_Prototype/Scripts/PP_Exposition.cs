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

        [Header("Timing")]
        [Tooltip("Seconds for each word to rise into place")]
        public float wordFadeTime = 0.3f;
        [Tooltip("Pause after a word lands before the next one starts")]
        public float wordGap = 0.05f;
        public float fadeOut = 0.5f;
        public float gap = 0.25f;
        [Tooltip("Line rise on fade-out, pixels")]
        public float riseDistance = 30f;
        [Tooltip("How far each word rises into place, in em")]
        public float wordRise = 0.35f;

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
            foreach (var line in lines)
            {
                _advance = false;
                text.alpha = 1f;

                // strictly one word at a time: each starts only after the
                // previous one has fully landed (plus a small pause)
                string[] words = line.Split(' ');
                float slot = wordFadeTime + wordGap;
                float revealTotal = slot * words.Length - wordGap;

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
                        float a = Mathf.Clamp01((t - i * slot) / Mathf.Max(0.01f, wordFadeTime));
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
                _rt.anchoredPosition = _home;

                // the line stays up until space moves the story along
                _advance = false;
                while (!_advance)
                    yield return null;
                _advance = false;

                t = 0f;
                while (t < fadeOut && !_advance)
                {
                    t += Time.unscaledDeltaTime;
                    float e = 1f - Mathf.Clamp01(t / fadeOut);
                    text.alpha = e;
                    _rt.anchoredPosition = _home + new Vector2(0f, riseDistance * 0.4f * (1f - e));
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

            SceneManager.LoadScene(nextScene);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame)
                _advance = true;
        }
    }
}
