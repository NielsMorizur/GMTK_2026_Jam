using UnityEngine;

public class Player_DashState : EntityState
{
    private float dashTimer;
    private int dashDirection;

    public Player_DashState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        player.IsDashing = true;
        dashTimer = player.dashDuration;
        dashDirection = player.FacingDirection;

        rb.gravityScale = 0f; // dash ignores gravity for its duration
        player.SetVelocity(dashDirection * player.dashSpeed, 0f);
    }

    public override void Update()
    {
        base.Update();

        dashTimer -= Time.deltaTime;

        // Keep velocity locked for the whole dash, in case anything else nudges it.
        player.SetVelocity(dashDirection * player.dashSpeed, 0f);

        if (dashTimer <= 0f)
        {
            if (player.groundDetected)
                stateMachine.ChangeState(player.moveInput.x != 0 ? player.moveState : player.idleState);
            else
                stateMachine.ChangeState(player.fallState);
        }
    }

    public override void Exit()
    {
        base.Exit();

        player.IsDashing = false;
        rb.gravityScale = player.DefaultGravityScale;
    }
}