using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// The countdown cake rides the TimeBar: it slides along the track as the
    /// level timer (Niels' controller, via the bar) runs down, showing a
    /// number that counts from 100 to 0. Three candle digits at 100, two from
    /// 99, one from 9. Purely visual, drives nothing.
    /// </summary>
    public class PP_CountdownCake : MonoBehaviour
    {
        [Tooltip("Auto-found in the scene when empty.")]
        public PP_TimeBar bar;
        [Tooltip("Digit sprites 0-9, in order.")]
        public Sprite[] digits = new Sprite[10];
        [Tooltip("Left-to-right digit renderers on the cake (3 covers 100).")]
        public SpriteRenderer[] digitRenderers = new SpriteRenderer[3];
        [Tooltip("The cake body renderer.")]
        public SpriteRenderer cake;

        [Header("Look")]
        public float digitSpacing = 0.34f;
        [Tooltip("How far above the bar the cake rides.")]
        public float heightAboveBar = 0.85f;
        [Tooltip("Value shown at a full bar.")]
        public int startNumber = 100;

        bool _searched;

        void Update()
        {
            if (!Application.isPlaying) return;

            if (bar == null && !_searched)
            {
                _searched = true;
                bar = Object.FindFirstObjectByType<PP_TimeBar>();
            }
            if (bar == null || bar.background == null) return;

            bool visible = bar.background.enabled;
            SetVisible(visible);
            if (!visible) return;

            float f = bar.Progress01;

            // ride the track: full time = right end, time's up = left end,
            // matching the direction the fill drains
            var b = bar.background.bounds;
            transform.position = new Vector3(
                Mathf.Lerp(b.min.x, b.max.x, f),
                b.center.y + heightAboveBar,
                bar.background.transform.position.z);

            ShowNumber(Mathf.CeilToInt(f * startNumber));
        }

        void ShowNumber(int n)
        {
            n = Mathf.Clamp(n, 0, 999);
            int count = n >= 100 ? 3 : n >= 10 ? 2 : 1;

            for (int i = 0; i < digitRenderers.Length; i++)
            {
                var r = digitRenderers[i];
                if (r == null) continue;

                // toggle the whole digit object so flame + glow follow along
                bool on = i < count;
                if (r.gameObject.activeSelf != on) r.gameObject.SetActive(on);
                if (!on) continue;

                int divisor = (int)Mathf.Pow(10, count - 1 - i);
                int d = (n / divisor) % 10;
                if (digits != null && d < digits.Length && digits[d] != null)
                    r.sprite = digits[d];

                var lp = r.transform.localPosition;
                lp.x = (i - (count - 1) * 0.5f) * digitSpacing;
                r.transform.localPosition = lp;
            }
        }

        void SetVisible(bool on)
        {
            if (cake != null) cake.enabled = on;
            if (on) return;
            foreach (var r in digitRenderers)
                if (r != null && r.gameObject.activeSelf)
                    r.gameObject.SetActive(false);
        }
    }
}
