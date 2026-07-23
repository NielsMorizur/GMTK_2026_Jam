using UnityEngine;

public class Player_JumpState : Player_AiredState
{
    public Player_JumpState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        float jumpVelocity = player.GetJumpVelocity(player.Stats.jumpHeightBlocks);
        player.SetVelocity(rb.linearVelocity.x, jumpVelocity);
    }

    public override void Update()
    {
        base.Update();

        // Variable jump height: cut the ascent short if the button is released early.
        if (input.Player.Jump.WasReleasedThisFrame() && rb.linearVelocity.y > 0f)
            player.SetVelocity(rb.linearVelocity.x, rb.linearVelocity.y * player.jumpCutMultiplier);

        if (rb.linearVelocity.y < 0)
            stateMachine.ChangeState(player.fallState);
    }
}