using UnityEngine;

public class Player_JumpState : Player_AiredState
{
    private float takeoffVelocity;

    // the double jump overrides this: it always flies its full height
    protected virtual bool AllowJumpCut => true;

    public Player_JumpState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        takeoffVelocity = player.GetJumpVelocity(player.Stats.jumpHeightBlocks);
        player.SetVelocity(rb.linearVelocity.x, takeoffVelocity);
    }

    public override void Update()
    {
        base.Update();

        // Variable jump height (off by default, toggle on the Player): cut the
        // ascent short on early release, but never below minJumpHeightBlocks.
        if (player.variableJumpHeight && AllowJumpCut
            && input.Player.Jump.WasReleasedThisFrame() && rb.linearVelocity.y > 0f)
        {
            float vy = rb.linearVelocity.y;
            float cut = vy * player.jumpCutMultiplier;

            // velocity still needed so height-gained-so-far + remaining rise
            // adds up to the guaranteed minimum
            float gravity = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
            float need = 2f * gravity * player.minJumpHeightBlocks
                         - (takeoffVelocity * takeoffVelocity - vy * vy);
            float floor = need > 0f ? Mathf.Sqrt(need) : 0f;

            player.SetVelocity(rb.linearVelocity.x, Mathf.Max(cut, floor));
        }

        if (rb.linearVelocity.y < 0)
            stateMachine.ChangeState(player.fallState);
    }
}
