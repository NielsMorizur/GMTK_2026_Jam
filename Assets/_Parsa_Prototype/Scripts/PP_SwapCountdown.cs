using System.Reflection;
using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// The player himself flashes for the last seconds before the next form
    /// takes over, quickening as the swap gets closer. Only when another form
    /// is actually coming; the final stage and the hub stay calm.
    /// </summary>
    public class PP_SwapCountdown : MonoBehaviour
    {
        [Tooltip("Seconds before the swap the flashing starts")]
        public float countdownFrom = 3f;
        [Tooltip("Base flash speed (radians/sec of the pulse)")]
        public float flashSpeed = 10f;
        [Tooltip("How much faster the flash gets right before the swap")]
        public float speedRamp = 1.6f;
        [Range(0f, 1f)]
        [Tooltip("How strongly the incoming form's color shows at the pulse peak")]
        public float maxTint = 0.85f;

        LevelAgeController _ctrl;
        FieldInfo _fiIndex, _fiSequence;
        bool _searched;
        SpriteRenderer _sprite;
        PP_FormSwapEffects _fx;
        Player _player;
        bool _flashing;
        Color _home;
        AgeStage _flashAge;

        void Awake()
        {
            const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;
            _fiIndex = typeof(LevelAgeController).GetField("currentIndex", F);
            _fiSequence = typeof(LevelAgeController).GetField("ageSequence", F);

            var anim = GetComponentInChildren<Animator>();
            _sprite = anim != null ? anim.GetComponent<SpriteRenderer>() : null;
            _fx = GetComponent<PP_FormSwapEffects>();   // the shared form palette
            _player = GetComponent<Player>();
        }

        void Update()
        {
            if (_sprite == null) return;
            if (PP_DeathHand.AnyPlaying) return;   // death owns the sprite

            if (_ctrl == null && !_searched)
            {
                _searched = true;
                _ctrl = FindAnyObjectByType<LevelAgeController>();
            }
            if (_ctrl == null || _fiIndex == null || _fiSequence == null)
            {
                StopFlash();
                return;
            }

            var seq = _fiSequence.GetValue(_ctrl) as System.Array;
            int idx = (int)_fiIndex.GetValue(_ctrl);
            bool hasNext = seq != null && idx + 1 < seq.Length;

            float rem = _ctrl.TimeRemainingInStage;
            if (!hasNext || rem <= 0f || rem > countdownFrom)
            {
                StopFlash();
                return;
            }

            // pulse toward the INCOMING form's color, quickening near the swap
            if (!_flashing)
            {
                _flashing = true;
                _home = _sprite.color;
                _flashAge = _player != null ? _player.CurrentAge : AgeStage.Elderly;
            }

            Color target = _home;
            int next = System.Convert.ToInt32(seq.GetValue(idx + 1));
            if (_fx != null && _fx.formColors != null
                && next >= 0 && next < _fx.formColors.Length)
            {
                target = _fx.formColors[next];
                target.a = _home.a;
            }

            float urgency = 1f + speedRamp * (1f - rem / Mathf.Max(0.01f, countdownFrom));
            float k = (Mathf.Sin(Time.time * flashSpeed * urgency) + 1f) * 0.5f * maxTint;
            _sprite.color = Color.Lerp(_home, target, k);
        }

        void StopFlash()
        {
            if (!_flashing || _sprite == null) return;
            _flashing = false;

            // if the form swapped while flashing, SetAge already painted the
            // NEW form's resting color; restoring the captured one would dress
            // the new form in the old form's tint and make the swap look dead
            if (_player == null || _player.CurrentAge == _flashAge)
                _sprite.color = _home;
        }
    }
}
