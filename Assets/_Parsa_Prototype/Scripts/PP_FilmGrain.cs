using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Faux film grain: cycles noise frames with a tiny random jitter so the
    /// speckle never sits still. Runs on real time; screen coverage comes
    /// from the object's scale like the vignette.
    /// </summary>
    public class PP_FilmGrain : MonoBehaviour
    {
        public Sprite[] frames;
        public float fps = 12f;
        [Tooltip("Random positional jitter per frame, world units")]
        public float jitter = 0.25f;

        SpriteRenderer _sr;
        Vector3 _home;
        float _t;
        int _i;

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _home = transform.localPosition;
        }

        void Update()
        {
            if (_sr == null || frames == null || frames.Length == 0) return;

            _t += Time.unscaledDeltaTime * fps;
            int frame = (int)_t % frames.Length;
            if (frame != _i)
            {
                _i = frame;
                _sr.sprite = frames[_i];
                transform.localPosition = _home + new Vector3(
                    Random.Range(-jitter, jitter), Random.Range(-jitter, jitter), 0f);
            }
        }
    }
}
