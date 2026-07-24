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

        SpriteRenderer _sr;
        Color _original;

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _original = _sr.color;
        }

        void Update()
        {
            if (_sr == null) return;

            bool available = GameManager.Instance != null
                             && GameManager.Instance.GetLevel() == levelName;
            _sr.color = available ? availableColor : _original;
        }
    }
}
