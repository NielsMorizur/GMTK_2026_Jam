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

        [Header("Standalone (no level controller, e.g. main menu)")]
        [Tooltip("How many forms the bar shows when there is no controller. 0 = ask the GameManager what's unlocked (all 4 if there is none).")]
        [Range(0, 4)] public int standaloneForms = 0;
        [Tooltip("Seconds each form lasts in standalone mode.")]
        public float standaloneSecondsPerForm = 30f;
        [Tooltip("Count down and loop while standalone; off = sit at full time.")]
        public bool standaloneRun = true;
        [Tooltip("When the standalone timer empties: the hand claims the player if there is one, otherwise the scene reloads. Off = just loop silently.")]
        public bool standaloneResetOnEmpty = true;

        /// <summary>Remaining fraction of the whole level, 1 at start, 0 at time's up. The cake rides this.</summary>
        public float Progress01 { get; private set; } = 1f;

        // the age scale: each form owns a band on 100..0
        // (enum order: Elderly, Adult, Teen, Baby)
        static readonly float[] AgeBandStart = { 100f, 65f, 30f, 15f };
        static readonly float[] AgeBandEnd = { 65f, 30f, 15f, 0f };

        /// <summary>His current age on the 100-0 scale. The cake shows this.</summary>
        public float CurrentAgeValue { get; private set; } = 100f;

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
                Layout(1f, EvenFracs(markers != null ? markers.Length : 0));
                PreviewColors();
                return;
            }

            if (ageController == null && player == null && !_searched)
            {
                _searched = true;
                ageController = Object.FindAnyObjectByType<LevelAgeController>();
            }

            if (ageController != null) UpdateFromTeamController();
            else if (player != null) UpdateFromPrototype();
            else UpdateStandalone();
        }

        // ---------- standalone path (main menu, hub) ----------
        float _standaloneElapsed;
        Player _standalonePlayer;
        bool _standaloneSearched;
        bool _standaloneArmed = true;   // waits for input when a player exists
        int _standaloneArmFrame = -1;

        int StandaloneFormCount()
        {
            if (standaloneForms > 0) return Mathf.Clamp(standaloneForms, 1, 4);

            var gm = GameManager.Instance;
            if (gm == null) return 4;

            string lvl = gm.GetLevel();
            if (lvl != null && lvl.StartsWith("Level")
                && int.TryParse(lvl.Substring(5), out int n))
                return Mathf.Clamp(n, 1, 4);
            return 4;   // "End": everything unlocked
        }

        void UpdateStandalone()
        {
            int count = StandaloneFormCount();
            float secs = Mathf.Max(1f, standaloneSecondsPerForm);
            float total = count * secs;

            // same rule as the levels: with a player in the scene, the clock
            // holds until real input. Player-less scenes (main menu) run free.
            if (!_standaloneSearched)
            {
                _standaloneSearched = true;
                _standalonePlayer = Object.FindAnyObjectByType<Player>();
            }
            if (_standaloneArmed && _standalonePlayer != null
                && Time.frameCount > _standaloneArmFrame)
            {
                if (PP_TimerGate.WakeInput(_standalonePlayer)) _standaloneArmed = false;
            }
            bool running = standaloneRun && !PP_DeathHand.AnyPlaying
                           && !PP_PauseMenu.IsOpen
                           && (_standalonePlayer == null || !_standaloneArmed);

            if (running)
            {
                _standaloneElapsed += Time.unscaledDeltaTime;
                if (_standaloneElapsed >= total)
                {
                    if (standaloneResetOnEmpty)
                    {
                        // park at empty through the death presentation; the
                        // reset at the end refills and re-arms the clock
                        _standaloneElapsed = total;
                        StandaloneTimeout();
                    }
                    else
                    {
                        _standaloneElapsed = 0f;   // silent loop
                    }
                }
            }

            // manual K-swaps drag the clock forward to that form's band, so
            // the bar and the form never disagree
            if (_standalonePlayer != null)
            {
                int cur = Mathf.Clamp((int)_standalonePlayer.CurrentAge, 0, count - 1);
                if (cur * secs > _standaloneElapsed) _standaloneElapsed = cur * secs;
            }

            int idx = Mathf.Clamp((int)(_standaloneElapsed / secs), 0, count - 1);
            float stageElapsed = Mathf.Clamp01(_standaloneElapsed / secs - idx);
            CurrentAgeValue = Mathf.Lerp(AgeBandStart[idx], AgeBandEnd[idx], stageElapsed);

            // ...and the clock transforms him at each band threshold, exactly
            // like a level (no LevelAgeController here to do it)
            if (_standalonePlayer != null && idx > (int)_standalonePlayer.CurrentAge)
                _standalonePlayer.SetAge((AgeStage)idx);

            SetVisible(true);
            Layout(Mathf.Clamp01(1f - _standaloneElapsed / total), EvenFracs(count));

            if (_fillSprite != null && stageColors != null && idx < stageColors.Length)
                _fillSprite.color = stageColors[idx];
            if (markers != null)
                for (int i = 0; i < markers.Length && i < count; i++)
                    if (markers[i] != null) ApplyMarkerLook(markers[i], i);
        }

        /// <summary>Refills the standalone clock and freezes it until the next
        /// real input (called by PP_LevelReset so R in the hub behaves exactly
        /// like a level reset).</summary>
        public void ResetStandaloneClock()
        {
            _standaloneElapsed = 0f;
            _standaloneArmed = true;
            _standaloneArmFrame = Time.frameCount;
        }

        void StandaloneTimeout()
        {
            // a player in the scene (the hub): time out for real, same sound
            // and hand claim as a level timeout
            var reset = Object.FindAnyObjectByType<PP_LevelReset>();
            if (reset != null)
            {
                var sfx = reset.GetComponent<PP_SfxPlayer>();
                if (sfx != null) sfx.PlayDie();
                reset.DieAndReset();
                return;
            }
            // no player (the main menu): start the scene over
            PP_ScreenFade.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        // ---------- team path (Level1+) ----------
        void UpdateFromTeamController()
        {
            // death presentation: hold the last honest reading so the player
            // can look up and SEE the empty clock; the reset refreshes it
            if (PP_DeathHand.AnyPlaying) return;
            var seq = _fiSequence != null ? _fiSequence.GetValue(ageController) as System.Array : null;
            if (seq == null || seq.Length == 0 || _fiIndex == null || _fiSeconds == null) return;

            int count = seq.Length;
            int idx = Mathf.Clamp((int)_fiIndex.GetValue(ageController), 0, count - 1);
            float secs = (float)_fiSeconds.GetValue(ageController);
            if (secs <= 0f) return;

            // the NUMBER follows the age bands: it falls through the current
            // form's band as the stage elapses, so it drops 35 counts in an
            // elderly/adult stage but only 15 in teen/baby
            int curForm = Mathf.Clamp(System.Convert.ToInt32(seq.GetValue(idx)), 0, 3);
            float stageElapsed = 1f - Mathf.Clamp01(ageController.TimeRemainingInStage / secs);
            CurrentAgeValue = Mathf.Lerp(AgeBandStart[curForm], AgeBandEnd[curForm], stageElapsed);

            // the BAR stays time-based: markers evenly spaced, cake slides at
            // constant speed, every stage covers the same stretch of track
            float remaining = (count - idx - 1) * secs
                              + Mathf.Clamp(ageController.TimeRemainingInStage, 0f, secs);
            SetVisible(true);
            Layout(Mathf.Clamp01(remaining / (count * secs)), EvenFracs(count));

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
            float pf = Mathf.Clamp01(remaining / total);
            CurrentAgeValue = pf * 100f;
            Layout(pf, EvenFracs(m));

            if (_fillSprite != null && player.CurrentForm != null)
                _fillSprite.color = player.CurrentForm.color;
            if (markers != null)
                for (int i = 0; i < markers.Length; i++)
                    if (markers[i] != null && i < m && i < player.forms.Length)
                        markers[i].color = player.forms[i].color;
        }

        // ---------- shared layout ----------
        static float[] EvenFracs(int n)
        {
            var fr = new float[Mathf.Max(0, n)];
            for (int i = 0; i < fr.Length; i++)
                fr[i] = (n - i) / (float)n;
            return fr;
        }

        void Layout(float f, float[] markerFracs)
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
            int chipCount = markerFracs != null ? markerFracs.Length : 0;
            for (int i = 0; i < markers.Length; i++)
            {
                if (markers[i] == null) continue;
                bool active = i < chipCount;
                markers[i].enabled = active;
                if (!active) continue;

                markers[i].transform.localPosition = new Vector3(
                    c.x - Width * 0.5f + markerFracs[i] * Width, c.y,
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
