using System.Collections;
using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Form-change drama: a particle burst in the incoming form's color, a
    /// small camera zoom punch, and a brief slow-mo beat, fired on every age
    /// swap. Set zoom punch or slow-mo scale to 1 to turn that piece off.
    /// </summary>
    public class PP_FormSwapEffects : MonoBehaviour
    {
        [Header("Colored burst")]
        public GameObject burstPrefab;
        [Tooltip("Burst color per incoming form: Elderly, Adult, Teen, Baby")]
        public Color[] formColors = new Color[]
        {
            new Color(0.7f, 0.7f, 0.75f, 0.9f),
            new Color(0.325f, 0.714f, 0.882f, 0.9f),
            new Color(0.953f, 0.529f, 0.267f, 0.9f),
            new Color(0.98f, 0.7f, 0.18f, 0.9f),
        };
        public float burstScale = 1.3f;

        [Header("Swirl + poof")]
        [Tooltip("Seconds the sprite swirls before the poof")]
        public float swirlDuration = 0.22f;
        public float swirlTurns = 1f;
        [Tooltip("Squash-stretch pop as he lands in the new form")]
        public Vector2 swapPop = new Vector2(1.25f, 1.25f);

        [Header("Slow-mo beat")]
        [Tooltip("Game speed during the beat; 1 = off")]
        public float slowMoScale = 0.3f;
        [Tooltip("Real-time seconds the beat lasts")]
        public float slowMoDuration = 0.15f;

        Player _player;
        Transform _sprite;
        AgeStage _lastAge;
        bool _init;
        bool _swirling;
        static bool _slowMo;

        void Awake()
        {
            _player = GetComponent<Player>();
            var anim = GetComponentInChildren<Animator>();
            _sprite = anim != null ? anim.transform : null;
        }

        void Update()
        {
            if (_player == null) return;

            var age = _player.CurrentAge;
            if (!_init)
            {
                _lastAge = age;
                _init = true;
                return;
            }

            if (age == _lastAge) return;

            // the level only ever ages him DOWN (enum forward); any backward
            // change is a reset rewinding to the start, never a real swap
            bool agedDown = (int)age > (int)_lastAge;
            _lastAge = age;
            if (!agedDown) return;

            Fire(age);
        }

        /// <summary>Adopt the current age without firing (level resets).</summary>
        public void ResyncAge()
        {
            if (_player != null)
            {
                _lastAge = _player.CurrentAge;
                _init = true;
            }
        }

        void Fire(AgeStage age)
        {
            if (slowMoScale < 1f && !_slowMo)
                StartCoroutine(SlowMo());

            StartCoroutine(SwirlAndPoof(age));
        }

        IEnumerator SwirlAndPoof(AgeStage age)
        {
            // tiny swirl...
            if (_sprite != null && swirlDuration > 0f && !_swirling)
            {
                _swirling = true;
                float t = 0f;
                while (t < swirlDuration)
                {
                    t += Time.unscaledDeltaTime;
                    float spin = Mathf.Lerp(0f, 360f * swirlTurns,
                        Mathf.Clamp01(t / swirlDuration));
                    _sprite.localRotation = Quaternion.Euler(0f, 0f, spin);
                    yield return null;
                }
                _sprite.localRotation = Quaternion.identity;
                _swirling = false;
            }

            // ...then pop into the new form with a poof
            if (burstPrefab != null)
            {
                var go = Instantiate(burstPrefab, transform.position, Quaternion.identity);
                go.transform.localScale = Vector3.one * burstScale;

                var ps = go.GetComponent<ParticleSystem>();
                int i = (int)age;
                if (ps != null && formColors != null && i >= 0 && i < formColors.Length)
                {
                    var main = ps.main;
                    main.startColor = formColors[i];
                }
            }

            var squash = GetComponent<PP_SquashStretch>();
            if (squash != null)
                squash.Impulse(swapPop);
        }

        IEnumerator SlowMo()
        {
            // same rule as the hit-freeze: never stack, always end at full
            // speed (or stay paused if the menu is up)
            if (PP_PauseMenu.IsOpen) yield break;
            _slowMo = true;
            Time.timeScale = slowMoScale;
            yield return new WaitForSecondsRealtime(slowMoDuration);
            Time.timeScale = PP_PauseMenu.IsOpen ? 0f : 1f;
            _slowMo = false;
        }
    }
}
