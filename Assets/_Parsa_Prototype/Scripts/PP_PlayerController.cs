using UnityEngine;
using UnityEngine.InputSystem;

namespace ParsaProto
{
    /// <summary>
    /// 2D platformer controller for the GMTK jam prototype.
    /// Good-feel movement: acceleration/friction, coyote time, jump buffer,
    /// variable jump height. Reads the New Input System directly off the
    /// keyboard so it needs no shared .inputactions asset.
    /// Unity 6: uses Rigidbody2D.linearVelocity.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PP_PlayerController : MonoBehaviour
    {
        [Header("Move")]
        [Tooltip("Top horizontal speed, units/sec.")]
        public float moveSpeed = 8f;
        [Tooltip("How fast we reach top speed on the ground.")]
        public float groundAccel = 90f;
        [Tooltip("How fast we stop on the ground.")]
        public float groundDecel = 110f;
        [Tooltip("Air control multiplier (0 = no control, 1 = full).")]
        [Range(0f, 1f)] public float airControl = 0.65f;

        [Header("Jump")]
        [Tooltip("How many blocks (tiles) high the jump reaches. 1 block = 1 world unit.")]
        public int jumpHeightBlocks = 3;
        [Tooltip("Extra height above the blocks so the jump clears the ledge instead of grazing it.")]
        public float jumpClearance = 0.35f;
        [Tooltip("Gravity while rising.")]
        public float gravityUp = 45f;
        [Tooltip("Gravity while falling (higher = snappier).")]
        public float gravityDown = 70f;
        [Tooltip("Extra gravity when jump released early (variable height).")]
        public float jumpCutMultiplier = 3f;
        [Tooltip("Clamp on fall speed.")]
        public float maxFallSpeed = 24f;

        [Header("Feel")]
        [Tooltip("Grace window to still jump just after leaving a ledge.")]
        public float coyoteTime = 0.10f;
        [Tooltip("Window to buffer a jump pressed just before landing.")]
        public float jumpBufferTime = 0.10f;

        [Header("Air Jump (old man's fart jump)")]
        [Tooltip("Enabled per form by PP_AgeLoop (old man only).")]
        public bool airJumpEnabled = false;
        [Tooltip("Height of the mid-air boost, in blocks. Weaker than a real jump.")]
        public float airJumpHeightBlocks = 1.2f;

        [Header("Dash")]
        [Tooltip("Enabled per form by PP_AgeLoop (teen only).")]
        public bool dashEnabled = false;
        [Tooltip("Dash speed, units/sec.")]
        public float dashSpeed = 18f;
        [Tooltip("How long the dash lasts.")]
        public float dashDuration = 0.15f;
        [Tooltip("Delay before you can dash again.")]
        public float dashCooldown = 0.35f;

        [Header("Ground Check")]
        [Tooltip("Probe box under the feet.")]
        public Vector2 groundCheckSize = new Vector2(0.6f, 0.12f);
        [Tooltip("How far below the collider to probe.")]
        public float groundCheckDistance = 0.06f;
        public LayerMask groundLayers = ~0;

        Rigidbody2D _rb;
        Collider2D _col;

        float _moveInput;
        bool _jumpHeld;

        float _coyoteCounter;
        float _jumpBufferCounter;
        bool _jumpQueued;
        bool _airJumpUsed;

        float _dashTimer;
        float _dashCooldownTimer;
        bool _dashQueued;
        bool _dashUsedInAir;
        int _facing = 1;

        public int Facing => _facing;
        public float MoveInput => _moveInput;
        public bool IsGrounded => _isGrounded;

        /// <summary>True while something else (grapple swing) owns the body.</summary>
        [System.NonSerialized] public bool controlSuspended;

        /// <summary>Ready-gate: physics runs but all input is ignored.</summary>
        [System.NonSerialized] public bool inputLocked;
        bool _isGrounded;
        bool _jumping;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<Collider2D>();
            _rb.gravityScale = 0f;              // we drive gravity manually for feel
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        void Update()
        {
            // --- input (New Input System, keyboard direct) ---
            var kb = Keyboard.current;
            if (kb == null) { _moveInput = 0f; return; }
            if (inputLocked) { _moveInput = 0f; _jumpHeld = false; return; }

            float left = (kb.aKey.isPressed || kb.leftArrowKey.isPressed) ? 1f : 0f;
            float right = (kb.dKey.isPressed || kb.rightArrowKey.isPressed) ? 1f : 0f;
            _moveInput = right - left;

            bool jumpPressed = kb.spaceKey.wasPressedThisFrame
                               || kb.wKey.wasPressedThisFrame
                               || kb.upArrowKey.wasPressedThisFrame;
            _jumpHeld = kb.spaceKey.isPressed || kb.wKey.isPressed || kb.upArrowKey.isPressed;

            if (jumpPressed)
            {
                _jumpBufferCounter = jumpBufferTime;
                _jumpQueued = true;
            }

            if (_moveInput > 0.01f) _facing = 1;
            else if (_moveInput < -0.01f) _facing = -1;

            if (kb.leftShiftKey.wasPressedThisFrame) _dashQueued = true;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            _isGrounded = GroundCheck();

            // timers
            _coyoteCounter = _isGrounded ? coyoteTime : _coyoteCounter - dt;
            _jumpBufferCounter -= dt;
            _dashCooldownTimer -= dt;
            if (_isGrounded)
            {
                _dashUsedInAir = false;
                _airJumpUsed = false;
            }

            if (controlSuspended)
            {
                _dashQueued = false;
                _jumpQueued = false;
                return;      // grapple owns velocity and gravity right now
            }

            // dash overrides everything while active
            if (_dashTimer > 0f)
            {
                _dashTimer -= dt;
                _rb.linearVelocity = new Vector2(_facing * dashSpeed, 0f);
                _dashQueued = false;
                return;
            }

            if (_dashQueued)
            {
                _dashQueued = false;
                if (dashEnabled && _dashCooldownTimer <= 0f && (_isGrounded || !_dashUsedInAir))
                {
                    _dashTimer = dashDuration;
                    _dashCooldownTimer = dashDuration + dashCooldown;
                    if (!_isGrounded) _dashUsedInAir = true;
                    _rb.linearVelocity = new Vector2(_facing * dashSpeed, 0f);
                    return;
                }
            }

            HandleHorizontal(dt);
            HandleJump();
            HandleGravity(dt);
        }

        void HandleHorizontal(float dt)
        {
            Vector2 v = _rb.linearVelocity;
            float target = _moveInput * moveSpeed;
            bool accelerating = Mathf.Abs(target) > 0.01f;
            float rate = accelerating ? groundAccel : groundDecel;
            if (!_isGrounded) rate *= airControl;
            v.x = Mathf.MoveTowards(v.x, target, rate * dt);
            _rb.linearVelocity = v;
        }

        void HandleJump()
        {
            if (_jumpBufferCounter > 0f && _coyoteCounter > 0f)
            {
                // v = sqrt(2 * g * h) using the rising gravity;
                // h = blocks + clearance so an N-block ledge is actually reachable
                float h = jumpHeightBlocks + jumpClearance;
                float jumpVelocity = Mathf.Sqrt(2f * gravityUp * h);
                Vector2 v = _rb.linearVelocity;
                v.y = jumpVelocity;
                _rb.linearVelocity = v;

                _jumping = true;
                _jumpBufferCounter = 0f;
                _coyoteCounter = 0f;
            }
            else if (_jumpQueued && !_isGrounded && airJumpEnabled
                     && !_airJumpUsed && _coyoteCounter <= 0f)
            {
                // fart jump: one weaker boost per airtime
                float v0 = Mathf.Sqrt(2f * gravityUp * airJumpHeightBlocks);
                Vector2 v = _rb.linearVelocity;
                v.y = v0;
                _rb.linearVelocity = v;

                _jumping = true;
                _airJumpUsed = true;
                _jumpBufferCounter = 0f;
            }

            _jumpQueued = false;
        }

        void HandleGravity(float dt)
        {
            Vector2 v = _rb.linearVelocity;

            float g = v.y > 0f ? gravityUp : gravityDown;

            // variable height: cut the rise short when jump released
            if (_jumping && v.y > 0f && !_jumpHeld)
                g *= jumpCutMultiplier;

            if (v.y <= 0f) _jumping = false;

            v.y -= g * dt;
            if (v.y < -maxFallSpeed) v.y = -maxFallSpeed;

            _rb.linearVelocity = v;
        }

        bool GroundCheck()
        {
            if (_col == null) return false;

            // Probe a thin box just under the feet. No dedicated ground layer
            // (that lives in shared ProjectSettings), so scan groundLayers and
            // ignore our own collider / body.
            Bounds b = _col.bounds;
            Vector2 center = new Vector2(b.center.x, b.min.y - groundCheckDistance * 0.5f);
            Vector2 size = new Vector2(groundCheckSize.x, groundCheckSize.y + groundCheckDistance);

            Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, groundLayers);
            foreach (var h in hits)
            {
                if (h == null || h == _col || h.isTrigger) continue;
                if (h.attachedRigidbody != null && h.attachedRigidbody == _rb) continue;
                // only count it as ground if we're not moving up through it
                if (_rb.linearVelocity.y <= 0.01f) return true;
            }
            return false;
        }

        void OnDrawGizmosSelected()
        {
            var col = GetComponent<Collider2D>();
            if (col == null) return;
            Bounds b = col.bounds;
            Vector2 center = new Vector2(b.center.x, b.min.y - groundCheckDistance * 0.5f);
            Vector2 size = new Vector2(groundCheckSize.x, groundCheckSize.y + groundCheckDistance);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(center, new Vector3(size.x, size.y, 0f));
        }
    }
}
