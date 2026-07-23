using UnityEngine;

public class Player : MonoBehaviour
{
    public Animator anim {get; private set; }

    public Rigidbody2D rb {get; private set; }

    public PlayerInputSet input {get; private set; }

    private StateMachine stateMachine;

    public Player_IdleState idleState {get; private set;}
    public Player_MoveState moveState {get; private set;}
    public Player_JumpState jumpState {get; private set;}
    public Player_FallState fallState {get; private set;}

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

    [Header("Jump feel")]
    [Tooltip("Grace window (seconds) to still jump after walking off a ledge.")]
    public float coyoteTime = 0.1f;
    [Tooltip("Window (seconds) a jump press is remembered before landing.")]
    public float jumpBufferTime = 0.1f;
    [Tooltip("Extra height added on top of the block height so a jump clears the ledge instead of grazing it.")]
    public float jumpClearance = 0.35f;
    [Tooltip("How much upward velocity survives when jump is released early. 0 = hard cut, 1 = no cut.")]
    [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f;

    private float coyoteTimer;
    private float jumpBufferTimer;

    [Header("Collision detection")]
    [SerializeField] private float groundCheckDistance;
    [SerializeField] private LayerMask whatIsGround;
    public bool groundDetected {get; private set;}

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();

        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>();

        stateMachine = new StateMachine();
        input = new PlayerInputSet();

        idleState = new Player_IdleState(this, stateMachine, "idle");
        moveState = new Player_MoveState(this, stateMachine, "move");
        jumpState = new Player_JumpState(this, stateMachine, "jumpFall");
        fallState = new Player_FallState(this, stateMachine, "jumpFall");
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
        HandleJumpTrigger();
        stateMachine.UpdateActiveState();
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
        if (jumpBufferTimer > 0f && coyoteTimer > 0f && Stats != null && Stats.canJump)
        {
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            hasDoubleJump = true; // fresh airborne trip, restore the double jump
            stateMachine.ChangeState(jumpState);
        }
    }

    // v = sqrt(2 * g * h), using the Rigidbody2D's own gravity scale so jump
    // height in "blocks" stays accurate to however gravity is tuned in the Inspector.
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
            anim.runtimeAnimatorController = Stats.animatorController;

        if (sr != null)
            sr.color = Stats.spriteColor;
    }

    public void SetVelocity(float xVelocity, float yVelocity)
    {
        rb.linearVelocity = new Vector2(xVelocity, yVelocity);
        HandleFlip(xVelocity);
    }

    private void HandleFlip(float xVelocity)
    {
        if (xVelocity > 0 && facingRight == false)
            Flip();
        else if (xVelocity < 0 && facingRight)
            Flip();
    }

    private void Flip()
    {
        transform.Rotate(0,180,0);
        facingRight = !facingRight;
    }

    private void HandleCollisionDetection()
    {
        groundDetected = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, whatIsGround);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, transform.position + new Vector3(0, -groundCheckDistance));
    }
}