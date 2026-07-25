using UnityEngine;
using UnityEngine.EventSystems;

namespace ParsaProto
{
    /// <summary>
    /// Physical-feeling buttons: grow slightly on hover, squish on press,
    /// spring back on release. Runs on real time so paused menus feel alive.
    /// </summary>
    public class PP_ButtonJuice : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public float hoverScale = 1.06f;
        public float pressScale = 0.94f;
        public float speed = 14f;

        float _target = 1f;
        bool _hovering;

        public void OnPointerEnter(PointerEventData e) { _hovering = true; _target = hoverScale; }
        public void OnPointerExit(PointerEventData e) { _hovering = false; _target = 1f; }
        public void OnPointerDown(PointerEventData e) { _target = pressScale; }
        public void OnPointerUp(PointerEventData e) { _target = _hovering ? hoverScale : 1f; }

        void OnDisable()
        {
            _target = 1f;
            transform.localScale = Vector3.one;
        }

        void Update()
        {
            float s = Mathf.Lerp(transform.localScale.x, _target,
                1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
