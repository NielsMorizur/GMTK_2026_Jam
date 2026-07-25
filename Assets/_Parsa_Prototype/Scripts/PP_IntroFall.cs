using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// The intro's closing shot: as the final age still fades, the elder
    /// drops in from off the top of the screen and falls to the bottom while
    /// the camera slowly leans toward him and zooms in. Place this object at
    /// the spawn spot (off-screen top right); it hides until its cue.
    /// </summary>
    public class PP_IntroFall : MonoBehaviour
    {
        [Tooltip("The still chain whose ending cues the fall")]
        public PP_StillSequence sequence;
        [Tooltip("Extra seconds after the final still starts fading before the fall")]
        public float extraDelay = 0.5f;

        [Header("Fall")]
        public float gravity = 10f;
        public float maxFallSpeed = 13f;
        [Tooltip("World Y where he comes to rest (bottom of the shot)")]
        public float endY = -4.2f;
        [Tooltip("Gentle spin while falling, degrees/second")]
        public float tumble = 20f;

        [Header("Camera")]
        public Camera cam;
        [Tooltip("Ortho size to zoom down to")]
        public float zoomTo = 3.2f;
        [Tooltip("Dutch-tilt angle at full lean, degrees")]
        public float leanAngle = 8f;
        [Tooltip("How quickly the camera commits; higher = snappier")]
        public float camEase = 1.1f;
        [Tooltip("How much of his position the camera adopts (x, y)")]
        public Vector2 followWeight = new Vector2(0.75f, 0.6f);

        SpriteRenderer _sr;
        float _t;
        float _vy;
        bool _falling;
        Vector3 _camHome;

        void Awake()
        {
            _sr = GetComponentInChildren<SpriteRenderer>();
            if (_sr != null) _sr.enabled = false;
            if (cam != null) _camHome = cam.transform.position;
        }

        void Update()
        {
            _t += Time.unscaledDeltaTime;
            float cue = (sequence != null ? sequence.FinalOutTime : 0f) + extraDelay;
            if (!_falling && _t < cue) return;

            if (!_falling)
            {
                _falling = true;
                if (_sr != null) _sr.enabled = true;
            }

            float dt = Time.unscaledDeltaTime;

            // the fall
            if (transform.position.y > endY)
            {
                _vy = Mathf.Min(_vy + gravity * dt, maxFallSpeed);
                var p = transform.position;
                p.y = Mathf.Max(endY, p.y - _vy * dt);
                transform.position = p;
                transform.Rotate(0f, 0f, tumble * dt);
            }
            else
            {
                transform.rotation = Quaternion.Lerp(transform.rotation,
                    Quaternion.identity, 1f - Mathf.Exp(-6f * dt));
            }

            // the camera: lean toward him, drift after him, zoom in
            if (cam == null) return;
            float k = 1f - Mathf.Exp(-camEase * dt);

            Vector3 target = new Vector3(
                _camHome.x + (transform.position.x - _camHome.x) * followWeight.x,
                _camHome.y + (transform.position.y - _camHome.y) * followWeight.y,
                _camHome.z);
            cam.transform.position = Vector3.Lerp(cam.transform.position, target, k);
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, zoomTo, k);

            float side = Mathf.Sign(transform.position.x - _camHome.x);
            cam.transform.rotation = Quaternion.Lerp(cam.transform.rotation,
                Quaternion.Euler(0f, 0f, -side * leanAngle), k);
        }
    }
}
