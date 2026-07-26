using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ParsaProto
{
    /// <summary>
    /// Main menu logic. All the visuals are ordinary editable scene objects;
    /// this only wires the buttons: Enter loads the hub, Controls and
    /// Settings swap panels, Back returns home. The screenshake toggle binds
    /// to the same persisted setting as the pause menu.
    /// </summary>
    public class PP_MainMenu : MonoBehaviour
    {
        public string enterScene = "MainRoom";
        [Tooltip("Enter cuts instantly instead of fading to black")]
        public bool enterCut = true;

        [Header("Panels")]
        public GameObject homePanel;
        public GameObject controlsPanel;
        public GameObject settingsPanel;

        [Header("Buttons")]
        public Button enterButton;
        public Button controlsButton;
        public Button settingsButton;
        public Button controlsBackButton;
        public Button settingsBackButton;
        public Toggle shakeToggle;
        public Slider masterSlider;
        public Slider sfxSlider;
        public Slider musicSlider;

        void Awake()
        {
            Time.timeScale = 1f;   // whatever happened before, the menu runs clean
            PP_DeathCounter.Reset();   // fresh run, fresh tally

            var module = GetComponentInChildren<InputSystemUIInputModule>(true);
            if (module != null && module.actionsAsset == null)
                module.AssignDefaultActions();

            if (enterButton != null)
                enterButton.onClick.AddListener(() =>
                {
                    if (enterCut) PP_ScreenFade.CutTo(enterScene);
                    else PP_ScreenFade.LoadScene(enterScene);
                });
            if (controlsButton != null)
                controlsButton.onClick.AddListener(() => Show(controlsPanel));
            if (settingsButton != null)
                settingsButton.onClick.AddListener(() => Show(settingsPanel));
            if (controlsBackButton != null)
                controlsBackButton.onClick.AddListener(() => Show(homePanel));
            if (settingsBackButton != null)
                settingsBackButton.onClick.AddListener(() => Show(homePanel));

            if (shakeToggle != null)
            {
                shakeToggle.SetIsOnWithoutNotify(PP_Settings.ScreenShakeDisabled);
                shakeToggle.onValueChanged.AddListener(on => PP_Settings.ScreenShakeDisabled = on);
            }

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

            PP_Settings.ApplyAudio();
            Show(homePanel);
        }

        void Show(GameObject panel)
        {
            if (homePanel != null) homePanel.SetActive(panel == homePanel);
            if (controlsPanel != null) controlsPanel.SetActive(panel == controlsPanel);
            if (settingsPanel != null) settingsPanel.SetActive(panel == settingsPanel);
        }
    }
}
