using System.Reflection;
using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Soft level reset: R and hazards call this instead of reloading the
    /// scene. The player returns to the PP_SpawnPoint (or wherever he stood
    /// at load), the age timer rewinds to stage one and freezes until the
    /// next input, exactly like a fresh load minus the loading hitch.
    /// </summary>
    public class PP_LevelReset : MonoBehaviour
    {
        const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;
        static readonly FieldInfo FiIndex = typeof(LevelAgeController).GetField("currentIndex", F);
        static readonly FieldInfo FiTimer = typeof(LevelAgeController).GetField("timer", F);
        static readonly FieldInfo FiActive = typeof(LevelAgeController).GetField("timerActive", F);
        static readonly FieldInfo FiSeq = typeof(LevelAgeController).GetField("ageSequence", F);

        Player _player;
        Vector3 _fallbackSpawn;

        void Awake()
        {
            _player = GetComponent<Player>();
        }

        void Start()
        {
            _fallbackSpawn = transform.position;
        }

        public void ResetLevel()
        {
            var spawn = Object.FindAnyObjectByType<PP_SpawnPoint>();
            transform.position = spawn != null ? spawn.transform.position : _fallbackSpawn;

            if (_player != null)
            {
                _player.ReleaseGrapple();
                if (_player.rb != null) _player.rb.linearVelocity = Vector2.zero;
                _player.hasDoubleJump = true;
                _player.hasAirDash = true;
            }

            var ctrl = Object.FindAnyObjectByType<LevelAgeController>();
            if (ctrl != null && FiIndex != null && FiTimer != null && FiActive != null)
            {
                FiIndex.SetValue(ctrl, 0);
                FiTimer.SetValue(ctrl, 0f);

                var seq = FiSeq != null ? FiSeq.GetValue(ctrl) as AgeStage[] : null;
                if (seq != null && seq.Length > 0)
                {
                    FiActive.SetValue(ctrl, true);
                    if (_player != null) _player.SetAge(seq[0]);
                }
            }
            else if (_player != null)
            {
                // no level timer here (the hub): resetting still means
                // going back to the elder
                _player.SetAge(AgeStage.Elderly);
            }

            // frozen until the next input, same as a fresh load
            var gate = GetComponent<PP_TimerGate>();
            if (gate != null) gate.Rearm();

            // going back to stage one is not a dramatic "form swap"
            var swapFx = GetComponent<PP_FormSwapEffects>();
            if (swapFx != null) swapFx.ResyncAge();
            var sfx = GetComponent<PP_SfxPlayer>();
            if (sfx != null) sfx.ResyncAge();
        }
    }
}
