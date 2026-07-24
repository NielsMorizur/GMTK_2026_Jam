using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// A block the adult's grappling gun can attach to. Glows with a soft
    /// pulse while the player can actually grapple it (adult form, in range).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PP_GrapplePoint : MonoBehaviour
    {
        [Tooltip("Color while out of range or wrong form.")]
        public Color baseColor = new Color(0.62f, 0.47f, 0.16f, 1f);

        [Tooltip("Color while grappleable; pulses slightly.")]
        public Color glowColor = new Color(1f, 0.85f, 0.35f, 1f);

        [Tooltip("Pulse speed while glowing.")]
        public float pulseSpeed = 3f;

        SpriteRenderer _sprite;
        PP_Grapple _grapple;

        void Awake()
        {
            _sprite = GetComponent<SpriteRenderer>();
        }

        void Start()
        {
            _grapple = Object.FindAnyObjectByType<PP_Grapple>();
        }

        void Update()
        {
            if (_sprite == null) return;

            bool targeted = _grapple != null && _grapple.CurrentTarget == this;

            if (targeted)
            {
                float t = Mathf.PingPong(Time.time * pulseSpeed, 1f) * 0.35f;
                _sprite.color = Color.Lerp(glowColor, Color.white, t);
            }
            else
            {
                _sprite.color = baseColor;
            }
        }
    }
}
