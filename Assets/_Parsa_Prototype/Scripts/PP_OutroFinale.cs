using UnityEngine;
using UnityEngine.EventSystems;
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

            Time.timeScale = 1f;
        }

        void Start()
        {
            PP_UiInput.Ensure();   // one owner for mouse and keyboard, always
        }

        /// <summary>The button's canvas only wakes up at the end, so its click
        /// plumbing is checked then: input owner alive, a raycaster on the
        /// canvas (no raycaster means the mouse sails straight past it), and
        /// the button selected so keyboard works without a first press.</summary>
        void ArmButton()
        {
            PP_UiInput.Ensure();
            if (againButton == null) return;

            var canvas = againButton.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                var root = canvas.rootCanvas;
                if (root.GetComponent<GraphicRaycaster>() == null)
                    root.gameObject.AddComponent<GraphicRaycaster>();
                if (root.transform.localScale == Vector3.zero)
                    root.transform.localScale = Vector3.one;
            }

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(againButton.gameObject);
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
                ArmButton();
            }
        }
    }
}
