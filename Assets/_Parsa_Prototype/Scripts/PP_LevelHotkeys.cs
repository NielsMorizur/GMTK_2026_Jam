using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ParsaProto
{
    /// <summary>
    /// Level hotkeys on the player: R restarts the level (same as dying),
    /// E skips to the next age stage by fast-forwarding the level timer,
    /// which reuses the LevelAgeController's own advance logic (so E on the
    /// final stage resets, exactly like the timer running out).
    /// </summary>
    public class PP_LevelHotkeys : MonoBehaviour
    {
        LevelAgeController _timer;
        FieldInfo _fiTimer, _fiSeconds, _fiIndex, _fiSequence;

        void Start()
        {
            _timer = Object.FindFirstObjectByType<LevelAgeController>();
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

            if (kb.rKey.wasPressedThisFrame)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }

            if (kb.eKey.wasPressedThisFrame && _timer != null
                && _fiTimer != null && _fiSeconds != null
                && _fiIndex != null && _fiSequence != null)
            {
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
