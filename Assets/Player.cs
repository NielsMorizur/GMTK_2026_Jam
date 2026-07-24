using UnityEngine;

public class Player : MonoBehaviour
{
    public Animator anim {get; private set; }

    public Rigidbody2D rb {get; private set; }

    public BoxCollider2D box2D {get; private set; }

    public PlayerInputSet input {get; private set; }

    private StateMachine stateMachine;

    public Player_IdleState idleState {get; private set;}
    public Player_MoveState moveState {get; private set;}
    public Player_JumpState jumpState {get; private set;}
    public Player_FallState fallState {get; private set;}
    public Player_DashState dashState {get; private set;}
    public Player_GrappleState grappleState {get; private set;}
    public Player_DoubleJumpState doubleJumpState {get; private set;}

    [Header("Age")]
    [SerializeField] private AgeStage startingAge = AgeStage.Elderly;
    [SerializeField] private PlayerAgeStats babyStats;
    [SerializeField] private PlayerAgeStats teenStats;
    [SerializeField] private PlayerAgeStats adultStats;
    [SerializeField] private PlayerAgeStats elderlyStats;

    public AgeStage CurrentAge {get; private set;}
    public PlayerAgeStats Stats {get; private set;}

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer sr;

    [Header("Movement details")]
    private bool facingRight = true;
    public Vector2 moveInput {get; private set;}
    public bool hasDoubleJump { get; set; } = true;
    public int FacingDirection => facingRight ? 1 : -1;

    [Header("Jump feel")]
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;
    public float jumpClearance = 0.35f;
    [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f;
    [Tooltip("Releasing jump early can never leave the jump shorter than this many blocks")]
    public float minJumpHeightBlocks = 1.25f;

    private float coyoteTimer;
    private float jumpBufferTimer;

    [Header("Dash")]
    public float dashSpeed = 18f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.35f;

    public bool hasAirDash { get; set; } = true;
    public bool IsDashing { get; set; }
    public float DefaultGravityScale { get; private set; }

    private float dashCooldownTimer;

    [Header("Grapple")]
    public float grappleRange = 6f;
    [Tooltip("Gravity scale while swinging. Lower = slower, floatier swings.")]
    public float swingGravityScale = 1.8f;
    [Tooltip("Gentle push from move input while swinging. Keep this small - it's a nudge, not a thruster.")]
    public float pumpForce = 8f;
    [Tooltip("Soft speed cap while swinging so pumping can't spiral into unrealistic speeds.")]
    public float maxSwingSpeed = 9f;
    [Tooltip("Drag applied only while swinging, so momentum decays naturally instead of building forever.")]
    [Range(0f, 2f)] public float swingLinearDamping = 0.3f;
    public float reelSpeed = 3f;
    public float minReelDistance = 0.6f;
    [Tooltip("Extra slack added to the rope on attach so it isn't instantly taut - lets momentum ease into the swing instead of jerking to a stop.")]
    public float grappleSlack = 0.4f;
    public float ropeWidth = 0.08f;
    public Color ropeColor = new Color(0.2f, 0.2f, 0.22f, 1f);

    public GrapplePoint CurrentGrappleTarget { get; private set; }
    public GrapplePoint AttachedGrapplePoint { get; private set; }
    public DistanceJoint2D GrappleJoint { get; private set; }
    public bool IsGrappling { get; private set; }
    public float DefaultLinearDamping { get; private set; }

    private LineRenderer ropeRenderer;

    [Header("Collision detection")]
    [SerializeField] private float groundCheckDistance;
    [SerializeField] private float groundCheckX = 0.1f;
    [Tooltip("Width of the ground probe. Slightly narrower than the collider so wall-hugging never counts as grounded.")]
    [SerializeField] private float groundCheckWidth = 0.9f;
    [SerializeField] private LayerMask whatIsGround;
    public bool groundDetected {get; private set;}

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
        box2D = GetComponent<BoxCollider2D>();
        DefaultGravityScale = rb.gravityScale;
        DefaultLinearDamping = rb.linearDamping;

        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>();

        SetupRope();
        
        stateMachine = new StateMachine();
        input = new PlayerInputSet();

        idleState = new Player_IdleState(this, stateMachine, "idle");
        moveState = new Player_MoveState(this, stateMachine, "move");
        jumpState = new Player_JumpState(this, stateMachine, "jumpFall");
        fallState = new Player_FallState(this, stateMachine, "jumpFall");
        dashState = new Player_DashState(this, stateMachine, "dash");
        grappleState = new Player_GrappleState(this, stateMachine, "grapple");
        doubleJumpState = new Player_DoubleJumpState(this, stateMachine, "doubleJump");
    }

    private void SetupRope()
    {
        ropeRenderer = gameObject.AddComponent<LineRenderer>();
        ropeRenderer.positionCount = 2;
        ropeRenderer.startWidth = ropeWidth;
        ropeRenderer.endWidth = ropeWidth;
        ropeRenderer.material = new Material(Shader.Find("Sprites/Default"));
        ropeRenderer.startColor = ropeColor;
        ropeRenderer.endColor = ropeColor;
        ropeRenderer.sortingOrder = 5;
        ropeRenderer.enabled = false;
    }

