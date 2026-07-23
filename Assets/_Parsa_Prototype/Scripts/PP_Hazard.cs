using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Hazard layer: anything painted on this tilemap sends the player
    /// back to the start of the level on touch.
    /// </summary>
    public class PP_Hazard : MonoBehaviour
    {
        void OnTriggerEnter2D(Collider2D other)
        {
            var loop = other.GetComponent<PP_AgeLoop>();
            if (loop != null) loop.RestartLevel();   // hazard = death: full reset, old man
        }
    }
}
