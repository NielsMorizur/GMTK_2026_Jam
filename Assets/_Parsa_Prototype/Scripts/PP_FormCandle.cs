using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// A candle whose flame burns in the current form's color: particles and
    /// glow glide to the new color on every swap, so the level itself shows
    /// which form you are. Colors follow the shared form palette.
    /// </summary>
    public class PP_FormCandle : MonoBehaviour
    {
        [Tooltip("The flame particles")]
        public ParticleSystem flame;
        [Tooltip("Soft glow behind the flame; its authored alpha is kept")]
        public SpriteRenderer glow;
        [Tooltip("Optional 2D light, tinted along with the flame")]
        public UnityEngine.Rendering.Universal.Light2D light2d;
        [Tooltip("Form palette, age order: Elderly, Adult, Teen, Baby")]
        public Color[] formColors = new Color[]
        {
            new Color(0.7f, 0.7f, 0.75f, 1f),
            new Color(0.325f, 0.714f, 0.882f, 1f),
            new Color(0.953f, 0.529f, 0.267f, 1f),
            new Color(0.98f, 0.7f, 0.18f, 1f),
        };
        [Tooltip("Seconds the color takes to glide on a swap")]
        public float tintTime = 0.6f;

        Player _player;
        float _refindTimer;
        Color _current;
        float _glowAlpha = 0.5f;

        void Awake()
        {
            if (glow != null) _glowAlpha = glow.color.a;
            _current = formColors != null && formColors.Length > 0 ? formColors[0] : Color.white;
            Apply(_current);
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
            Color target = formColors[i];
            float k = 1f - Mathf.Exp(-3f * Time.deltaTime / Mathf.Max(0.05f, tintTime));
            _current = Color.Lerp(_current, target, k);
            Apply(_current);
        }

        void Apply(Color c)
        {
            if (flame != null)
            {
                var main = flame.main;
                main.startColor = new ParticleSystem.MinMaxGradient(
                    c, Color.Lerp(c, Color.white, 0.4f));
            }
            if (glow != null)
            {
                var g = c;
                g.a = _glowAlpha;
                glow.color = g;
            }
            if (light2d != null)
                light2d.color = Color.Lerp(c, Color.white, 0.2f);
        }
    }
}
