using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// On-screen control hints (world-space text parented to the camera).
    /// The reset hint is always on; the ability hint follows the current form.
    /// </summary>
    public class PP_HintUI : MonoBehaviour
    {
        public PP_AgeLoop player;
        public TextMesh resetHint;
        public TextMesh abilityHint;

        void Update()
        {
            if (resetHint != null)
                resetHint.text = "Press R to reset";

            if (abilityHint == null || player == null) return;

            switch (player.FormIndex)
            {
                case 0: abilityHint.text = "Space to double jump"; break;
                case 1: abilityHint.text = "Space to grapple"; break;
                case 2: abilityHint.text = "Shift to dash"; break;
                default: abilityHint.text = ""; break;
            }
        }
    }
}
