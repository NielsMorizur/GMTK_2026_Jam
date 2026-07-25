using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class GrapplePoint : MonoBehaviour
{
    [Tooltip("Color while out of range or wrong form.")]
    public Color baseColor = new Color(0.62f, 0.47f, 0.16f, 1f);

    [Tooltip("Color while grappleable; pulses slightly.")]
    public Color glowColor = new Color(1f, 0.85f, 0.35f, 1f);

    [Tooltip("Pulse speed while glowing.")]
    public float pulseSpeed = 3f;

    [Tooltip("Hook art while idle.")]
    public Sprite normalSprite;

    [Tooltip("Hook art while targeted/attached.")]
    public Sprite activeSprite;

    [Tooltip("Where the rope visually connects, in world units from the center (the hook ring sits below center).")]
    public Vector2 ropeAnchorOffset = new Vector2(0f, -0.3f);

    /// <summary>The point the rope should be drawn to.</summary>
    public Vector3 RopeAnchor => transform.position + (Vector3)ropeAnchorOffset;

    private SpriteRenderer sr;
    private Player player;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        player = FindAnyObjectByType<Player>();
    }

    private void Update()
    {
        if (sr == null) return;

        bool targeted = player != null && player.CurrentGrappleTarget == this;
        bool attached = player != null && player.IsGrappling
                        && player.AttachedGrapplePoint == this;

        // the second hook art only while the belt is actually hooked on;
        // being merely in range keeps the normal art (the glow signals range)
        if (normalSprite != null && activeSprite != null)
            sr.sprite = attached ? activeSprite : normalSprite;

        if (targeted)
        {
            float t = Mathf.PingPong(Time.time * pulseSpeed, 1f) * 0.35f;
            sr.color = Color.Lerp(glowColor, Color.white, t);
        }
        else
        {
            sr.color = baseColor;
        }
    }
}