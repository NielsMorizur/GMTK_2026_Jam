using UnityEngine.InputSystem;

namespace ParsaProto
{
    /// <summary>
    /// Left click is bound to Jump so it can double jump and grapple, but the
    /// ground jump stays keyboard-only. The grounded jump paths ask here.
    /// </summary>
    public static class PP_MouseInput
    {
        /// <summary>True on a frame whose jump press came from the mouse and
        /// nothing else: the ground jump ignores it.</summary>
        public static bool MouseOnlyJumpThisFrame
        {
            get
            {
                var mouse = Mouse.current;
                if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return false;

                var kb = Keyboard.current;
                if (kb == null) return true;
                return !kb.spaceKey.wasPressedThisFrame
                       && !kb.wKey.wasPressedThisFrame
                       && !kb.upArrowKey.wasPressedThisFrame;
            }
        }
    }
}
