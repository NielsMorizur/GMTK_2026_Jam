using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Hub arrival shot: every time the room loads, the camera opens zoomed
    /// tight on the player, holds a beat, then eases out to the framing the
    /// camera was authored with. Purely visual; gameplay runs normally.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PP_HubCameraIntro : MonoBehaviour
    {
        [Tooltip("Ortho size of the opening close-up")]
        public float startZoom = 3f;
        [Tooltip("Seconds held on the player before zooming out")]
        public float hold = 0.7f;
        [Tooltip("Seconds the zoom-out takes")]
        public float zoomTime = 1.1f;
        [Tooltip("Where the close-up centers, relative to the player")]
        public Vector2 frameOffset = new Vector2(0f, 0.3f);
        [Tooltip("Draw the close-up frame in the Scene view (never visible in game)")]
        public bool showFramePreview = true;

        Camera _cam;
        float _homeSize;
        Vector3 _homePos;
        Player _player;
        float _t;
        bool _done;

        void Start()
        {
            _cam = GetComponent<Camera>();
            _homeSize = _cam.orthographicSize;
            _homePos = transform.position;
            _player = FindAnyObjectByType<Player>();
            if (_player == null)
            {
                _done = true;
                return;
            }
            transform.position = FramePos();
            _cam.orthographicSize = startZoom;
        }

        Vector3 FramePos()
        {
            return new Vector3(_player.transform.position.x + frameOffset.x,
                _player.transform.position.y + frameOffset.y, _homePos.z);
        }

        void LateUpdate()
        {
            if (_done) return;
            _t += Time.unscaledDeltaTime;

            Vector3 onPlayer = _player != null ? FramePos() : _homePos;

            if (_t < hold)
            {
                transform.position = onPlayer;
                return;
            }

            float k = Mathf.Clamp01((_t - hold) / Mathf.Max(0.05f, zoomTime));
            float e = k * k * (3f - 2f * k);
            transform.position = Vector3.Lerp(onPlayer, _homePos, e);
            _cam.orthographicSize = Mathf.Lerp(startZoom, _homeSize, e);

            if (k >= 1f)
            {
                _done = true;
                transform.position = _homePos;
                _cam.orthographicSize = _homeSize;
            }
        }

        // yellow box in the Scene view = exactly what the opening close-up
        // frames; drag Frame Offset / Start Zoom and watch it move
        void OnDrawGizmos()
        {
            if (!showFramePreview || Application.isPlaying) return;
            var player = FindAnyObjectByType<Player>();
            if (player == null) return;
            var cam = GetComponent<Camera>();
            float aspect = cam != null ? cam.aspect : 16f / 9f;
            Vector3 c = new Vector3(player.transform.position.x + frameOffset.x,
                player.transform.position.y + frameOffset.y, 0f);
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.9f);
            Gizmos.DrawWireCube(c, new Vector3(2f * startZoom * aspect, 2f * startZoom, 0.1f));
        }
    }
}
