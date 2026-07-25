using System.Collections;
using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Menu entrance + idle motion: elements fade/slide in staggered on load,
    /// then the title floats gently. Purely visual, layout is authored in the
    /// scene and everything returns to those authored positions.
    /// </summary>
    public class PP_MenuJuice : MonoBehaviour
    {
        [Tooltip("Animated in this order; first slides from above, rest from below")]
        public RectTransform[] entranceOrder;
        public float entranceDuration = 0.35f;
        public float entranceStagger = 0.09f;
        public float slideDistance = 46f;

        [Header("Title float")]
        public RectTransform bobTarget;
        public float bobAmplitude = 7f;
        public float bobPeriod = 3.5f;

        Vector2[] _homes;
        Vector2 _bobHome;
        bool _entranceDone;

        IEnumerator Start()
        {
            if (entranceOrder == null || entranceOrder.Length == 0)
            {
                _entranceDone = true;
                yield break;
            }

            _homes = new Vector2[entranceOrder.Length];
            var groups = new CanvasGroup[entranceOrder.Length];
            for (int i = 0; i < entranceOrder.Length; i++)
            {
                if (entranceOrder[i] == null) continue;
                _homes[i] = entranceOrder[i].anchoredPosition;
                groups[i] = entranceOrder[i].GetComponent<CanvasGroup>();
                if (groups[i] == null)
                    groups[i] = entranceOrder[i].gameObject.AddComponent<CanvasGroup>();
                groups[i].alpha = 0f;
            }
            if (bobTarget != null) _bobHome = bobTarget.anchoredPosition;

            float total = entranceDuration + entranceStagger * (entranceOrder.Length - 1);
            float t = 0f;
            while (t < total)
            {
                t += Time.unscaledDeltaTime;
                for (int i = 0; i < entranceOrder.Length; i++)
                {
                    if (entranceOrder[i] == null) continue;
                    float local = Mathf.Clamp01((t - entranceStagger * i) / entranceDuration);
                    float e = local * local * (3f - 2f * local);   // smoothstep
                    float dir = i == 0 ? 1f : -1f;                 // title from above
                    entranceOrder[i].anchoredPosition =
                        _homes[i] + new Vector2(0f, dir * slideDistance * (1f - e));
                    if (groups[i] != null) groups[i].alpha = e;
                }
                yield return null;
            }

            for (int i = 0; i < entranceOrder.Length; i++)
            {
                if (entranceOrder[i] == null) continue;
                entranceOrder[i].anchoredPosition = _homes[i];
                if (groups[i] != null) groups[i].alpha = 1f;
            }
            _entranceDone = true;
        }

        void Update()
        {
            if (!_entranceDone || bobTarget == null) return;
            float w = Mathf.PI * 2f / Mathf.Max(0.1f, bobPeriod);
            bobTarget.anchoredPosition =
                _bobHome + new Vector2(0f, Mathf.Sin(Time.unscaledTime * w) * bobAmplitude);
        }
    }
}
