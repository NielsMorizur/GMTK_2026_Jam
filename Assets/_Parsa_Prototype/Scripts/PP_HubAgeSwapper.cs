using UnityEngine;
using UnityEngine.InputSystem;

namespace ParsaProto
{
    /// <summary>
    /// Hub-only form swapping: K cycles through the forms unlocked by level
    /// progress (Level1 = old man only, Level2 adds adult, and so on).
    /// Sleeps in any scene with a level timer, where K fast-forwards the
    /// timer instead.
    /// </summary>
    public class PP_HubAgeSwapper : MonoBehaviour
    {
        Player _player;
        bool _hub;

        void Awake()
        {
            _player = GetComponent<Player>();
        }

        void Start()
        {
            _hub = FindAnyObjectByType<LevelAgeController>() == null;
        }

        int UnlockedForms()
        {
            var gm = GameManager.Instance;
            if (gm == null) return 4;   // solo scene testing: no cap

            string lvl = gm.GetLevel();
            if (lvl != null && lvl.StartsWith("Level")
                && int.TryParse(lvl.Substring(5), out int n))
                return Mathf.Clamp(n, 1, 4);

            return 4;                   // "End": everything unlocked
        }

        void Update()
        {
            if (!_hub || _player == null) return;

            var kb = Keyboard.current;
            if (kb == null || !kb.kKey.wasPressedThisFrame) return;

            // one swap at a time: K waits out the swap presentation
            var fx = GetComponent<PP_FormSwapEffects>();
            if (fx != null && fx.IsPresenting) return;

            int count = UnlockedForms();
            if (count <= 1) return;

            int next = ((int)_player.CurrentAge + 1) % count;
            _player.SetAge((AgeStage)next);
        }
    }
}