    private void OnEnable()
    {
        input.Enable();

        input.Player.Movement.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Movement.canceled += ctx => moveInput = Vector2.zero;
    }

    private void OnDisable()
    {
        input.Disable();
    }

    private void Start()
    {
        SetAge(startingAge);
        stateMachine.Initialize(idleState);
    }

    private void Update()
    {
        HandleCollisionDetection();
        HandleJumpTimers();
        HandleDashTimers();
        HandleGrappleTargeting();
        HandleGrappleTrigger();
        HandleDashTrigger();
        HandleJumpTrigger();
        stateMachine.UpdateActiveState();
    }

    private void FixedUpdate()
    {
        stateMachine.FixedUpdateActiveState();
        if (IsGrappling)
            HandleGrapplePhysics();
    }

    private void LateUpdate()
    {
        if (IsGrappling && AttachedGrapplePoint != null && ropeRenderer != null)
        {
            ropeRenderer.SetPosition(0, transform.position);
            ropeRenderer.SetPosition(1, AttachedGrapplePoint.transform.position);
        }
    }

    private void HandleJumpTimers()
    {
        coyoteTimer = groundDetected ? coyoteTime : coyoteTimer - Time.deltaTime;

        if (input.Player.Jump.WasPerformedThisFrame())
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= Time.deltaTime;
    }

    private void HandleJumpTrigger()
    {
        if (IsDashing || IsGrappling) return;

        if (jumpBufferTimer > 0f && coyoteTimer > 0f && Stats != null && Stats.canJump)
        {
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            hasDoubleJump = true;
            stateMachine.ChangeState(jumpState);
        }
    }

    private void HandleDashTimers()
    {
        dashCooldownTimer -= Time.deltaTime;

        if (groundDetected)
            hasAirDash = true;
    }

    private void HandleDashTrigger()
    {
        if (IsDashing || IsGrappling) return;

        bool canUseDash = Stats != null && Stats.canDash
                           && dashCooldownTimer <= 0f
                           && (groundDetected || hasAirDash);

        if (canUseDash && input.Player.Dash.WasPerformedThisFrame())
        {
            dashCooldownTimer = dashCooldown;

            if (!groundDetected)
                hasAirDash = false;

            stateMachine.ChangeState(dashState);
        }
    }

    private void HandleGrappleTargeting()
    {
        if (IsGrappling)
        {
            CurrentGrappleTarget = AttachedGrapplePoint;
            return;
        }

        bool canLookForTarget = Stats != null && Stats.canGrapple && !groundDetected;
        CurrentGrappleTarget = canLookForTarget ? FindGrappleTarget() : null;
    }

    private void HandleGrappleTrigger()
    {
        if (IsGrappling || IsDashing) return;
        if (groundDetected) return;
        if (Stats == null || !Stats.canGrapple) return;
        if (CurrentGrappleTarget == null) return;

        if (input.Player.Jump.WasPerformedThisFrame())
            AttachGrapple(CurrentGrappleTarget);
    }

    private GrapplePoint FindGrappleTarget()
    {
        GrapplePoint bestFacing = null, bestAny = null;
        float bestFacingDist = grappleRange, bestAnyDist = grappleRange;
        int facing = FacingDirection;

        foreach (var gp in FindObjectsByType<GrapplePoint>())
        {
            float d = Vector2.Distance(transform.position, gp.transform.position);
            if (d > grappleRange) continue;
            if (GrappleLineBlocked(gp.transform.position)) continue;   // no grappling through walls

            if (d <= bestAnyDist) { bestAnyDist = d; bestAny = gp; }

            float dx = gp.transform.position.x - transform.position.x;
            bool inFacing = dx == 0f || Mathf.Sign(dx) == facing;
            if (inFacing && d <= bestFacingDist) { bestFacingDist = d; bestFacing = gp; }
        }

        return bestFacing != null ? bestFacing : bestAny;
    }

    // True when ground or a hazard sits between the player and the target.
    private bool GrappleLineBlocked(Vector2 target)
    {
        foreach (var hit in Physics2D.LinecastAll(transform.position, target))
        {
            if (hit.collider == null) continue;
            if (hit.collider.GetComponentInParent<Player>() != null) continue;      // own body
            if (hit.collider.GetComponent<GrapplePoint>() != null) continue;        // points never block
            bool groundHit = (whatIsGround.value & (1 << hit.collider.gameObject.layer)) != 0;
            bool hazardHit = hit.collider.GetComponent<ParsaProto.PP_Hazard>() != null;
            if (groundHit || hazardHit) return true;
        }
        return false;
    }

    /// <summary>Rope path check while attached; the grapple state releases when blocked.</summary>
    public bool IsGrappleLineBlocked()
    {
        return AttachedGrapplePoint != null
            && GrappleLineBlocked(AttachedGrapplePoint.transform.position);
    }

