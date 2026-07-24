using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Movable level start marker. The marker sprite shows in the editor and
    /// hides itself in play mode. Soft resets teleport the player here.
    /// </summary>
    public class PP_SpawnPoint : MonoBehaviour
    {
        void Awake()
        {
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
        }
    }
}
