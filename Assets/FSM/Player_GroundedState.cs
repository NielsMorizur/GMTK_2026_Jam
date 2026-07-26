using UnityEngine;

public class Player_GroundedState : EntityState
{
    public Player_GroundedState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Update()
    {
        base.Update();

        if (rb.linearVelocity.y < -0.1f)   // tolerance: resting contact jitters microscopically below zero
            stateMachine.ChangeState(player.fallState);

        if (input.Player.Jump.WasPerformedThisFrame() && player.Stats.canJump
            && !ParsaProto.PP_MouseInput.MouseOnlyJumpThisFrame)
            stateMachine.ChangeState(player.jumpState);
    }
}
