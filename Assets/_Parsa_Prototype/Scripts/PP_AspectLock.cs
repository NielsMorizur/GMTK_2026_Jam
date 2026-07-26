using UnityEngine;
using UnityEngine.SceneManagement;

namespace ParsaProto
{
    /// <summary>
    /// Locks every scene camera to a 16:9 frame whatever shape the window is.
    /// Wider windows get pillarbox bars, narrower ones letterbox bars, so the
    /// player always sees exactly the framing the level was built in and
    /// never the off-stage area beside it. Boots itself, no setup needed.
    /// </summary>
    public class PP_AspectLock : MonoBehaviour
    {
        /// <summary>The shape the game is authored in.</summary>
        public const float TargetAspect = 16f / 9f;

        static PP_AspectLock _i;
        int _w, _h;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (_i != null) return;
            var go = new GameObject("PP_AspectLock");
            DontDestroyOnLoad(go);
            _i = go.AddComponent<PP_AspectLock>();

            // the bars need something clearing them to black every frame,
            // or a browser canvas can smear whatever was there before
            var barsGo = new GameObject("PP_LetterboxClear");
            barsGo.transform.SetParent(go.transform, false);
            var bars = barsGo.AddComponent<Camera>();
            bars.clearFlags = CameraClearFlags.SolidColor;
            bars.backgroundColor = Color.black;
            bars.cullingMask = 0;
            bars.depth = -100f;
            bars.orthographic = true;
            bars.allowMSAA = false;
            bars.allowHDR = false;

            _i.Apply();
            SceneManager.sceneLoaded += (s, m) => { if (_i != null) _i.Apply(); };
        }

        void Update()
        {
            // browser windows resize freely; re-frame only when they do
            if (Screen.width == _w && Screen.height == _h) return;
            Apply();
        }

        void Apply()
        {
            _w = Screen.width;
            _h = Screen.height;
            if (_w <= 0 || _h <= 0) return;

            float window = (float)_w / _h;
            Rect rect;
            if (window > TargetAspect)          // too wide: bars left and right
            {
                float k = TargetAspect / window;
                rect = new Rect((1f - k) * 0.5f, 0f, k, 1f);
            }
            else                                 // too tall: bars top and bottom
            {
                float k = window / TargetAspect;
                rect = new Rect(0f, (1f - k) * 0.5f, 1f, k);
            }

            var cams = FindObjectsByType<Camera>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var cam in cams)
            {
                // render-texture cameras (the backdrop blur rig) own their
                // own framing and must be left alone
                if (cam.targetTexture != null) continue;
                // the bar-clearing camera stays full screen on purpose
                if (cam.gameObject.name == "PP_LetterboxClear") continue;
                cam.rect = rect;
            }
        }
    }
}
