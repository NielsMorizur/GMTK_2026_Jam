using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ParsaProto
{
    /// <summary>
    /// Full-screen fade wipe. Self-bootstraps on game start and persists
    /// across scenes: every scene opens from black, PP_ScreenFade.LoadScene
    /// fades to black before switching, and Wipe covers soft resets (deaths)
    /// with a quick out-and-in. All fades run on real time.
    /// </summary>
    public class PP_ScreenFade : MonoBehaviour
    {
        public static float fadeOutTime = 0.25f;
        public static float fadeInTime = 0.35f;

        static PP_ScreenFade _i;
        Image _img;
        float _alpha;
        float _from, _to, _t;
        float _dur = -1f;
        System.Action _onDone;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (_i != null) return;
            var root = new GameObject("PP_ScreenFade");
            DontDestroyOnLoad(root);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            var imgGo = new GameObject("Black");
            imgGo.transform.SetParent(root.transform, false);
            var img = imgGo.AddComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _i = root.AddComponent<PP_ScreenFade>();
            _i._img = img;
            _i.StartFade(1f, 0f, fadeInTime, null);   // first scene opens from black

            SceneManager.sceneLoaded += (s, m) =>
            {
                if (_i == null) return;
                if (_cutNextArrival)
                {
                    _cutNextArrival = false;
                    _i._dur = -1f;
                    _i.Apply(0f);
                    return;
                }
                _i.StartFade(1f, 0f, fadeInTime, null);
            };
        }

        /// <summary>Begin a slow fade to black over the given seconds (deaths
        /// start this as the presentation starts).</summary>
        public static void FadeOut(float duration)
        {
            if (_i != null) _i.StartFade(_i._alpha, 1f, duration, null);
        }

        /// <summary>Fade to black, then load; the arrival fades back in.</summary>
        public static void LoadScene(string scene)
        {
            if (_i == null) { SceneManager.LoadScene(scene); return; }
            _i.StartFade(_i._alpha, 1f, fadeOutTime, () => SceneManager.LoadScene(scene));
        }

        static bool _cutNextArrival;

        /// <summary>Hard cut: no fade out, no arrival fade - one frame it's
        /// the old scene, the next it's the new one.</summary>
        public static void CutTo(string scene)
        {
            _cutNextArrival = true;
            SceneManager.LoadScene(scene);
        }

        /// <summary>Fade to black, run the action under cover, fade back in.
        /// Used around death respawns so the snap back is never visible.</summary>
        public static void Wipe(System.Action atBlack)
        {
            if (_i == null) { atBlack?.Invoke(); return; }
            _i.StartFade(_i._alpha, 1f, fadeOutTime, () =>
            {
                atBlack?.Invoke();
                _i.StartFade(1f, 0f, fadeInTime, null);
            });
        }

        void StartFade(float from, float to, float dur, System.Action onDone)
        {
            _from = from;
            _to = to;
            _dur = Mathf.Max(0.01f, dur);
            _t = 0f;
            _onDone = onDone;
            Apply(from);
        }

        void Update()
        {
            if (_dur <= 0f) return;
            _t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(_t / _dur);
            Apply(Mathf.Lerp(_from, _to, k));
            if (k >= 1f)
            {
                _dur = -1f;
                var cb = _onDone;
                _onDone = null;
                cb?.Invoke();
            }
        }

        void Apply(float a)
        {
            _alpha = a;
            if (_img == null) return;
            var c = _img.color;
            c.a = a;
            _img.color = c;
            _img.enabled = a > 0.001f;
        }
    }
}
