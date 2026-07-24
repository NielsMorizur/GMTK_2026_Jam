using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Gentle ambient float for background artwork: a slow drift around
    /// wherever the object is placed in the scene.
    /// </summary>
    public class PP_FloatBob : MonoBehaviour
    {
        public float verticalAmplitude = 0.4f;
        public float horizontalAmplitude = 0.15f;
        [Tooltip("Seconds for one full bob cycle")]
        public float period = 6f;

        Vector3 _base;

        void OnEnable()
        {
            _base = transform.localPosition;
        }

        void Update()
        {
            if (!Application.isPlaying) return;

            float w = Mathf.PI * 2f / Mathf.Max(0.1f, period);
            float t = Time.time * w;
            transform.localPosition = _base + new Vector3(
                Mathf.Sin(t * 0.5f) * horizontalAmplitude,
                Mathf.Sin(t) * verticalAmplitude,
                0f);
        }
    }
}
