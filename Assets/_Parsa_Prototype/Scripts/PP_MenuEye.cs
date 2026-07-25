using UnityEngine;
using UnityEngine.InputSystem;

namespace ParsaProto
{
    /// <summary>
    /// The menu eye: opens when a button is highlighted, tracks the cursor
    /// (mouse focus) or the selected button (keyboard focus), blinks now and
    /// then while awake, and dozes off again after a quiet spell.
    /// </summary>
    public class PP_MenuEye : MonoBehaviour
    {
        [Tooltip("The iris, moved inside the eye")]
        public RectTransform iris;
        [Tooltip("The closed-lid overlay")]
        public GameObject lid;
        [Tooltip("How far the iris may wander from center, in canvas pixels")]
        public float maxIrisOffset = 26f;
        public float trackSpeed = 14f;

        [Header("Behavior")]
        [Tooltip("Seconds after the last button contact before the eye closes")]
        public float closeDelay = 1.5f;
        [Tooltip("Seconds between idle blinks while open")]
        public float blinkIntervalMin = 2.5f;
        public float blinkIntervalMax = 4.5f;
        public float blinkDuration = 0.1f;

        Canvas _canvas;
        Vector2 _irisHome;
        Transform _lastFocus;
        float _blinkTimer;
        float _blinkUntil;

        void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            if (iris != null) _irisHome = iris.anchoredPosition;
            _blinkTimer = Random.Range(blinkIntervalMin, blinkIntervalMax);
        }

        void Update()
        {
            float now = Time.realtimeSinceStartup;
            var focus = PP_ButtonJuice.CurrentFocus;
            if (focus != null) _lastFocus = focus.transform;

            // awake while actually hovering, or briefly after the last contact
            bool hovering = focus != null && PP_ButtonJuice.FocusFromPointer;
            bool grace = _lastFocus != null
                         && now - PP_ButtonJuice.LastFocusEvent < closeDelay;
            bool open = hovering || (focus != null) || grace;

            // idle blinks while open
            if (open)
            {
                _blinkTimer -= Time.unscaledDeltaTime;
                if (_blinkTimer <= 0f)
                {
                    _blinkUntil = now + blinkDuration;
                    _blinkTimer = Random.Range(blinkIntervalMin, blinkIntervalMax);
                }
            }

            bool lidShown = !open || now < _blinkUntil;
            if (lid != null && lid.activeSelf != lidShown)
                lid.SetActive(lidShown);

            if (iris == null) return;

            Vector2 target = _irisHome;
            if (open)
            {
                Vector2 targetScreen;
                if (PP_ButtonJuice.FocusFromPointer && Mouse.current != null)
                    targetScreen = Mouse.current.position.ReadValue();
                else if (_lastFocus != null)
                    targetScreen = RectTransformUtility.WorldToScreenPoint(null, _lastFocus.position);
                else
                    targetScreen = RectTransformUtility.WorldToScreenPoint(null, transform.position);

                Vector2 eyeScreen = RectTransformUtility.WorldToScreenPoint(null, transform.position);
                float scale = _canvas != null ? _canvas.scaleFactor : 1f;
                Vector2 dir = (targetScreen - eyeScreen) / Mathf.Max(0.01f, scale);
                target = _irisHome + Vector2.ClampMagnitude(dir, maxIrisOffset);
            }

            iris.anchoredPosition = Vector2.Lerp(iris.anchoredPosition, target,
                1f - Mathf.Exp(-trackSpeed * Time.unscaledDeltaTime));
        }
    }
}
