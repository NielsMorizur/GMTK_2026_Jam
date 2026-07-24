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