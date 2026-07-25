using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Backdrop layer: a gentle autonomous drift keeps it alive, and when the
    /// camera moves the layer slides against it by its depth factor, so
    /// nearer layers travel more. Parent it under the camera; 0 depth means
    /// infinitely far (glued to the view).
    /// </summary>
    public class PP_ParallaxLayer : MonoBehaviour
    {
        [Range(0f, 1f)]
        [Tooltip("How strongly the layer slides against camera movement. 0 = infinitely far")]
        public float depth = 0.1f;

        [Header("Idle drift")]
        public float driftAmplitude = 0.1f;
        public float driftPeriod = 12f;
        [Tooltip("Offset so layers don't sway in unison")]
        public float driftPhase = 0f;

        Vector3 _baseLocal;
        Transform _cam;
        Vector3 _camStart;

        void Start()
        {
            _baseLocal = transform.localPosition;
            _cam = transform.parent;
            if (_cam != null) _camStart = _cam.position;
        }

        void LateUpdate()
        {
            Vector3 camOffset = _cam != null ? _cam.position - _camStart : Vector3.zero;

            float t = Time.time * (Mathf.PI * 2f) / Mathf.Max(0.1f, driftPeriod) + driftPhase;
            Vector3 drift = new Vector3(Mathf.Sin(t), Mathf.Sin(t * 0.63f) * 0.5f, 0f)
                            * driftAmplitude;

            Vector3 p = _baseLocal + drift - camOffset * depth;
            p.z = _baseLocal.z;
            transform.localPosition = p;
        }
    }
}
