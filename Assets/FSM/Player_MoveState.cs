using UnityEngine;

public class Player_MoveState : Player_GroundedState
{
    public Player_MoveState(Player player, StateMachine stateMachine, string stateName) : base(player, stateMachine, stateName)
    {
    }

    public override void Update()
    {
        base.Update();

        if (player.moveInput.x == 0)
            stateMachine.ChangeState(player.idleState);
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        Debug.Log(rb.linearVelocity.x);
        player.SetVelocity(player.moveInput.x * player.Stats.moveSpeed, rb.linearVelocity.y);
    }
}
