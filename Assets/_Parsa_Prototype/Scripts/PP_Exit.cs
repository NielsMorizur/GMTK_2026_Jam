using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Level exit zone. Touching it with the required form advances to the
    /// next level. The real gate should be level design (a baby-sized tunnel,
    /// a teen-height ledge); the form check here is a safety net.
    /// </summary>
    public class PP_Exit : MonoBehaviour
    {
        public PP_LevelManager manager;

        [Tooltip("Form index required to use this exit. -1 = any form.")]
        public int requiredForm = -1;

        // Stay, not Enter: aging into the right form while already standing
        // inside the exit must still trigger it.
        void OnTriggerStay2D(Collider2D other)
        {
            var loop = other.GetComponent<PP_AgeLoop>();
            if (loop == null) return;
            if (requiredForm >= 0 && loop.FormIndex != requiredForm) return;
            if (manager != null) manager.LevelCompleted();
        }
    }
}