    public void AttachGrapple(GrapplePoint point)
    {
        AttachedGrapplePoint = point;

        float currentDistance = Vector2.Distance(transform.position, point.transform.position);

        GrappleJoint = gameObject.AddComponent<DistanceJoint2D>();
        GrappleJoint.autoConfigureConnectedAnchor = false;
        GrappleJoint.connectedAnchor = point.transform.position;
        GrappleJoint.distance = currentDistance + grappleSlack; // slack prevents an instant hard stop
        GrappleJoint.maxDistanceOnly = true;
        GrappleJoint.enableCollision = true;

        rb.gravityScale = swingGravityScale;
        rb.linearDamping = swingLinearDamping;
        IsGrappling = true;

        // the press that attached us must not linger as a buffered jump
        jumpBufferTimer = 0f;

        if (ropeRenderer != null)
            ropeRenderer.enabled = true;

        stateMachine.ChangeState(grappleState);
    }

    private void HandleGrapplePhysics()
    {
        if (GrappleJoint == null) return;

        // Gentle pump - nudges the swing, doesn't override it.
        rb.AddForce(new Vector2(moveInput.x * pumpForce, 0f), ForceMode2D.Force);

        // Holding up reels the rope in.
        if (moveInput.y > 0f)
        {
            GrappleJoint.distance = Mathf.Max(minReelDistance,
                GrappleJoint.distance - reelSpeed * Time.fixedDeltaTime);
        }

        // Soft cap so pumping can't spiral the swing into unrealistic speed.
        if (rb.linearVelocity.magnitude > maxSwingSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSwingSpeed;
    }

    public void ReleaseGrapple()
    {
        if (GrappleJoint != null)
            Destroy(GrappleJoint);

        GrappleJoint = null;
        AttachedGrapplePoint = null;
        rb.gravityScale = DefaultGravityScale;
        rb.linearDamping = DefaultLinearDamping;
        IsGrappling = false;

        if (ropeRenderer != null)
            ropeRenderer.enabled = false;
    }

    public float GetJumpVelocity(float heightInBlocks)
    {
        float gravity = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
        float height = heightInBlocks + jumpClearance;
        return Mathf.Sqrt(2f * gravity * height);
    }

    public void SetAge(AgeStage newAge)
    {
        CurrentAge = newAge;

        Stats = newAge switch
        {
            AgeStage.Baby => babyStats,
            AgeStage.Teen => teenStats,
            AgeStage.Adult => adultStats,
            AgeStage.Elderly => elderlyStats,
            _ => adultStats
        };

        if (Stats == null)
        {
            Debug.LogWarning($"No PlayerAgeStats assigned for age: {newAge}");
            return;
        }

        if (Stats.animatorController != null && anim != null)
        {
            anim.runtimeAnimatorController = Stats.animatorController;

            // the controller swap wipes every animator flag; re-assert the
            // active state's so the animation doesn't pop to Idle mid-action
            if (stateMachine != null && stateMachine.currentState != null)
                stateMachine.currentState.ReassertAnimation();
        }

        if (sr != null)
            sr.color = Stats.spriteColor;

        if (box2D != null)
        {
            box2D.size = new Vector2(Stats.colliderSizeX, Stats.colliderSizeY);
            box2D.offset = new Vector2(Stats.colliderOffsetX, Stats.colliderOffsetY);   
        }

        groundCheckWidth = Stats.groundCheckWidth;
        groundCheckDistance = Stats.groundCheckDistance;

        if (IsGrappling && !Stats.canGrapple)
            stateMachine.ChangeState(fallState);
    }

    public void SetVelocity(float xVelocity, float yVelocity)
    {
        rb.linearVelocity = new Vector2(xVelocity, yVelocity);
        HandleFlip(xVelocity);
    }

    private void HandleFlip(float xVelocity)
    {
        // dead zone: resting velocity jitters around zero and must never flip us
        if (xVelocity > 0.01f && facingRight == false)
            Flip();
        else if (xVelocity < -0.01f && facingRight)
            Flip();
    }

    private void Flip()
    {
        transform.Rotate(0,180,0);
        facingRight = !facingRight;
    }

    private void HandleCollisionDetection()
    {
        // box the width of the feet instead of a single center ray, so ledge
        // edges and tile seams read correctly. Probe from the collider's
        // world center: per-form offsets and the 180° flip move the collider,
        // and the probe must move with it. groundCheckX flips with facing.
        Vector2 origin = box2D != null ? (Vector2)box2D.bounds.center
                                       : (Vector2)transform.position;
        origin.x += groundCheckX * FacingDirection;

        groundDetected = Physics2D.BoxCast(origin,
            new Vector2(groundCheckWidth, 0.1f), 0f, Vector2.down,
            groundCheckDistance, whatIsGround);
    }

    private void OnDrawGizmos()
    {
        var box = box2D != null ? box2D : GetComponent<BoxCollider2D>();
        Vector3 origin = box != null ? (Vector3)box.bounds.center : transform.position;
        origin.x += groundCheckX * (facingRight ? 1 : -1);

        Gizmos.DrawWireCube(origin + Vector3.down * groundCheckDistance,
            new Vector3(groundCheckWidth, 0.1f, 0));
    }
}