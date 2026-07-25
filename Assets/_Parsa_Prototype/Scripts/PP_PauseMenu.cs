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
