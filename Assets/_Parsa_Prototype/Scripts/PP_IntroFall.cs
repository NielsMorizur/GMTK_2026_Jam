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
        [Tooltip("Off = wait for the director's IntroSectionBegin instead of the sequence clock")]
        public bool autoCue = true;

        [Header("Fall")]
        public float gravity = 10f;
        public float maxFallSpeed = 13f;
        [Tooltip("World X where he lands (he drifts there during the fall)")]
        public float endX = 5.5f;
        [Tooltip("World Y where he comes to rest (bottom of the shot)")]
        public float endY = -4.2f;
        [Tooltip("The elder leans this many degrees while falling (rights himself on landing)")]
        public float elderTiltAngle = 12f;
        [Tooltip("Continuous spin instead of the fixed tilt; 0 = use the tilt")]
        public float tumble = 0f;

        [Header("Camera")]
        public Camera cam;
        [Tooltip("Ortho size to zoom down to")]
        public float zoomTo = 3.2f;
        [Tooltip("How quickly the camera commits; higher = snappier")]
        public float camEase = 1.1f;
        [Tooltip("The camera stays put until his descent passes this fraction (0.5 = halfway down)")]
        [Range(0f, 1f)] public float followAfter = 0.5f;
        [Tooltip("How much of his position the camera adopts (x, y)")]
        public Vector2 followWeight = new Vector2(0.75f, 0.6f);
        [Tooltip("Optional animator on the elder; falls in JumpFall, lands to Idle")]
        public Animator anim;
        [Tooltip("Optional sound bank on the elder (landing thud)")]
        public PP_SfxPlayer sfx;

        /// <summary>The candle climb takes the camera over from here.</summary>
        public void ReleaseCamera() => cam = null;

        /// <summary>True from the moment the drop begins (locks section skipping).</summary>
        public bool FallStarted => _falling;
        /// <summary>True once he's down on the platform.</summary>
        public bool Landed => _landedOnce;

        SpriteRenderer _sr;
        float _t;
        float _vy;
        bool _falling;
        bool _landedOnce;
        Vector3 _camHome;

        void Awake()
        {
            _sr = GetComponentInChildren<SpriteRenderer>();
            if (_sr != null) _sr.enabled = false;
            if (cam != null) _camHome = cam.transform.position;
        }

        /// <summary>Director hook: the fall section started.</summary>
        void IntroSectionBegin()
        {
            BeginFall();
        }

        Vector3 _fallStart;

        void BeginFall()
        {
            if (_falling) return;
            _falling = true;
            _fallStart = transform.position;
            if (_sr != null) _sr.enabled = true;
            if (anim != null) anim.CrossFade("JumpFall", 0.05f);
        }

        void Update()
        {
            if (!_falling && autoCue)
            {
                _t += Time.unscaledDeltaTime;
                float cue = (sequence != null ? sequence.FinalOutTime : 0f) + extraDelay;
                if (_t >= cue) BeginFall();
            }
            if (!_falling) return;

            float dt = Time.unscaledDeltaTime;

            // the fall - runs once; after touchdown the elder belongs to
            // whoever moves him next (the candle climb), no more gravity
            if (!_landedOnce && transform.position.y > endY)
            {
                _vy = Mathf.Min(_vy + gravity * dt, maxFallSpeed);
                var p = transform.position;
                p.y = Mathf.Max(endY, p.y - _vy * dt);
                // drift toward the landing X as the descent progresses
                float prog = Mathf.Clamp01((_fallStart.y - p.y)
                    / Mathf.Max(0.01f, _fallStart.y - endY));
                p.x = Mathf.Lerp(_fallStart.x, endX, Mathf.SmoothStep(0f, 1f, prog));
                transform.position = p;

                if (tumble != 0f)
                    transform.Rotate(0f, 0f, tumble * dt);
                else
                    transform.rotation = Quaternion.Lerp(transform.rotation,
                        Quaternion.Euler(0f, 0f, elderTiltAngle),
                        1f - Mathf.Exp(-5f * dt));
            }
            else
            {
                if (!_landedOnce)
                {
                    if (anim != null) anim.CrossFade("Idle", 0.08f);
                    if (sfx != null) sfx.Play(sfx.landing.For(AgeStage.Elderly));
                }
                _landedOnce = true;
                transform.rotation = Quaternion.Lerp(transform.rotation,
                    Quaternion.identity, 1f - Mathf.Exp(-6f * dt));
            }

            // the camera: drift after him and zoom in - never tilted, the
            // elder carries the tilt himself. It holds its opening frame
            // until he's far enough down, then commits. Follow stays live
            // through the candle climb handoff.
            if (cam == null) return;
            float fallProg = Mathf.Clamp01((_fallStart.y - transform.position.y)
                / Mathf.Max(0.01f, _fallStart.y - endY));
            if (!_landedOnce && fallProg < followAfter) return;
            float k = 1f - Mathf.Exp(-camEase * dt);

            Vector3 target = new Vector3(
                _camHome.x + (transform.position.x - _camHome.x) * followWeight.x,
                _camHome.y + (transform.position.y - _camHome.y) * followWeight.y,
                _camHome.z);
            cam.transform.position = Vector3.Lerp(cam.transform.position, target, k);
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, zoomTo, k);
            cam.transform.rotation = Quaternion.identity;
        }
    }
}
