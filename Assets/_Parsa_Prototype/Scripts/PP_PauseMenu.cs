using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
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
        public Toggle shakeToggle;
        public Slider masterSlider;
        public Slider sfxSlider;
        public Slider musicSlider;
        public Button mainMenuButton;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

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

            // make sure the UI input module has actions to click with
            var module = GetComponentInChildren<InputSystemUIInputModule>(true);
            if (module != null && module.actionsAsset == null)
                module.AssignDefaultActions();

            if (panel != null)
                panel.SetActive(false);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || panel == null) return;
            if (PP_DeathHand.AnyPlaying) return;   // no pausing mid-claim

            if (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame)
            {
                bool open = !panel.activeSelf;
                panel.SetActive(open);
                IsOpen = open;
                Time.timeScale = open ? 0f : 1f;
            }
        }
    }
}
