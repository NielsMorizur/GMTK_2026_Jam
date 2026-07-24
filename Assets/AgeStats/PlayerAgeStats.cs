using UnityEngine;

[CreateAssetMenu(fileName = "New Age Stats", menuName = "Player/Age Stats")]
public class PlayerAgeStats : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    [Range(0f, 1f)]
    public float inAirMoveMultiplier = 1f;

    [Header("Jump")]
    [Tooltip("Jump height in world units (1 = one tile/block).")]
    public float jumpHeightBlocks = 2f;

    [Header("Abilities")]
    public bool canJump = true;
    public bool canPush = true;
    public bool canDoubleJump = true;
    public bool canDash = false;
    public bool canGrapple = false;

    [Header("Collider Size")]
    public float colliderSizeX = 0.85f;
    public float colliderSizeY = 1.75f;
    public float colliderOffsetX = 0.067f;
    public float colliderOffsetY = -0.075f;
    public float groundCheckWidth = 0.39f;
    public float groundCheckDistance = 1.15f;

    [Header("Visuals")]
    public RuntimeAnimatorController animatorController;
    public Color spriteColor = Color.black;
}
