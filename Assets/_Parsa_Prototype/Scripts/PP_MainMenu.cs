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

        void Awake()
        {
            Time.timeScale = 1f;   // whatever happened before, the menu runs clean

            var module = GetComponentInChildren<InputSystemUIInputModule>(true);
            if (module != null && module.actionsAsset == null)
                module.AssignDefaultActions();

            if (enterButton != null)
                enterButton.onClick.AddListener(() => SceneManager.LoadScene(enterScene));
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
