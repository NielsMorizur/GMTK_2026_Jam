using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Floating hazard: loops its idle flipbook, and opens its eyes (the
    /// blink overlay) when the player comes within range, closing them again
    /// when he leaves. Killing is handled by the PP_Hazard component on the
    /// same object; the trigger collider is the hitbox, resize it freely.
    /// </summary>
    public class PP_FloatingHazard : MonoBehaviour
    {
        [Header("Idle")]
        public Sprite[] idleFrames;
        public float idleFps = 8f;

        [Header("Eyes")]
        [Tooltip("Blink strip in closed-to-open order")]
        public Sprite[] blinkFrames;
        [Tooltip("Frames per second the eyes open and close at")]
        public float blinkFps = 14f;
        [Tooltip("Player distance at which the eyes open")]
        public float eyeRange = 4f;

        [Header("Renderers")]
        public SpriteRenderer visual;
        public SpriteRenderer eyes;

        Player _player;
        float _idleT;
        float _blink;   // continuous position along the blink strip

        void Update()
        {
            if (visual != null && idleFrames != null && idleFrames.Length > 0)
            {
                _idleT += Time.deltaTime * idleFps;
                visual.sprite = idleFrames[(int)_idleT % idleFrames.Length];
            }

            if (eyes == null || blinkFrames == null || blinkFrames.Length == 0) return;

            if (_player == null)
                _player = FindAnyObjectByType<Player>();

            bool near = _player != null &&
                Vector2.Distance(_player.transform.position, transform.position) <= eyeRange;

            // the overlay always shows: it rests on the first frame (closed)
            // and slides to the last (open) while the player is near
            _blink = Mathf.MoveTowards(_blink, near ? blinkFrames.Length - 1 : 0f,
                blinkFps * Time.deltaTime);

            eyes.enabled = true;
            int i = Mathf.Clamp(Mathf.RoundToInt(_blink), 0, blinkFrames.Length - 1);
            eyes.sprite = blinkFrames[i];
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0.3f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, eyeRange);
        }
    }
}
