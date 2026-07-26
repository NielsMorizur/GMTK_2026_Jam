using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace ParsaProto
{
    /// <summary>
    /// The one and only UI input owner. Menus, the pause screen and the outro
    /// each used to bring their own EventSystem; whichever registered first
    /// won and the others' buttons went dead. This boots one persistent
    /// EventSystem with input actions and retires every other one it meets,
    /// so mouse and keyboard work in every scene, always.
    /// </summary>
    public class PP_UiInput : MonoBehaviour
    {
        static PP_UiInput _i;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() => Ensure();

        public static void Ensure()
        {
            if (_i == null)
            {
                var go = new GameObject("PP_UiInput");
                DontDestroyOnLoad(go);
                go.AddComponent<EventSystem>();
                var module = go.AddComponent<InputSystemUIInputModule>();
                // assign while disabled, so the module's OnEnable sees actions
                // and actually starts feeding pointer events
                module.enabled = false;
                module.AssignDefaultActions();
                module.enabled = true;
                _i = go.AddComponent<PP_UiInput>();
                SceneManager.sceneLoaded += (s, m) => { if (_i != null) _i.Sweep(); };
            }
            _i.Sweep();
        }

        /// <summary>Retire every other EventSystem and make ours current.</summary>
        void Sweep()
        {
            var mine = GetComponent<EventSystem>();
            var all = FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var es in all)
            {
                if (es == mine) continue;
                var mod = es.GetComponent<BaseInputModule>();
                if (mod != null) Destroy(mod);
                Destroy(es);
            }

            var module = GetComponent<InputSystemUIInputModule>();
            if (module != null && module.actionsAsset == null)
            {
                module.enabled = false;
                module.AssignDefaultActions();
                module.enabled = true;
            }
            mine.enabled = true;
            EventSystem.current = mine;
        }

        void OnDestroy()
        {
            if (_i == this) _i = null;
        }
    }
}
