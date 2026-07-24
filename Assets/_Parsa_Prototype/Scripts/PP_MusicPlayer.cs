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
            DontDestroyOnLoad(gameObject);
        }
    }
}
