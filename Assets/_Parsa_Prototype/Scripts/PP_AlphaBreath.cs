using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Very slow alpha pulse on a sprite (the vignette breathing). Subtle by
    /// design; amplitude is how far the alpha strays from its authored value.
    /// </summary>
    public class PP_AlphaBreath : MonoBehaviour
    {
        [Range(0f, 0.5f)] public float amplitude = 0.06f;
        public float period = 7f;

        SpriteRenderer _sr;
        float _baseAlpha;

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _baseAlpha = _sr.color.a;
        }

        void Update()
        {
            if (_sr == null) return;
            float w = Mathf.PI * 2f / Mathf.Max(0.5f, period);
            var c = _sr.color;
            c.a = Mathf.Clamp01(_baseAlpha + Mathf.Sin(Time.unscaledTime * w) * amplitude);
            _sr.color = c;
        }
    }
}
