using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Hub doorway into one level. Locked (gray) until the previous level is
    /// complete, then green; touch it to enter its level.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PP_HubPortal : MonoBehaviour
    {
        public PP_LevelManager manager;
        [Tooltip("Which level this portal leads to (0-based).")]
        public int levelIndex;

        [Tooltip("Ignore progression: portal is always open (playground).")]
        public bool alwaysUnlocked = false;

        public Color lockedColor = new Color(0.35f, 0.38f, 0.42f, 1f);
        public Color unlockedColor = new Color(0.19215687f, 0.79607844f, 0.4745098f, 1f);

        SpriteRenderer _sprite;

        void Awake()
        {
            _sprite = GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            if (manager != null && _sprite != null)
                _sprite.color = (alwaysUnlocked || manager.IsUnlocked(levelIndex)) ? unlockedColor : lockedColor;
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (manager == null || !manager.InHub) return;
            if (!alwaysUnlocked && !manager.IsUnlocked(levelIndex)) return;
            if (other.GetComponent<PP_AgeLoop>() == null) return;
            manager.EnterLevel(levelIndex);
        }
    }
}
