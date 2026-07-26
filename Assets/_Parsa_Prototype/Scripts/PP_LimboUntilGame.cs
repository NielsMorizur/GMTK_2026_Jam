using UnityEngine;
using UnityEngine.SceneManagement;

namespace ParsaProto
{
    /// <summary>
    /// Pre-game music: plays the limbo track from the main menu, survives the
    /// cut into the intro, and dies the moment the game proper starts (any
    /// scene not in its list). While it lives, the anthem stems stay silent;
    /// when it dies they breathe in. Place one in the menu AND the intro so
    /// direct scene testing works; duplicates remove themselves.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class PP_LimboUntilGame : MonoBehaviour
    {
        public static PP_LimboUntilGame Instance { get; private set; }

        [Range(0f, 1f)] public float volume = 0.5f;
        [Tooltip("Scenes where the limbo track keeps playing")]
        public string[] scenes = { "MainMenu", "Intro", "Intro (new)" };

        AudioSource _src;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _src = GetComponent<AudioSource>();
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        void OnSceneChanged(Scene from, Scene to)
        {
            foreach (var s in scenes)
                if (to.name == s) return;
            Destroy(gameObject);   // game started: hand over to the stems
        }

        void Update()
        {
            if (PP_MusicPlayer.Instance != null)
                PP_MusicPlayer.Instance.Suppress(true);
            if (_src != null)
                _src.volume = volume * PP_Settings.MusicVolume;
        }

        void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            if (Instance == this) Instance = null;
            if (PP_MusicPlayer.Instance != null)
                PP_MusicPlayer.Instance.Suppress(false);
        }
    }
}
