using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Persistent music: placed in the hub, survives every scene load, and
    /// destroys any duplicate of itself when the hub is revisited.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class PP_MusicPlayer : MonoBehaviour
    {
        public static PP_MusicPlayer Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // persists only when standalone; as a child (e.g. on the player)
            // it simply lives and dies with its parent
            if (transform.parent == null)
                DontDestroyOnLoad(gameObject);
        }
    }
}
