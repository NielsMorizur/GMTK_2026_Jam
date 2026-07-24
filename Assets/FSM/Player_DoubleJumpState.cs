using UnityEngine;

// Second jump: identical movement to the normal jump, but announces its own
// animation flag so the gas-jump art can play without the air state pulling
// the animator back.
public class Player_DoubleJumpState : Player_JumpState
{
    // gas jump is all-or-nothing: releasing space never shortens it
    protected override bool AllowJumpCut => false;

    public Player_DoubleJumpState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }
}
