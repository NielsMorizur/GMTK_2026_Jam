using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Tints a hub door while it's the currently available level, so the
    /// player can see where to go. Reads GameManager progress; anything not
    /// yet (or no longer) available keeps its original look.
    /// </summary>
    public class PP_DoorGlow : MonoBehaviour
    {
        [Tooltip("The level this door leads to, e.g. Level1")]
        public string levelName = "Level1";
        public Color availableColor = new Color(0.35f, 0.9f, 0.45f, 1f);

        [Tooltip("Soft halo behind the door, shown while available. Its authored alpha is the maximum.")]
        public SpriteRenderer backGlow;
        [Tooltip("How much the halo breathes, 0 = steady")]
        [Range(0f, 1f)] public float glowPulseAmount = 0.3f;
        public float glowPulsePeriod = 2.2f;
        [Tooltip("Level exits: glow even when no GameManager exists (direct scene testing)")]
        public bool assumeAvailableWithoutManager = false;

        SpriteRenderer _sr;
        Color _original;
        float _glowMax = -1f;
        float _fade;

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _original = _sr.color;
        }

        void Update()
        {
            if (_sr == null) return;

            bool available = GameManager.Instance != null
                ? GameManager.Instance.GetLevel() == levelName
                : assumeAvailableWithoutManager;
            _sr.color = available ? availableColor : _original;

            if (backGlow == null) return;
            if (_glowMax < 0f) _glowMax = backGlow.color.a;

            _fade = Mathf.MoveTowards(_fade, available ? 1f : 0f, Time.deltaTime * 2f);
            float breathe = 1f - glowPulseAmount * 0.5f
                * (1f + Mathf.Sin(Time.time * Mathf.PI * 2f / Mathf.Max(0.2f, glowPulsePeriod)));
            var c = backGlow.color;
            c.a = _glowMax * _fade * breathe;
            backGlow.color = c;
        }
    }
}
