using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// The countdown bar. Resize/move the BACKGROUND sprite and everything
    /// (fill, chips) re-fits to it, live in the editor too. One chip per form
    /// sits where its segment begins; the fill drains across the level's
    /// lifetime, tinted with the current form's color.
    /// </summary>
    [ExecuteAlways]
    public class PP_TimeBar : MonoBehaviour
    {
        public PP_AgeLoop player;

        [Tooltip("The fill sprite that drains (sized by this script).")]
        public Transform fill;

        [Tooltip("THE handle: resize/move this and the whole bar follows.")]
        public SpriteRenderer background;

        [Tooltip("One chip per form, in aging order. Replace sprites with head art later.")]
        public SpriteRenderer[] markers;

        [Tooltip("Fill height as a fraction of the background height.")]
        [Range(0.1f, 1f)] public float fillHeightFraction = 0.65f;

        [Tooltip("Chip size as a fraction of the background height.")]
        [Range(0.5f, 3f)] public float chipSizeFraction = 1.15f;

        SpriteRenderer _fillSprite;

        float Width => background != null ? Mathf.Max(background.transform.localScale.x - 0.2f, 0.1f) : 10f;
        float Height => background != null ? background.transform.localScale.y : 0.4f;

        void Awake()
        {
            if (fill != null) _fillSprite = fill.GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            if (background == null) return;
            if (_fillSprite == null && fill != null) _fillSprite = fill.GetComponent<SpriteRenderer>();

            if (!Application.isPlaying)
            {
                // editor preview: full bar, all chips, laid out live while you drag
                Layout(1f, markers != null ? markers.Length : 0);
                return;
            }

            bool hidden = player == null || player.timerPaused || player.freeCycle
                          || player.forms == null || player.forms.Length == 0;
            SetVisible(!hidden);
            if (hidden) return;

            int m = player.FormsThisLevel;
            float total = m * player.formDuration;
            if (total <= 0f) return;

            float remaining = (m - player.FormIndex - 1) * player.formDuration
                              + Mathf.Max(player.TimeLeftInForm, 0f);
            Layout(Mathf.Clamp01(remaining / total), m);

            if (_fillSprite != null && player.CurrentForm != null)
                _fillSprite.color = player.CurrentForm.color;

            if (markers != null)
                for (int i = 0; i < markers.Length; i++)
                    if (markers[i] != null && i < m && i < player.forms.Length)
                        markers[i].color = player.forms[i].color;
        }

        /// <summary>Fits fill + chips to the background. f = fraction remaining.</summary>
        void Layout(float f, int chipCount)
        {
            Vector3 c = background.transform.localPosition;

            if (fill != null)
            {
                fill.localScale = new Vector3(f * Width, Height * fillHeightFraction, 1f);
                fill.localPosition = new Vector3(c.x - Width * 0.5f + f * Width * 0.5f, c.y,
                                                 fill.localPosition.z);
            }

            if (markers == null) return;
            for (int i = 0; i < markers.Length; i++)
            {
                if (markers[i] == null) continue;
                bool active = i < chipCount;
                markers[i].enabled = active;
                if (!active) continue;

                float frac = (chipCount - i) / (float)chipCount;   // where form i's segment begins
                markers[i].transform.localPosition = new Vector3(
                    c.x - Width * 0.5f + frac * Width, c.y,
                    markers[i].transform.localPosition.z);
                float s = Height * chipSizeFraction;
                markers[i].transform.localScale = new Vector3(s, s, 1f);
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
