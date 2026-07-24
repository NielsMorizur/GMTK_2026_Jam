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
        bool _released;

        void Start()
        {
            _controller = FindFirstObjectByType<LevelAgeController>();

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

            bool anyInput =
                (Keyboard.current != null && Keyboard.current.anyKey.isPressed) ||
                (Mouse.current != null && (Mouse.current.leftButton.isPressed
                                           || Mouse.current.rightButton.isPressed));

            if (anyInput)
            {
                TimerActiveField.SetValue(_controller, true);
                _released = true;
            }
        }
    }
}
