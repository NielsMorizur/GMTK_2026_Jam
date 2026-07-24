using UnityEngine;

public class Player_GrappleState : EntityState
{
    public Player_GrappleState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Update()
    {
        base.Update();

        bool held = input.Player.Jump.IsPressed();
        bool stillAllowed = player.Stats != null && player.Stats.canGrapple && player.AttachedGrapplePoint != null;

        if (!held || !stillAllowed)
        {
            stateMachine.ChangeState(player.fallState);
        }
    }

    public override void Exit()
    {
        base.Exit();

        player.ReleaseGrapple();
    }
}