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
    public float jumpHeightBlocks = 3f;

    [Header("Abilities")]
    public bool canJump = true;
    public bool canPush = true;
    public bool canDoubleJump = true;

    [Header("Visuals")]
    public RuntimeAnimatorController animatorController;
    public Color spriteColor = Color.black;
}
