using UnityEngine;

public class Player_AiredState : EntityState
{
    private int enterFrame;

    public Player_AiredState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        enterFrame = Time.frameCount;   // the press that STARTED this state must not double-fire
    }

    public override void Update()
    {
        base.Update();

        if (Time.frameCount != enterFrame
            && input.Player.Jump.WasPerformedThisFrame() && player.Stats.canDoubleJump && player.hasDoubleJump
            && !ParsaProto.PP_MouseInput.MouseOnlyJumpThisFrame)
        {
            stateMachine.ChangeState(player.doubleJumpState);
            player.hasDoubleJump = false;
        }

        if (player.groundDetected)
            player.hasDoubleJump = true;
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        
        player.SetVelocity(player.moveInput.x * (player.Stats.moveSpeed * player.Stats.inAirMoveMultiplier), rb.linearVelocity.y);
    }
}