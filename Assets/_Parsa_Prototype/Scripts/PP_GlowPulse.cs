using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ParsaProto
{
    /// <summary>
    /// Slow breathing pulse for a glowing prop: the core and halo sprites fade
    /// in and out together and the 2D light follows, so it reads as emitting
    /// rather than as a flat card. Everything is authored in the scene; this
    /// only animates brightness.
    /// </summary>
    [ExecuteAlways]
    public class PP_GlowPulse : MonoBehaviour
    {
        public SpriteRenderer core;
        public SpriteRenderer halo;
        public Light2D light2d;

        [Tooltip("How far the brightness swings, 0 to 1")]
        public float pulseAmount = 0.18f;
        [Tooltip("Seconds for one full breath")]
        public float pulsePeriod = 2.6f;
        [Tooltip("Extra scale the halo breathes by")]
        public float haloDrift = 0.04f;

        float _coreA = -1f, _haloA, _lightI;
        Vector3 _haloScale;

        void OnEnable()
        {
            if (core != null) _coreA = core.color.a;
            if (halo != null)
            {
                _haloA = halo.color.a;
                _haloScale = halo.transform.localScale;
            }
            if (light2d != null) _lightI = light2d.intensity;
        }

        void Update()
        {
            if (_coreA < 0f) OnEnable();

            float t = Application.isPlaying ? Time.unscaledTime : (float)UnityEditor_Time();
            float k = 1f + Mathf.Sin(t * (Mathf.PI * 2f) / Mathf.Max(0.05f, pulsePeriod)) * pulseAmount;

            if (core != null) SetAlpha(core, _coreA * k);
            if (halo != null)
            {
                SetAlpha(halo, _haloA * k);
                halo.transform.localScale = _haloScale * (1f + (k - 1f) * (haloDrift / Mathf.Max(0.0001f, pulseAmount)));
            }
            if (light2d != null) light2d.intensity = _lightI * k;
        }

        static double UnityEditor_Time()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorApplication.timeSinceStartup;
#else
            return Time.unscaledTimeAsDouble;
#endif
        }

        static void SetAlpha(SpriteRenderer r, float a)
        {
            var c = r.color;
            c.a = Mathf.Clamp01(a);
            r.color = c;
        }
    }
}
