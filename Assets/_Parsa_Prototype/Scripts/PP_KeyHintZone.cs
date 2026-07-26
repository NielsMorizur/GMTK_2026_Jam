using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Teaching prompt: a keycap that floats over the player while he stands
    /// inside an invisible rectangular zone. The zone is this object's
    /// authored position + Zone Size, drawn as a yellow wire box in the Scene
    /// view only. Optionally retires forever once the player performs a swap
    /// while the prompt is up.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PP_KeyHintZone : MonoBehaviour
    {
        [Tooltip("Width/height of the invisible trigger area around this object's position")]
        public Vector2 zoneSize = new Vector2(6f, 4f);
        [Tooltip("Stop showing forever once the player swaps while the prompt is visible")]
        public bool hideAfterSwap = true;

        [Header("Prompt look")]
        public float heightAbovePlayer = 1.5f;
        public float bobAmplitude = 0.12f;
        public float bobPeriod = 1.6f;
        public float fadeSpeed = 10f;

        Vector3 _zoneCenter;
        Player _player;
        SpriteRenderer _sr;
        float _alpha;
        AgeStage _seenAge;
        bool _used;

        void Awake()
        {
            _zoneCenter = transform.position;   // the sprite itself roams
            _sr = GetComponent<SpriteRenderer>();
            SetAlpha(0f);
        }

        void Update()
        {
            if (_player == null)
            {
                _player = FindAnyObjectByType<Player>();
                if (_player == null) return;
                _seenAge = _player.CurrentAge;
            }

            // a swap while the prompt was up means the lesson landed
            if (_player.CurrentAge != _seenAge)
            {
                if (hideAfterSwap && _alpha > 0.1f) _used = true;
                _seenAge = _player.CurrentAge;
            }

            Vector3 p = _player.transform.position;
            bool inside = Mathf.Abs(p.x - _zoneCenter.x) <= zoneSize.x * 0.5f
                          && Mathf.Abs(p.y - _zoneCenter.y) <= zoneSize.y * 0.5f;
            bool show = inside && !_used && !PP_DeathHand.AnyPlaying;

            _alpha = Mathf.MoveTowards(_alpha, show ? 1f : 0f,
                fadeSpeed * Time.deltaTime * 0.25f);
            SetAlpha(_alpha);

            if (_alpha <= 0f) return;
            float bob = Mathf.Sin(Time.time * Mathf.PI * 2f / Mathf.Max(0.2f, bobPeriod))
                        * bobAmplitude;
            transform.position = p + new Vector3(0f, heightAbovePlayer + bob, 0f);
        }

        void SetAlpha(float a)
        {
            var c = _sr.color;
            c.a = a;
            _sr.color = c;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.9f);
            Vector3 c = Application.isPlaying ? _zoneCenter : transform.position;
            Gizmos.DrawWireCube(c, new Vector3(zoneSize.x, zoneSize.y, 0.1f));
        }
    }
}
