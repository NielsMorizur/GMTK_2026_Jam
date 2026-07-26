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
            if (PP_DeathHand.AnyPlaying) return;

            var kb = Keyboard.current;
            var mouse = Mouse.current;
            bool pressed = (kb != null && kb.kKey.wasPressedThisFrame)
                || (mouse != null && mouse.rightButton.wasPressedThisFrame);
            if (!pressed) return;

            // anti-double-fire only: short cooldown since the last swap
            var fx = GetComponent<PP_FormSwapEffects>();
            if (fx != null && Time.realtimeSinceStartup - fx.LastSwapTime < 0.25f)
            {
                Debug.Log("[PPDBG] hub K eaten: swap cooldown");
                return;
            }

            int count = UnlockedForms();
            if (count <= 1)
            {
                Debug.Log("[PPDBG] hub K inert: only 1 form unlocked");
                return;
            }

            // one-way aging: K stops at the youngest unlocked form.
            // R resets back to the elder, same as in levels.
            int next = (int)_player.CurrentAge + 1;
            if (next >= count)
            {
                Debug.Log($"[PPDBG] hub K inert: youngest unlocked form (form={_player.CurrentAge} unlocked={count})");
                return;
            }
            _player.SetAge((AgeStage)next);
        }
    }
}
