using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// The countdown bar: drains across the level's whole lifetime
    /// (formDuration x forms in this level). One checkpoint chip per form
    /// sits where that form's segment begins; the fill is tinted with the
    /// current form's color. Hidden in the hub and in free-cycle playground.
    /// </summary>
    public class PP_TimeBar : MonoBehaviour
    {
        public PP_AgeLoop player;

        [Tooltip("The fill sprite that drains (scaled/moved by this script).")]
        public Transform fill;
        public SpriteRenderer background;

        [Tooltip("One chip per form, in aging order. Replace sprites with head art later.")]
        public SpriteRenderer[] markers;

        [Tooltip("Bar width in world units (matches the background's width).")]
        public float barWidth = 10f;

        SpriteRenderer _fillSprite;

        void Awake()
        {
            if (fill != null) _fillSprite = fill.GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            bool hidden = player == null || player.timerPaused || player.freeCycle
                          || player.forms == null || player.forms.Length == 0;

            SetVisible(!hidden);
            if (hidden) return;

            int m = player.FormsThisLevel;
            float total = m * player.formDuration;
            if (total <= 0f) return;

            float remaining = (m - player.FormIndex - 1) * player.formDuration
                              + Mathf.Max(player.TimeLeftInForm, 0f);
            float f = Mathf.Clamp01(remaining / total);

            if (fill != null)
            {
                fill.localScale = new Vector3(f * barWidth, fill.localScale.y, 1f);
                fill.localPosition = new Vector3(-barWidth * 0.5f + f * barWidth * 0.5f,
                                                 fill.localPosition.y, fill.localPosition.z);
            }
            if (_fillSprite != null && player.CurrentForm != null)
                _fillSprite.color = player.CurrentForm.color;

            if (markers != null)
            {
                for (int i = 0; i < markers.Length; i++)
                {
                    if (markers[i] == null) continue;
                    bool active = i < m && i < player.forms.Length;
                    markers[i].enabled = active;
                    if (!active) continue;

                    float frac = (m - i) / (float)m;   // where form i's segment begins
                    markers[i].transform.localPosition = new Vector3(
                        -barWidth * 0.5f + frac * barWidth,
                        markers[i].transform.localPosition.y,
                        markers[i].transform.localPosition.z);
                    markers[i].color = player.forms[i].color;
                }
            }
        }

        void SetVisible(bool on)
        {
            if (background != null) background.enabled = on;
            if (_fillSprite != null) _fillSprite.enabled = on;
            if (!on && markers != null)
                foreach (var mk in markers)
                    if (mk != null) mk.enabled = false;
        }
    }
}
