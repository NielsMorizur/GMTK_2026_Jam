using System.Collections;
using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Celeste-style dash juice: ghost trail + hit-freeze + screen shake.
    /// Watches the Dash animation state, so it fires for whichever form can
    /// dash. Everything is tunable in the inspector; zero art required, the
    /// ghosts are snapshots of whatever sprite the player is wearing.
    /// </summary>
    public class PP_DashEffects : MonoBehaviour
    {
        [Header("Ghost trail")]
        [Tooltip("Ghost color at spawn; it blends toward the end tint as it fades")]
        public Color ghostStartTint = new Color(0.5f, 0.5f, 0.5f, 0.55f);
        public Color ghostEndTint = new Color(0f, 0f, 0f, 0f);
        [Tooltip("Seconds between ghost snapshots while dashing")]
        public float ghostInterval = 0.07f;
        [Tooltip("Seconds for each ghost to fade away")]
        public float ghostFadeTime = 0.18f;

        [Header("Hit freeze")]
        [Tooltip("Real-time seconds the game freezes when the dash starts. 0 = off")]
        public float freezeDuration = 0.05f;

        [Header("Screen shake")]
        [Tooltip("World-units strength of the camera shake. 0 = off")]
        public float shakeStrength = 0.12f;
        public float shakeDuration = 0.15f;

        [Header("Double jump")]
        [Tooltip("Freeze + shake when the double jump fires. 0 = off")]
        public float doubleJumpFreezeDuration = 0.04f;
        public float doubleJumpShakeStrength = 0.08f;
        public float doubleJumpShakeDuration = 0.12f;

        Animator _anim;
        SpriteRenderer _sprite;
        bool _wasDashing;
        bool _wasDoubleJump;
        float _ghostTimer;

        void Awake()
        {
            _anim = GetComponentInChildren<Animator>();
            _sprite = GetComponentInChildren<SpriteRenderer>();
        }

        void Update()
        {
            if (_anim == null || _sprite == null) return;

            var state = _anim.GetCurrentAnimatorStateInfo(0);

            bool doubleJumping = state.IsName("DoubleJump");
            if (doubleJumping && !_wasDoubleJump)
            {
                if (doubleJumpFreezeDuration > 0f)
                    StartCoroutine(HitFreeze(doubleJumpFreezeDuration));
                if (doubleJumpShakeStrength > 0f && Camera.main != null)
                    StartCoroutine(Shake(Camera.main.transform,
                        doubleJumpShakeStrength, doubleJumpShakeDuration));
            }
            _wasDoubleJump = doubleJumping;

            bool dashing = state.IsName("Dash");

            if (dashing && !_wasDashing)
            {
                _ghostTimer = 0f;
                if (freezeDuration > 0f)
                    StartCoroutine(HitFreeze(freezeDuration));
                if (shakeStrength > 0f && Camera.main != null)
                    StartCoroutine(Shake(Camera.main.transform, shakeStrength, shakeDuration));
            }

            if (dashing)
            {
                _ghostTimer -= Time.deltaTime;
                if (_ghostTimer <= 0f)
                {
                    SpawnGhost();
                    _ghostTimer = ghostInterval;
                }
            }

            _wasDashing = dashing;
        }

        void SpawnGhost()
        {
            var src = _sprite.transform;
            var go = new GameObject("PP_DashGhost");
            go.transform.SetPositionAndRotation(src.position, src.rotation);
            go.transform.localScale = src.lossyScale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite.sprite;
            sr.flipX = _sprite.flipX;
            sr.flipY = _sprite.flipY;
            sr.sortingLayerID = _sprite.sortingLayerID;
            sr.sortingOrder = _sprite.sortingOrder - 1;   // behind the player
            sr.color = ghostStartTint;

            var ghost = go.AddComponent<PP_SpriteGhost>();
            ghost.fadeTime = ghostFadeTime;
            ghost.startColor = ghostStartTint;
            ghost.endColor = ghostEndTint;
        }

        IEnumerator HitFreeze(float duration)
        {
            float prev = Time.timeScale;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = prev;
        }

        IEnumerator Shake(Transform cam, float strength, float duration)
        {
            // additive delta shake, so it plays nice with a moving camera
            Vector3 applied = Vector3.zero;
            float t = 0f;
            while (t < duration && cam != null)
            {
                t += Time.unscaledDeltaTime;
                float falloff = 1f - t / duration;
                Vector3 next = (Vector3)(Random.insideUnitCircle * strength * falloff);
                cam.position += next - applied;
                applied = next;
                yield return null;
            }
            if (cam != null)
                cam.position -= applied;
        }
    }

    /// <summary>One fading snapshot left behind by the dash.</summary>
    public class PP_SpriteGhost : MonoBehaviour
    {
        public float fadeTime = 0.18f;
        public Color startColor = new Color(0.5f, 0.5f, 0.5f, 0.55f);
        public Color endColor = new Color(0f, 0f, 0f, 0f);

        SpriteRenderer _sr;
        float _life;

        void Start()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            _life += Time.deltaTime;
            float t = _life / Mathf.Max(0.01f, fadeTime);
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            _sr.color = Color.Lerp(startColor, endColor, t);
        }
    }
}
