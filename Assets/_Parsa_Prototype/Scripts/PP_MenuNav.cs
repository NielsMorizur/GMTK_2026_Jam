using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ParsaProto
{
    /// <summary>
    /// Celeste-style keyboard menu navigation: W/S (or arrows) move the
    /// selection through whatever is visible, Space / J / Enter activates,
    /// A/D nudge a selected slider. Works alongside the mouse.
    /// </summary>
    public class PP_MenuNav : MonoBehaviour
    {
        [Tooltip("Slider step per A/D press")]
        public float sliderStep = 0.1f;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            bool up = kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame;
            bool down = kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame;
            // Enter is deliberately absent: the UI input module already
            // submits on Enter, and doing it here too would double-fire
            bool submit = kb.spaceKey.wasPressedThisFrame || kb.jKey.wasPressedThisFrame;
            bool left = kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame;
            bool right = kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame;

            if (!(up || down || submit || left || right)) return;

            var items = Visible();
            if (items.Count == 0) return;

            var es = EventSystem.current;
            GameObject cur = es != null ? es.currentSelectedGameObject : null;
            int idx = items.FindIndex(s => s.gameObject == cur);

            if (up || down)
            {
                if (idx < 0) idx = 0;                       // first press just lands on top
                else idx = (idx + (down ? 1 : -1) + items.Count) % items.Count;
                es.SetSelectedGameObject(items[idx].gameObject);
                return;
            }

            if (idx < 0) return;   // nothing selected: submit/adjust do nothing
            var sel = items[idx];

            if (submit)
            {
                ExecuteEvents.Execute(sel.gameObject,
                    new BaseEventData(es), ExecuteEvents.submitHandler);
                return;
            }

            if (sel is Slider slider)
                slider.value = Mathf.Clamp(slider.value + (right ? sliderStep : -sliderStep),
                    slider.minValue, slider.maxValue);
        }

        List<Selectable> Visible()
        {
            var all = GetComponentsInChildren<Selectable>(false);
            var list = new List<Selectable>();
            foreach (var s in all)
                if (s.gameObject.activeInHierarchy && s.interactable)
                    list.Add(s);
            // top to bottom
            list.Sort((a, b) => b.transform.position.y.CompareTo(a.transform.position.y));
            return list;
        }
    }
}
