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
            transform.position = new Vector3(_player.transform.position.x,
                _player.transform.position.y, _homePos.z);
            _cam.orthographicSize = startZoom;
        }

        void LateUpdate()
        {
            if (_done) return;
            _t += Time.unscaledDeltaTime;

            Vector3 onPlayer = _player != null
                ? new Vector3(_player.transform.position.x, _player.transform.position.y, _homePos.z)
                : _homePos;

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
    }
}
