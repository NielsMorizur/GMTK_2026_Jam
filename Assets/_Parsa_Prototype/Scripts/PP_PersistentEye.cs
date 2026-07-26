using UnityEngine;
using UnityEngine.SceneManagement;

namespace ParsaProto
{
    /// <summary>
    /// The special eye: in the menu it behaves like any menu eye, but when
    /// Enter cuts to the intro it survives on its own overlay canvas, stays
    /// open, and watches whatever the intro director points it at (each
    /// section hands it a target). It dies once the game proper starts.
    /// </summary>
    [RequireComponent(typeof(PP_MenuEye))]
    public class PP_PersistentEye : MonoBehaviour
    {
        public static PP_PersistentEye Instance { get; private set; }

        static Transform _watchTarget;
        /// <summary>Director hook: what the eye should look at now.</summary>
        public static void Watch(Transform t) => _watchTarget = t;

        [Tooltip("Scenes it survives; anywhere else it removes itself")]
        public string[] scenes = { "MainMenu", "Intro", "Intro (new)" };
        public float trackSpeed = 8f;
        public float blinkIntervalMin = 2.5f;
        public float blinkIntervalMax = 4.5f;
        public float blinkDuration = 0.1f;

        PP_MenuEye _menuEye;
        RectTransform _iris;
        GameObject _lid;
        float _maxOffset = 26f;
        Vector2 _irisHome;
        Canvas _canvas;
        bool _cinematic;
        float _blinkTimer;
        float _blinkUntil;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _menuEye = GetComponent<PP_MenuEye>();
            _iris = _menuEye != null ? _menuEye.iris : null;
            _lid = _menuEye != null ? _menuEye.lid : null;
            if (_menuEye != null) _maxOffset = _menuEye.maxIrisOffset;
            if (_iris != null) _irisHome = _iris.anchoredPosition;
            _blinkTimer = Random.Range(blinkIntervalMin, blinkIntervalMax);

            // its own persistent overlay canvas, keeping the screen position
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, transform.position);
            var root = new GameObject("PP_PersistentEyeCanvas");
            _canvas = root.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 500;
            DontDestroyOnLoad(root);
            transform.SetParent(root.transform, false);
            transform.position = new Vector3(screen.x, screen.y, 0f);

            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        void OnSceneChanged(Scene from, Scene to)
        {
            foreach (var s in scenes)
            {
                if (to.name != s) continue;
                if (to.name != "MainMenu") EnterCinematic();
                return;
            }
            Destroy(gameObject);
        }

        void EnterCinematic()
        {
            _cinematic = true;
            _watchTarget = null;
            if (_menuEye != null) _menuEye.enabled = false;   // the intro drives now
        }

        void Update()
        {
            if (!_cinematic) return;   // in the menu, PP_MenuEye still rules

            float now = Time.realtimeSinceStartup;
            _blinkTimer -= Time.unscaledDeltaTime;
            if (_blinkTimer <= 0f)
            {
                _blinkUntil = now + blinkDuration;
                _blinkTimer = Random.Range(blinkIntervalMin, blinkIntervalMax);
            }
            bool lidShown = now < _blinkUntil;
            if (_lid != null && _lid.activeSelf != lidShown) _lid.SetActive(lidShown);

            if (_iris == null) return;
            Vector2 target = _irisHome;
            if (_watchTarget != null && Camera.main != null)
            {
                Vector2 ts = Camera.main.WorldToScreenPoint(_watchTarget.position);
                Vector2 es = RectTransformUtility.WorldToScreenPoint(null, transform.position);
                float scale = _canvas != null ? Mathf.Max(0.01f, _canvas.scaleFactor) : 1f;
                target = _irisHome + Vector2.ClampMagnitude((ts - es) / scale, _maxOffset);
            }
            _iris.anchoredPosition = Vector2.Lerp(_iris.anchoredPosition, target,
                1f - Mathf.Exp(-trackSpeed * Time.unscaledDeltaTime));
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            SceneManager.activeSceneChanged -= OnSceneChanged;
            if (_canvas != null && _canvas.gameObject != null
                && transform.parent == _canvas.transform)
                Destroy(_canvas.gameObject);
        }
    }
}
