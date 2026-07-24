using System.Reflection;
using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// The countdown bar. Two possible data sources:
    ///  - the team's LevelAgeController + its age sequence (auto-found in the
    ///    scene if the slot is empty), or
    ///  - the old prototype PP_AgeLoop (TestParsa).
    /// Resize/move the BACKGROUND sprite and fill + chips re-fit live, in the
    /// editor too. Chip colors come from stageColors (age enum order).
    /// Reads the team controller's private fields via reflection on purpose,
    /// so no team script needs editing; swap to real getters when they exist.
    /// </summary>
    [ExecuteAlways]
    public class PP_TimeBar : MonoBehaviour
    {
        [Header("Data source (either one)")]
        public LevelAgeController ageController;
        public PP_AgeLoop player;

        [Header("Pieces")]
        [Tooltip("The fill sprite that drains (sized by this script).")]
        public Transform fill;
        [Tooltip("THE handle: resize/move this and the whole bar follows.")]
        public SpriteRenderer background;
        [Tooltip("One chip per stage of the level's sequence. Replace sprites with head art later.")]
        public SpriteRenderer[] markers;

        [Header("Look")]
        [Range(0.1f, 1f)] public float fillHeightFraction = 0.65f;
        [Range(0.5f, 3f)] public float chipSizeFraction = 1.15f;
        [Tooltip("Colors by age, in enum order: Elderly, Adult, Teen, Baby.")]
        public Color[] stageColors = new Color[]
        {
            new Color(0.7f, 0.7f, 0.75f, 1f),
            new Color(0.3254902f, 0.7137255f, 0.88235295f, 1f),
            new Color(0.9529412f, 0.5294118f, 0.26666668f, 1f),
            new Color(0.98f, 0.7f, 0.18f, 1f),
        };
        [Tooltip("Form art by age, same order. When set, the threshold marker shows this sprite untinted instead of a colored chip.")]
        public Sprite[] stageSprites = new Sprite[4];

        /// <summary>Remaining fraction of the whole level, 1 at start, 0 at time's up. The cake rides this.</summary>
        public float Progress01 { get; private set; } = 1f;

        SpriteRenderer _fillSprite;
        FieldInfo _fiIndex, _fiSequence, _fiSeconds;
        bool _searched;

        float Width => background != null ? Mathf.Max(background.transform.localScale.x - 0.2f, 0.1f) : 10f;
        float Height => background != null ? background.transform.localScale.y : 0.4f;

        void Awake()
        {
            if (fill != null) _fillSprite = fill.GetComponent<SpriteRenderer>();
            var t = typeof(LevelAgeController);
            const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
            _fiIndex = t.GetField("currentIndex", F);
            _fiSequence = t.GetField("ageSequence", F);
            _fiSeconds = t.GetField("secondsPerStage", F);
        }

        void Update()
        {
            if (background == null) return;
            if (_fillSprite == null && fill != null) _fillSprite = fill.GetComponent<SpriteRenderer>();

            if (!Application.isPlaying)
            {
                Layout(1f, markers != null ? markers.Length : 0);
                PreviewColors();
                return;
            }

            if (ageController == null && player == null && !_searched)
            {
                _searched = true;
                ageController = Object.FindFirstObjectByType<LevelAgeController>();
            }

            if (ageController != null) UpdateFromTeamController();
            else if (player != null) UpdateFromPrototype();
        }

        // ---------- team path (Level1+) ----------
        void UpdateFromTeamController()
        {
            var seq = _fiSequence != null ? _fiSequence.GetValue(ageController) as System.Array : null;
            if (seq == null || seq.Length == 0 || _fiIndex == null || _fiSeconds == null) return;

            int count = seq.Length;
            int idx = Mathf.Clamp((int)_fiIndex.GetValue(ageController), 0, count - 1);
            float secs = (float)_fiSeconds.GetValue(ageController);
            if (secs <= 0f) return;

            float remaining = (count - idx - 1) * secs
                              + Mathf.Clamp(ageController.TimeRemainingInStage, 0f, secs);
            SetVisible(true);
            Layout(Mathf.Clamp01(remaining / (count * secs)), count);

            if (_fillSprite != null) _fillSprite.color = StageColor(seq, idx);
            if (markers != null)
                for (int i = 0; i < markers.Length && i < count; i++)
                    if (markers[i] != null)
                        ApplyMarkerLook(markers[i], System.Convert.ToInt32(seq.GetValue(i)));
        }

        void ApplyMarkerLook(SpriteRenderer marker, int age)
        {
            var art = (stageSprites != null && age >= 0 && age < stageSprites.Length)
                ? stageSprites[age] : null;
            if (art != null)
            {
                marker.sprite = art;
                marker.color = Color.white;
            }
            else if (stageColors != null && age >= 0 && age < stageColors.Length)
            {
                marker.color = stageColors[age];
            }
        }

        Color StageColor(System.Array seq, int i)
        {
            int age = System.Convert.ToInt32(seq.GetValue(i));
            return (stageColors != null && age >= 0 && age < stageColors.Length)
                ? stageColors[age] : Color.white;
        }

        // ---------- old prototype path (TestParsa) ----------
        void UpdateFromPrototype()
        {
            bool hidden = player.timerPaused || player.freeCycle
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

        // ---------- shared layout ----------
        void Layout(float f, int chipCount)
        {
            Progress01 = f;
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

                float frac = (chipCount - i) / (float)chipCount;   // segment start
                markers[i].transform.localPosition = new Vector3(
                    c.x - Width * 0.5f + frac * Width, c.y,
                    markers[i].transform.localPosition.z);
                float s = Height * chipSizeFraction;
                markers[i].transform.localScale = new Vector3(s, s, 1f);
            }
        }

        void PreviewColors()
        {
            if (markers == null) return;
            for (int i = 0; i < markers.Length; i++)
                if (markers[i] != null)
                    ApplyMarkerLook(markers[i], i);
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
