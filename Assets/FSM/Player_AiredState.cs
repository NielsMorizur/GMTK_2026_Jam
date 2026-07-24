using UnityEngine;

public class Player_AiredState : EntityState
{
    public Player_AiredState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Update()
    {
        base.Update();

        if (input.Player.Jump.WasPerformedThisFrame() && player.Stats.canDoubleJump && player.hasDoubleJump)
        {
            stateMachine.ChangeState(player.jumpState);
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