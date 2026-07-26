using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Nudges a SpriteRenderer's color toward the current form's color, by
    /// Strength, gliding on every swap. Authored alpha is always kept, so it
    /// works on overlays like the color wash without touching their opacity.
    /// Drop on anything that should quietly follow the form.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PP_FormTint : MonoBehaviour
    {
        [Tooltip("0 = untouched, 1 = fully the form color")]
        [Range(0f, 1f)] public float strength = 0.35f;
        [Tooltip("Seconds the tint takes to glide on a swap")]
        public float tintTime = 0.8f;
        [Tooltip("Form palette, age order: Elderly, Adult, Teen, Baby")]
        public Color[] formColors = new Color[]
        {
            new Color(0.7f, 0.7f, 0.75f, 1f),
            new Color(0.325f, 0.714f, 0.882f, 1f),
            new Color(0.953f, 0.529f, 0.267f, 1f),
            new Color(0.98f, 0.7f, 0.18f, 1f),
        };

        SpriteRenderer _sr;
        Color _base;
        Color _current;
        Player _player;
        float _refindTimer;

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _base = _sr.color;
            _current = _base;
        }

        void Update()
        {
            if (_player == null)
            {
                _refindTimer -= Time.unscaledDeltaTime;
                if (_refindTimer <= 0f)
                {
                    _player = FindAnyObjectByType<Player>();
                    _refindTimer = 0.5f;
                }
            }

            int i = _player != null ? Mathf.Clamp((int)_player.CurrentAge, 0, formColors.Length - 1) : 0;
            Color target = Color.Lerp(_base, formColors[i], strength);
            target.a = _base.a;

            float k = 1f - Mathf.Exp(-3f * Time.deltaTime / Mathf.Max(0.05f, tintTime));
            _current = Color.Lerp(_current, target, k);
            _current.a = _base.a;
            _sr.color = _current;
        }
    }
}
