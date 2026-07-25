using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ParsaProto
{
    /// <summary>
    /// Level hotkeys on the player: R restarts the level (same as dying),
    /// K skips to the next age stage by fast-forwarding the level timer,
    /// which reuses the LevelAgeController's own advance logic (K is inert
    /// on the final stage).
    /// </summary>
    public class PP_LevelHotkeys : MonoBehaviour
    {
        LevelAgeController _timer;
        FieldInfo _fiTimer, _fiSeconds, _fiIndex, _fiSequence, _fiActive;

        // TEMP [PPDBG] state watchers for the K-swap freeze hunt
        bool _dbgActive = true;
        int _dbgIdx = -1;
        float _dbgStuckTime;
        float _dbgPrevTimer = -1f;
        float _dbgTrace;   // >0: trace the raw timer every frame for N seconds

        void Start()
        {
            _timer = Object.FindAnyObjectByType<LevelAgeController>();

            var all = Object.FindObjectsByType<LevelAgeController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (all.Length != 1)
                Debug.LogWarning($"[PPDBG] {all.Length} LevelAgeControllers in scene! "
                    + string.Join(", ", System.Array.ConvertAll(all,
                        c => $"{c.gameObject.name}#{c.GetEntityId()} enabled={c.isActiveAndEnabled}")));
            if (_timer != null)
                Debug.Log($"[PPDBG] hotkeys bound to controller #{_timer.GetEntityId()} on '{_timer.gameObject.name}'");
            var t = typeof(LevelAgeController);
            const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
            _fiTimer = t.GetField("timer", F);
            _fiSeconds = t.GetField("secondsPerStage", F);
            _fiIndex = t.GetField("currentIndex", F);
            _fiSequence = t.GetField("ageSequence", F);
            _fiActive = t.GetField("timerActive", F);
        }

        void DbgWatch()
        {
            if (_timer == null || _fiActive == null || _fiIndex == null || _fiTimer == null) return;

            bool active = (bool)_fiActive.GetValue(_timer);
            int idx = (int)_fiIndex.GetValue(_timer);
            int prevIdx = _dbgIdx;
            if (active != _dbgActive)
                Debug.Log($"[PPDBG] timerActive {_dbgActive}->{active} idx={idx} ts={Time.timeScale:F2} t={Time.realtimeSinceStartup:F2}");
            if (idx != _dbgIdx && _dbgIdx >= 0)
                Debug.Log($"[PPDBG] stage {_dbgIdx}->{idx} active={active} ts={Time.timeScale:F2} t={Time.realtimeSinceStartup:F2}");
            _dbgActive = active;
            _dbgIdx = idx;

            // the raw clock: any backward jump that is not a stage advance or
            // reset is the smoking gun
            float tv = (float)_fiTimer.GetValue(_timer);
            float secs = (float)_fiSeconds.GetValue(_timer);
            float prev = _dbgPrevTimer;
            _dbgPrevTimer = tv;
            if (prev >= 0f && tv < prev - 0.0001f && idx == prevIdx)
                Debug.LogWarning($"[PPDBG] REWOUND: timer {prev:F3}->{tv:F3} idx={idx} active={active} ts={Time.timeScale:F2} anyPlaying={PP_DeathHand.AnyPlaying}");

            // timer marked running yet not moving at all while the game runs
            bool shouldMove = active && Time.timeScale > 0.5f && !PP_DeathHand.AnyPlaying;
            if (shouldMove && Mathf.Approximately(tv, prev))
                _dbgStuckTime += Time.unscaledDeltaTime;
            else _dbgStuckTime = 0f;
            if (_dbgStuckTime > 0.5f)
            {
                Debug.LogWarning($"[PPDBG] STUCK: timer={tv:F3}/{secs} idx={idx} active={active} ts={Time.timeScale:F2} anyPlaying={PP_DeathHand.AnyPlaying}");
                _dbgStuckTime = -99f;   // once per incident
            }

            // fine-grained trace window after each K press
            if (_dbgTrace > 0f)
            {
                _dbgTrace -= Time.unscaledDeltaTime;
                Debug.Log($"[PPDBG] trace timer={tv:F3} idx={idx} active={active} ts={Time.timeScale:F2} dt={Time.deltaTime:F4}");
            }
        }

        void Update()
        {
            DbgWatch();

            var kb = Keyboard.current;
            if (kb == null) return;

            // no inputs during ANY death presentation (hand or ghost)
            if (PP_DeathHand.AnyPlaying)
            {
                if (kb.kKey.wasPressedThisFrame)
                    Debug.Log("[PPDBG] K eaten: death presentation playing");
                return;
            }

            bool kPressed = kb.kKey.wasPressedThisFrame;
            string kCtx = kPressed ? DbgK() : null;

            if (kb.rKey.wasPressedThisFrame)
            {
                if (kPressed) Debug.Log($"[PPDBG] K eaten: R pressed same frame. {kCtx}");
                var reset = GetComponent<PP_LevelReset>();
                if (reset != null) reset.ResetLevel();
                else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }

            if (!kPressed) return;

            if (_timer == null || _fiTimer == null || _fiSeconds == null
                || _fiIndex == null || _fiSequence == null)
            {
                Debug.LogWarning($"[PPDBG] K dead: no controller/reflection ref. {kCtx}");
                return;
            }

            // anti-double-fire only: a short cooldown since the LAST swap.
            // dashing, double jumping, anything else never blocks K.
            var fx = GetComponent<PP_FormSwapEffects>();
            if (fx != null && Time.realtimeSinceStartup - fx.LastSwapTime < 0.25f)
            {
                Debug.Log($"[PPDBG] K eaten: swap cooldown. {kCtx}");
                return;
            }

            // K does nothing on the level's final form
            var seq = _fiSequence.GetValue(_timer) as System.Array;
            int idx = (int)_fiIndex.GetValue(_timer);
            if (seq == null || idx + 1 >= seq.Length)
            {
                Debug.Log($"[PPDBG] K inert: final stage. {kCtx}");
                return;
            }

            // jump the clock to the stage boundary; their Update does the rest
            _fiTimer.SetValue(_timer, (float)_fiSeconds.GetValue(_timer));
            _dbgTrace = 2.5f;   // per-frame trace for the next 2.5s
            Debug.Log($"[PPDBG] K wrote boundary: readback={(float)_fiTimer.GetValue(_timer):F3}. {kCtx}");
        }

        string DbgK()
        {
            var p = GetComponent<Player>();
            string age = p != null ? p.CurrentAge.ToString() : "?";
            string idx = _timer != null && _fiIndex != null ? _fiIndex.GetValue(_timer).ToString() : "?";
            string tv = _timer != null && _fiTimer != null ? ((float)_fiTimer.GetValue(_timer)).ToString("F2") : "?";
            string act = _timer != null && _fiActive != null ? _fiActive.GetValue(_timer).ToString() : "?";
            return $"[scene={SceneManager.GetActiveScene().name} form={age} idx={idx} timer={tv} active={act} ts={Time.timeScale:F2} anyPlaying={PP_DeathHand.AnyPlaying}]";
        }
    }
}
