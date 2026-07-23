using UnityEngine;

public class Player_AiredState : EntityState
{
    public Player_AiredState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Update()
    {
        base.Update();

        player.SetVelocity(player.moveInput.x * (player.Stats.moveSpeed * player.Stats.inAirMoveMultiplier), rb.linearVelocity.y);
    }
}
