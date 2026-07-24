using UnityEngine;

public class Player_GrappleState : EntityState
{
    // Entry grace + release debounce: one odd frame of input or state must
    // never flick the swing off and back on.
    private const float MinAttachTime = 0.1f;
    private float enterTime;
    private int releasedFrames;

    // every animator flag that is not ours
    private static readonly string[] OtherAnimBools =
        { "idle", "move", "jumpFall", "dash", "doubleJump" };

    public Player_GrappleState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        enterTime = Time.time;
        releasedFrames = 0;
    }

    public override void Update()
    {
        base.Update();

        // Hard lock: while the grapple is deployed, its animation is the only
        // one allowed. Clear every other flag and snap the animator back if
        // anything else managed to start playing.
        anim.SetBool(animBoolName, true);
        foreach (var b in OtherAnimBools)
            anim.SetBool(b, false);

        if (!anim.GetCurrentAnimatorStateInfo(0).IsName("Grapple")
            && !anim.GetNextAnimatorStateInfo(0).IsName("Grapple"))
            anim.Play("Grapple", 0, 0f);

        bool held = input.Player.Jump.IsPressed();
        bool stillAllowed = player.Stats != null && player.Stats.canGrapple && player.AttachedGrapplePoint != null;

        // require two consecutive released frames before letting go
        releasedFrames = held ? 0 : releasedFrames + 1;

        if (Time.time - enterTime < MinAttachTime) return;

        if (releasedFrames >= 2 || !stillAllowed)
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
