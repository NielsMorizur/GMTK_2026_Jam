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
        [Tooltip("Seconds for each word to fade in")]
        public float wordFadeTime = 0.3f;
        [Tooltip("Seconds between one word starting and the next")]
        public float wordGap = 0.14f;
        public float fadeOut = 0.5f;
        public float gap = 0.25f;
        public float riseDistance = 30f;

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

                // words fade in one after another; the whole line drifts up
                // gently across the reveal
                string[] words = line.Split(' ');
                float revealTotal = wordGap * (words.Length - 1) + wordFadeTime;

                float t = 0f;
                while (t < revealTotal && !_advance)
                {
                    t += Time.unscaledDeltaTime;
                    float lp = Mathf.Clamp01(t / revealTotal);
                    lp = lp * lp * (3f - 2f * lp);
                    _rt.anchoredPosition = _home + new Vector2(0f, -riseDistance * (1f - lp));

                    sb.Length = 0;
                    for (int i = 0; i < words.Length; i++)
                    {
                        float a = Mathf.Clamp01((t - i * wordGap) / Mathf.Max(0.01f, wordFadeTime));
                        sb.Append("<alpha=#").Append(((int)(a * 255)).ToString("X2")).Append(">");
                        sb.Append(words[i]);
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
