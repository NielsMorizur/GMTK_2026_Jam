using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ParsaProto
{
    /// <summary>
    /// Holds the level age timer frozen until the first input after every scene
    /// load (so also after each reset). Reaches into LevelAgeController via
    /// reflection so Niels' script stays untouched.
    /// </summary>
    public class PP_TimerGate : MonoBehaviour
    {
        static readonly FieldInfo TimerActiveField =
            typeof(LevelAgeController).GetField("timerActive",
                BindingFlags.Instance | BindingFlags.NonPublic);

        static readonly FieldInfo AgeSequenceField =
            typeof(LevelAgeController).GetField("ageSequence",
                BindingFlags.Instance | BindingFlags.NonPublic);

        LevelAgeController _controller;
        Player _player;
        bool _released;

        void Start()
        {
            Engage();
        }

        /// <summary>Re-freeze the timer until the next input (soft resets).</summary>
        public void Rearm()
        {
            Engage();
        }

        void Engage()
        {
            _released = false;
            _controller = FindAnyObjectByType<LevelAgeController>();

            // nothing to gate: no controller in this scene, or it has no age
            // sequence (its own Start already parked the timer permanently)
            var seq = _controller != null
                ? AgeSequenceField?.GetValue(_controller) as AgeStage[]
                : null;
            if (_controller == null || seq == null || seq.Length == 0
                || TimerActiveField == null)
            {
                _released = true;
                return;
            }

            TimerActiveField.SetValue(_controller, false);
        }

        void Update()
        {
            if (_released) return;

            // a FRESH key press only: keys already held when the scene starts
            // (alt-tab, the click into the game view, R held from a reset)
            // must not start the clock
            bool anyInput = Keyboard.current != null
                            && Keyboard.current.anyKey.wasPressedThisFrame;

            // ...but actually MOVING counts too. Holding W/A across a reset is
            // not a fresh press, yet walking around with a frozen clock is
            // plainly "playing" - the freeze is only for standing at spawn.
            if (!anyInput)
            {
                if (_player == null) _player = FindAnyObjectByType<Player>();
                anyInput = _player != null && _player.rb != null
                           && _player.rb.linearVelocity.magnitude > 0.1f
                           && !PP_DeathHand.AnyPlaying;
            }

            if (anyInput)
            {
                TimerActiveField.SetValue(_controller, true);
                _released = true;
            }
        }
    }
}
