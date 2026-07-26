using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ParsaProto
{
    /// <summary>
    /// Tints the Light2D on this object toward the current form's color,
    /// gliding on swaps. White Blend keeps some neutral light mixed in so
    /// the scene never goes fully monochrome.
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    public class PP_FormLight : MonoBehaviour
    {
        [Tooltip("0 = pure form color, 1 = pure white")]
        [Range(0f, 1f)] public float whiteBlend = 0.35f;
        [Tooltip("Seconds the tint takes to glide on a swap")]
        public float tintTime = 0.6f;
        [Tooltip("Form palette, age order: Elderly, Adult, Teen, Baby")]
        public Color[] formColors = new Color[]
        {
            new Color(0.7f, 0.7f, 0.75f, 1f),
            new Color(0.325f, 0.714f, 0.882f, 1f),
            new Color(0.953f, 0.529f, 0.267f, 1f),
            new Color(0.98f, 0.7f, 0.18f, 1f),
        };

        Light2D _light;
        Player _player;
        float _refindTimer;
        Color _current = Color.white;

        void Awake()
        {
            _light = GetComponent<Light2D>();
        }

        void Update()
        {
            if (_light == null) return;
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
            Color target = Color.Lerp(formColors[i], Color.white, whiteBlend);
            float k = 1f - Mathf.Exp(-3f * Time.deltaTime / Mathf.Max(0.05f, tintTime));
            _current = Color.Lerp(_current, target, k);
            _light.color = _current;
        }
    }
}
