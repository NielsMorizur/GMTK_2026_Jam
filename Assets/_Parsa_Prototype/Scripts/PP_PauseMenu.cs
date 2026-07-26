using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ParsaProto
{
    /// <summary>
    /// Simple ESC menu. Lives across scenes, never duplicates. The panel and
    /// its contents are ordinary editable UI objects on the prefab; this
    /// script only shows/hides the panel and binds the screenshake toggle to
    /// the persisted setting.
    /// </summary>
    public class PP_PauseMenu : MonoBehaviour
    {
        public static PP_PauseMenu Instance { get; private set; }

        /// <summary>True while the menu is open (game paused).</summary>
        public static bool IsOpen { get; private set; }

        [Tooltip("The panel ESC / P shows and hides")]
        public GameObject panel;
        [Tooltip("Tick to see the panel in the editor for layout work; play mode always starts hidden")]
        public bool showPanelInEditor;
        public Toggle shakeToggle;
        public Slider masterSlider;
        public Slider sfxSlider;
        public Slider musicSlider;
        public Button mainMenuButton;

        [Header("Controls page")]
        public GameObject settingsPage;
        public GameObject controlsPage;
        public Button controlsButton;
        public Button controlsBackButton;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.activeSceneChanged += OnSceneChanged;

            if (shakeToggle != null)
            {
                shakeToggle.SetIsOnWithoutNotify(PP_Settings.ScreenShakeDisabled);
                shakeToggle.onValueChanged.AddListener(on => PP_Settings.ScreenShakeDisabled = on);
            }

            // same knobs as the main menu settings, saved live
            if (masterSlider != null)
            {
                masterSlider.SetValueWithoutNotify(PP_Settings.MasterVolume);
                masterSlider.onValueChanged.AddListener(v => PP_Settings.MasterVolume = v);
            }
            if (sfxSlider != null)
            {
                sfxSlider.SetValueWithoutNotify(PP_Settings.SfxVolume);
                sfxSlider.onValueChanged.AddListener(v => PP_Settings.SfxVolume = v);
            }
            if (musicSlider != null)
            {
                musicSlider.SetValueWithoutNotify(PP_Settings.MusicVolume);
                musicSlider.onValueChanged.AddListener(v => PP_Settings.MusicVolume = v);
            }
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(() =>
                {
                    Time.timeScale = 1f;
                    IsOpen = false;
                    if (panel != null) panel.SetActive(false);
                    PP_ScreenFade.LoadScene("MainMenu");
                });
            }

            if (controlsButton != null)
                controlsButton.onClick.AddListener(() => ShowControls(true));
            if (controlsBackButton != null)
                controlsBackButton.onClick.AddListener(() => ShowControls(false));

            ClaimInput();

            if (panel != null)
                panel.SetActive(false);
        }

        /// <summary>One persistent owner handles UI input everywhere; this
        /// just makes sure it exists and has retired any rival.</summary>
        void ClaimInput() => PP_UiInput.Ensure();

        void OnSceneChanged(Scene from, Scene to)
        {
            // nothing to pause in the menu or the outro, so it stands down
            if (to.name != "MainMenu" && to.name != "Outro") return;
            IsOpen = false;
            Time.timeScale = 1f;
            SceneManager.activeSceneChanged -= OnSceneChanged;
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                SceneManager.activeSceneChanged -= OnSceneChanged;
            }
        }

        void ShowControls(bool on)
        {
            if (settingsPage != null) settingsPage.SetActive(!on);
            if (controlsPage != null) controlsPage.SetActive(on);
        }

#if UNITY_EDITOR
        // the editor checkbox shows/hides the panel for layout work; SetActive
        // is not allowed inside OnValidate itself, hence the delayCall
        void OnValidate()
        {
            if (Application.isPlaying || panel == null) return;
            var p = panel;
            bool show = showPanelInEditor;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (p != null && p.activeSelf != show) p.SetActive(show);
            };
        }
#endif

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || panel == null) return;
            if (PP_DeathHand.AnyPlaying) return;   // no pausing mid-claim

            if (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame)
            {
                bool open = !panel.activeSelf;
                if (open)
                {
                    ShowControls(false);   // always land on the settings page
                    ClaimInput();          // the level may have taken input over
                    EventSystem.current?.SetSelectedGameObject(null);
                }
                panel.SetActive(open);
                IsOpen = open;
                Time.timeScale = open ? 0f : 1f;
            }
        }
    }
}
