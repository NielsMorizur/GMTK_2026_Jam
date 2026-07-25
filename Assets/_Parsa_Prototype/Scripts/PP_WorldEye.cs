using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// A watching eye placed in the world (the hub): dormant with its lid
    /// closed until the player moves, then it opens and the iris follows him
    /// around, blinking now and then. Dozes off again once he stands still.
    /// </summary>
    public class PP_WorldEye : MonoBehaviour
    {
        [Tooltip("The iris, moved inside the eye")]
        public Transform iris;
        [Tooltip("The closed-lid overlay")]
        public GameObject lid;
        [Tooltip("How far the iris may wander from center, in world units")]
        public float maxIrisOffset = 0.35f;
        public float trackSpeed = 10f;

        [Header("Behavior")]
        [Tooltip("Only notice the player within this range; 0 = the whole scene")]
        public float wakeRange = 0f;
        [Tooltip("Speed that counts as 'moving'")]
        public float moveThreshold = 0.5f;
        [Tooltip("Seconds of stillness before the eye closes")]
        public float closeDelay = 1.5f;
        [Tooltip("Seconds between idle blinks while open")]
        public float blinkIntervalMin = 2.5f;
        public float blinkIntervalMax = 4.5f;
        public float blinkDuration = 0.1f;

        Player _player;
        Vector3 _irisHome;
        float _lastMoveTime = -999f;
        float _blinkTimer;
        float _blinkUntil;

        void Awake()
        {
            if (iris != null) _irisHome = iris.localPosition;
            _blinkTimer = Random.Range(blinkIntervalMin, blinkIntervalMax);
        }

        void Update()
        {
            if (_player == null)
            {
                _player = FindAnyObjectByType<Player>();
                if (_player == null) return;
            }

            float now = Time.realtimeSinceStartup;
            bool inRange = wakeRange <= 0f
                || Vector2.Distance(_player.transform.position, transform.position) <= wakeRange;
            bool moving = _player.rb != null
                && _player.rb.linearVelocity.magnitude > moveThreshold;
            if (moving && inRange && !PP_DeathHand.AnyPlaying && !PP_PauseMenu.IsOpen)
                _lastMoveTime = now;

            bool open = now - _lastMoveTime < closeDelay;

            // idle blinks while open
            if (open)
            {
                _blinkTimer -= Time.unscaledDeltaTime;
                if (_blinkTimer <= 0f)
                {
                    _blinkUntil = now + blinkDuration;
                    _blinkTimer = Random.Range(blinkIntervalMin, blinkIntervalMax);
                }
            }

            bool lidShown = !open || now < _blinkUntil;
            if (lid != null && lid.activeSelf != lidShown)
                lid.SetActive(lidShown);

            if (iris == null) return;

            Vector3 target = _irisHome;
            if (open)
            {
                Vector3 dir = _player.transform.position - transform.position;
                target = _irisHome + Vector3.ClampMagnitude(dir, maxIrisOffset);
                target.z = _irisHome.z;
            }
            iris.localPosition = Vector3.Lerp(iris.localPosition, target,
                1f - Mathf.Exp(-trackSpeed * Time.unscaledDeltaTime));
        }
    }
}
