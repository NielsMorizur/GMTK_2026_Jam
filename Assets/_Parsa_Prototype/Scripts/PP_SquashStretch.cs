using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Squash and stretch for the player. Scales ONLY the sprite child, the
    /// collider and physics are untouched. A little spring drives the scale
    /// back to normal with a natural overshoot.
    ///
    /// Triggers: jump (stretch), landing (squash scaled by fall speed),
    /// gas double jump (big pop), dash (stretch along the dash), grapple
    /// release (launch stretch).
    /// </summary>
    public class PP_SquashStretch : MonoBehaviour
    {
        [Header("Spring feel")]
        [Tooltip("How fast the sprite pulls back to normal shape")]
        public float springStiffness = 220f;
        [Tooltip("Higher = less wobble after the pull-back")]
        public float springDamping = 14f;

        [Header("Impulses (x = width, y = height)")]
        public Vector2 jumpStretch = new Vector2(0.85f, 1.15f);
        public Vector2 gasJumpStretch = new Vector2(0.72f, 1.32f);
        public Vector2 dashStretch = new Vector2(1.3f, 0.75f);
        public Vector2 grappleReleaseStretch = new Vector2(0.85f, 1.2f);

        [Header("Landing")]
        public Vector2 landSquashLight = new Vector2(1.08f, 0.92f);
        public Vector2 landSquashHeavy = new Vector2(1.35f, 0.65f);
        [Tooltip("Fall speed that produces the full heavy squash")]
        public float fallSpeedForFullSquash = 18f;

        Player _player;
        Animator _anim;
        Transform _sprite;
        Vector3 _baseScale;

        Vector2 _scale = Vector2.one;
        Vector2 _scaleVel;

        bool _wasGrounded = true;
        bool _wasDashing;
        bool _wasGrappling;
        bool _wasDoubleJump;
        float _prevVy;

        void Awake()
        {
            _player = GetComponent<Player>();
            _anim = GetComponentInChildren<Animator>();
            if (_anim != null)
            {
                _sprite = _anim.transform;
                _baseScale = _sprite.localScale;
            }
        }

        void Update()
        {
            if (_player == null || _sprite == null) return;

            bool grounded = _player.groundDetected;
            float vy = _player.rb != null ? _player.rb.linearVelocity.y : 0f;

            // jump: left the ground going up
            if (!grounded && _wasGrounded && vy > 0.01f)
                Impulse(jumpStretch);

            // landing: squash by how hard he came down
            if (grounded && !_wasGrounded)
            {
                float k = Mathf.Clamp01(Mathf.Abs(Mathf.Min(_prevVy, 0f)) / Mathf.Max(1f, fallSpeedForFullSquash));
                Impulse(Vector2.Lerp(landSquashLight, landSquashHeavy, k));
            }
            _wasGrounded = grounded;
            _prevVy = vy;

            // gas double jump: big pop
            if (_anim != null)
            {
                bool dj = _anim.GetCurrentAnimatorStateInfo(0).IsName("DoubleJump");
                if (dj && !_wasDoubleJump)
                    Impulse(gasJumpStretch);
                _wasDoubleJump = dj;
            }

            // dash: stretch along the movement
            bool dashing = _player.IsDashing;
            if (dashing && !_wasDashing)
                Impulse(dashStretch);
            _wasDashing = dashing;

            // grapple release: launch stretch
            bool grappling = _player.IsGrappling;
            if (!grappling && _wasGrappling)
                Impulse(grappleReleaseStretch);
            _wasGrappling = grappling;

            // spring the shape back to normal with a bit of overshoot
            float dt = Time.deltaTime;
            if (dt > 0f)
            {
                _scaleVel += (Vector2.one - _scale) * (springStiffness * dt);
                _scaleVel *= 1f / (1f + springDamping * dt);
                _scale += _scaleVel * dt;
            }

            _sprite.localScale = new Vector3(
                _baseScale.x * _scale.x, _baseScale.y * _scale.y, _baseScale.z);
        }

        public void Impulse(Vector2 target)
        {
            _scale = target;
            _scaleVel = Vector2.zero;
        }
    }
}
