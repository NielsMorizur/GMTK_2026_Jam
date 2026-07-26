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
        int _armFrame = -1;

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
            _armFrame = Time.frameCount;
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

            // the R press that caused this re-arm must not immediately release it
            if (Time.frameCount <= _armFrame) return;

            if (_player == null) _player = FindAnyObjectByType<Player>();
            if (WakeInput(_player))
            {
                TimerActiveField.SetValue(_controller, true);
                _released = true;
            }
        }

        /// <summary>The one shared wake rule: a FRESH key press, or actually
        /// moving while holding a key. Spawn-settle physics with no key down
        /// never wakes a clock; holding W across a reset and walking does.
        /// The standalone TimeBar uses this too.</summary>
        public static bool WakeInput(Player player)
        {
            if (PP_DeathHand.AnyPlaying || PP_PauseMenu.IsOpen) return false;
            var kb = Keyboard.current;
            if (kb == null) return false;

            if (kb.anyKey.wasPressedThisFrame) return true;

            // mouse gameplay clicks (jump / swap) count as a fresh press too
            var mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.wasPressedThisFrame
                || mouse.rightButton.wasPressedThisFrame)) return true;

            // held-key wake counts MOVEMENT keys only - a held R from the
            // reset (or any other non-gameplay key) plus respawn physics
            // settle must not start the clock by itself
            bool movementHeld = kb.aKey.isPressed || kb.dKey.isPressed
                || kb.wKey.isPressed || kb.sKey.isPressed
                || kb.leftArrowKey.isPressed || kb.rightArrowKey.isPressed
                || kb.upArrowKey.isPressed || kb.downArrowKey.isPressed
                || kb.spaceKey.isPressed;

            return movementHeld
                   && player != null && player.rb != null
                   && player.rb.linearVelocity.magnitude > 0.5f;
        }
    }
}
