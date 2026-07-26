using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ParsaProto
{
    /// <summary>
    /// The outro's ending: after the lines have said their piece, the screen
    /// fades to black and the "again?" button appears above it. Clicking it
    /// cuts back to the target scene.
    /// </summary>
    public class PP_OutroFinale : MonoBehaviour
    {
        [Tooltip("Seconds from scene start until the fade to black begins")]
        public float fadeAt = 8.5f;
        public float fadeTime = 1.5f;
        [Tooltip("Activated once the screen is black")]
        public GameObject buttonRoot;
        public Button againButton;
        [Tooltip("Shows the run's death tally above the button")]
        public TMPro.TextMeshProUGUI deathsText;
        public string targetScene = "MainMenu";

        float _t;
        bool _faded;
        bool _shown;

        void Awake()
        {
            if (buttonRoot != null) buttonRoot.SetActive(false);
            if (againButton != null)
                againButton.onClick.AddListener(() => PP_ScreenFade.CutTo(targetScene));

            // the UI input module arrives with no actions in this scene
            // (the menu assigns them itself) - without this, no clicks/hover
            var module = FindAnyObjectByType<InputSystemUIInputModule>();
            if (module != null && module.actionsAsset == null)
                module.AssignDefaultActions();
        }

        void Update()
        {
            _t += Time.unscaledDeltaTime;
            if (!_faded && _t >= fadeAt)
            {
                _faded = true;
                PP_ScreenFade.FadeOut(fadeTime);
            }
            if (!_shown && _t >= fadeAt + fadeTime)
            {
                _shown = true;
                if (deathsText != null)
                    deathsText.text = PP_DeathCounter.Count == 1
                        ? "1 death" : PP_DeathCounter.Count + " deaths";
                if (buttonRoot != null) buttonRoot.SetActive(true);
            }
        }
    }
}
