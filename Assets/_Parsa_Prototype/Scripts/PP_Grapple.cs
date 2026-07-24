using UnityEngine;
using UnityEngine.InputSystem;

namespace ParsaProto
{
    /// <summary>
    /// The adult's grappling gun. While airborne, hold Space to attach to the
    /// current target point (the glowing one) and swing; release Space to let
    /// go with momentum. Target = nearest in-range point in the facing
    /// direction, falling back to nearest overall. Move keys pump the swing.
    /// Enabled per form by PP_AgeLoop.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PP_PlayerController))]
    public class PP_Grapple : MonoBehaviour
    {
        [Tooltip("Max distance from the player to a grapple point.")]
        public float range = 6f;

        [Tooltip("Gravity scale while swinging (normal control is suspended).")]
        public float swingGravityScale = 3.5f;

        [Tooltip("Horizontal force from move keys while swinging.")]
        public float pumpForce = 30f;

        [Tooltip("Reel-in speed while holding W/Up during a swing, units/sec.")]
        public float reelSpeed = 4f;

        [Tooltip("How close the reel can pull you to the point.")]
        public float minReelDistance = 0.6f;

        public float ropeWidth = 0.08f;
        public Color ropeColor = new Color(0.2f, 0.2f, 0.22f, 1f);

        /// <summary>Set per form by PP_AgeLoop (adult only).</summary>
        [System.NonSerialized] public bool grappleEnabled;

        Rigidbody2D _rb;
        PP_PlayerController _ctrl;
        DistanceJoint2D _joint;
        LineRenderer _rope;
        PP_GrapplePoint _target;
        PP_GrapplePoint _candidate;

        public bool IsSwinging => _joint != null;

        /// <summary>The point Space would grab right now (or the one attached). Glow follows this.</summary>
        public PP_GrapplePoint CurrentTarget => IsSwinging ? _target : _candidate;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _ctrl = GetComponent<PP_PlayerController>();

            _rope = gameObject.AddComponent<LineRenderer>();
            _rope.positionCount = 2;
            _rope.startWidth = ropeWidth;
            _rope.endWidth = ropeWidth;
            _rope.material = new Material(Shader.Find("Sprites/Default"));
            _rope.startColor = ropeColor;
            _rope.endColor = ropeColor;
            _rope.sortingOrder = 5;
            _rope.enabled = false;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            bool held = kb.spaceKey.isPressed;

            if (IsSwinging)
            {
                if (!held || !grappleEnabled) Release();
                return;
            }

            // target (and glow) only exists while a grab is actually possible: airborne
            _candidate = (grappleEnabled && !_ctrl.IsGrounded) ? SelectTarget() : null;

            // airborne + fresh press + a target = grab. Grounded Space stays jump.
            if (grappleEnabled && !_ctrl.IsGrounded
                && kb.spaceKey.wasPressedThisFrame && _candidate != null)
            {
                Attach(_candidate);
            }
        }

        void FixedUpdate()
        {
            if (!IsSwinging) return;

            _rb.AddForce(new Vector2(_ctrl.MoveInput * pumpForce, 0f));

            // W/Up reels the rope in, pulling the player toward the point
            var kb = Keyboard.current;
            if (kb != null && (kb.wKey.isPressed || kb.upArrowKey.isPressed))
                _joint.distance = Mathf.Max(minReelDistance,
                    _joint.distance - reelSpeed * Time.fixedDeltaTime);
        }

        void LateUpdate()
        {
            if (IsSwinging && _target != null)
            {
                _rope.SetPosition(0, transform.position);
                _rope.SetPosition(1, _target.transform.position);
            }
        }

        PP_GrapplePoint SelectTarget()
        {
            PP_GrapplePoint bestFacing = null, bestAny = null;
            float bestFacingDist = range, bestAnyDist = range;
            int facing = _ctrl.Facing;

            foreach (var gp in Object.FindObjectsByType<PP_GrapplePoint>())
            {
                float d = Vector2.Distance(transform.position, gp.transform.position);
                if (d > range) continue;

                if (d <= bestAnyDist) { bestAnyDist = d; bestAny = gp; }

                float dx = gp.transform.position.x - transform.position.x;
                bool inFacing = dx == 0f || Mathf.Sign(dx) == facing;
                if (inFacing && d <= bestFacingDist) { bestFacingDist = d; bestFacing = gp; }
            }
            return bestFacing != null ? bestFacing : bestAny;
        }

        void Attach(PP_GrapplePoint point)
        {
            _target = point;
            _joint = gameObject.AddComponent<DistanceJoint2D>();
            _joint.autoConfigureConnectedAnchor = false;
            _joint.connectedAnchor = point.transform.position;
            _joint.distance = Vector2.Distance(transform.position, point.transform.position);
            _joint.maxDistanceOnly = true;    // rope, not rod: can go slack
            _joint.enableCollision = true;

            _rb.gravityScale = swingGravityScale;   // Unity gravity drives the pendulum
            _ctrl.controlSuspended = true;
            _rope.enabled = true;
        }

        public void Release()
        {
            if (_joint != null) Destroy(_joint);
            _joint = null;
            _target = null;
            _rb.gravityScale = 0f;                  // controller's manual gravity resumes
            _ctrl.controlSuspended = false;
            _rope.enabled = false;
        }

        /// <summary>Form changed away from adult: drop the rope.</summary>
        public void ForceRelease()
        {
            if (IsSwinging) Release();
        }
    }
}
