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
        [Tooltip("Seconds after the last button contact before the eye dozes off, rolled anew between these bounds each time")]
        public float closeDelayMin = 1f;
        public float closeDelayMax = 3.5f;
        [Tooltip("Seconds between idle blinks while open")]
        public float blinkIntervalMin = 2.5f;
        public float blinkIntervalMax = 4.5f;
        public float blinkDuration = 0.1f;

        [Header("Sound")]
        [Tooltip("Played on every idle blink")]
        public AudioClip blinkSfx;
        [Range(0f, 1f)] public float blinkVolume = 0.5f;
        [Tooltip("Random pitch spread per blink, +/- this much")]
        public float blinkPitchJitter = 0.08f;

        Canvas _canvas;
        Vector2 _irisHome;
        Transform _lastFocus;
        float _blinkTimer;
        float _blinkUntil;
        float _closeDelay;
        float _lastSeenFocusEvent = -1f;
        AudioSource _audio;

        void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            if (iris != null) _irisHome = iris.anchoredPosition;
            _blinkTimer = Random.Range(blinkIntervalMin, blinkIntervalMax);
            _closeDelay = Random.Range(closeDelayMin, closeDelayMax);

            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
        }

        void Update()
        {
            float now = Time.realtimeSinceStartup;
            var focus = PP_ButtonJuice.CurrentFocus;
            if (focus != null) _lastFocus = focus.transform;

            // every fresh contact rolls a new doze-off delay, so the eye
            // sometimes lingers and sometimes shuts quickly
            if (PP_ButtonJuice.LastFocusEvent != _lastSeenFocusEvent)
            {
                _lastSeenFocusEvent = PP_ButtonJuice.LastFocusEvent;
                _closeDelay = Random.Range(closeDelayMin, closeDelayMax);
            }

            // awake while actually hovering, or briefly after the last contact
            bool hovering = focus != null && PP_ButtonJuice.FocusFromPointer;
            bool grace = _lastFocus != null
                         && now - PP_ButtonJuice.LastFocusEvent < _closeDelay;
            bool open = hovering || (focus != null) || grace;

            // idle blinks while open
            if (open)
            {
                _blinkTimer -= Time.unscaledDeltaTime;
                if (_blinkTimer <= 0f)
                {
                    _blinkUntil = now + blinkDuration;
                    _blinkTimer = Random.Range(blinkIntervalMin, blinkIntervalMax);

                    if (blinkSfx != null && _audio != null)
                    {
                        _audio.pitch = 1f + Random.Range(-blinkPitchJitter, blinkPitchJitter);
                        _audio.PlayOneShot(blinkSfx, blinkVolume * PP_Settings.SfxVolume);
                    }
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
