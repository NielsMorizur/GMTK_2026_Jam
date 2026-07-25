using UnityEngine;
using UnityEngine.EventSystems;

namespace ParsaProto
{
    /// <summary>
    /// Physical-feeling buttons: grow slightly on hover, squish on press,
    /// spring back on release. Runs on real time so paused menus feel alive.
    /// </summary>
    public class PP_ButtonJuice : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        public float hoverScale = 1.06f;
        public float pressScale = 0.94f;
        public float speed = 14f;

        /// <summary>The button currently highlighted (hover or keyboard), if any.
        /// The menu eye watches this.</summary>
        public static PP_ButtonJuice CurrentFocus { get; private set; }
        /// <summary>True when the focus came from the mouse.</summary>
        public static bool FocusFromPointer { get; private set; }
        /// <summary>Real time of the last focus change; the eye uses this to
        /// doze off after a quiet spell.</summary>
        public static float LastFocusEvent { get; private set; }

        float _target = 1f;
        bool _hovering;

        public void OnPointerEnter(PointerEventData e)
        {
            _hovering = true; _target = hoverScale;
            CurrentFocus = this; FocusFromPointer = true;
            LastFocusEvent = Time.realtimeSinceStartup;
        }

        public void OnPointerExit(PointerEventData e)
        {
            _hovering = false; _target = 1f;
            if (CurrentFocus == this) CurrentFocus = null;
            LastFocusEvent = Time.realtimeSinceStartup;
        }

        public void OnPointerDown(PointerEventData e) { _target = pressScale; }
        public void OnPointerUp(PointerEventData e) { _target = _hovering ? hoverScale : 1f; }

        public void OnSelect(BaseEventData e)
        {
            _target = hoverScale;
            CurrentFocus = this; FocusFromPointer = false;
            LastFocusEvent = Time.realtimeSinceStartup;
        }

        public void OnDeselect(BaseEventData e)
        {
            if (!_hovering) _target = 1f;
            if (CurrentFocus == this && !FocusFromPointer) CurrentFocus = null;
        }

        void OnDisable()
        {
            _target = 1f;
            transform.localScale = Vector3.one;
            if (CurrentFocus == this) CurrentFocus = null;
        }

        void Update()
        {
            float s = Mathf.Lerp(transform.localScale.x, _target,
                1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
