using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ParsaProto
{
    /// <summary>
    /// Level hotkeys on the player: R restarts the level (same as dying),
    /// K skips to the next age stage by fast-forwarding the level timer,
    /// which reuses the LevelAgeController's own advance logic (K is inert
    /// on the final stage).
    /// </summary>
    public class PP_LevelHotkeys : MonoBehaviour
    {
        LevelAgeController _timer;
        FieldInfo _fiTimer, _fiSeconds, _fiIndex, _fiSequence;

        void Start()
        {
            _timer = Object.FindAnyObjectByType<LevelAgeController>();
            var t = typeof(LevelAgeController);
            const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
            _fiTimer = t.GetField("timer", F);
            _fiSeconds = t.GetField("secondsPerStage", F);
            _fiIndex = t.GetField("currentIndex", F);
            _fiSequence = t.GetField("ageSequence", F);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            // no inputs during ANY death presentation (hand or ghost)
            if (PP_DeathHand.AnyPlaying) return;

            if (kb.rKey.wasPressedThisFrame)
            {
                var reset = GetComponent<PP_LevelReset>();
                if (reset != null) reset.ResetLevel();
                else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }

            if (kb.kKey.wasPressedThisFrame && _timer != null
                && _fiTimer != null && _fiSeconds != null
                && _fiIndex != null && _fiSequence != null)
            {
                // anti-double-fire only: a short cooldown since the LAST swap.
                // dashing, double jumping, anything else never blocks K.
                var fx = GetComponent<PP_FormSwapEffects>();
                if (fx != null && Time.realtimeSinceStartup - fx.LastSwapTime < 0.25f) return;

                // E does nothing on the level's final form
                var seq = _fiSequence.GetValue(_timer) as System.Array;
                int idx = (int)_fiIndex.GetValue(_timer);
                if (seq == null || idx + 1 >= seq.Length) return;

                // jump the clock to the stage boundary; their Update does the rest
                _fiTimer.SetValue(_timer, (float)_fiSeconds.GetValue(_timer));
            }
        }
    }
}
