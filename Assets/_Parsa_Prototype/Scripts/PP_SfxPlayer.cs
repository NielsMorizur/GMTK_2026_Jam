using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Sound effect bank + triggers, lives on the player. Every slot holds any
    /// number of variations; each play picks one at random with a touch of
    /// pitch jitter. Slots left empty simply stay silent, so clips can be
    /// dropped in whenever they're ready.
    ///
    /// Auto-triggered: swap form, footsteps, jump, fart (double jump), dash,
    /// grapple attach/release, landing. Die and victory are fired by
    /// PP_Hazard / PP_LevelExit and survive the scene change.
    /// </summary>
    public class PP_SfxPlayer : MonoBehaviour
    {
        [System.Serializable]
        public class Sfx
        {
            public AudioClip[] variations;
            [Range(0f, 1f)] public float volume = 1f;
            [Range(0f, 0.3f)] public float pitchJitter = 0.05f;
        }

        [System.Serializable]
        public class FormSfx
        {
            public Sfx elderly = new Sfx();
            public Sfx adult = new Sfx();
            public Sfx teen = new Sfx();
            public Sfx baby = new Sfx();

            public Sfx For(AgeStage age) => age switch
            {
                AgeStage.Adult => adult,
                AgeStage.Teen => teen,
                AgeStage.Baby => baby,
                _ => elderly,
            };
        }

        [Header("One-offs")]
        public Sfx swapForm = new Sfx();
        public Sfx fart = new Sfx();            // old man only, on double jump
        public Sfx grappleAttached = new Sfx();
        public Sfx grappleReleased = new Sfx();
        public Sfx dash = new Sfx();            // teen
        public Sfx victory = new Sfx();

        [Header("Per form")]
        public FormSfx walking = new FormSfx();
        public FormSfx jump = new FormSfx();
        public FormSfx landing = new FormSfx();
        public FormSfx die = new FormSfx();

        [Header("Footsteps")]
        public float stepInterval = 0.35f;

        Player _player;
        Animator _anim;
        AudioSource _source;
        AgeStage _lastAge;
        bool _ageInit;
        bool _wasGrounded = true, _wasGrappling, _wasDoubleJump, _wasDash;
        float _stepTimer;

        void Awake()
        {
            _player = GetComponent<Player>();
            _anim = GetComponentInChildren<Animator>();
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
        }

        void Update()
        {
            if (_player == null) return;
            var age = _player.CurrentAge;

            // swap form (skip the initial assignment on level load)
            if (!_ageInit) { _lastAge = age; _ageInit = true; }
            else if (age != _lastAge) { Play(swapForm); _lastAge = age; }

            bool grounded = _player.groundDetected;

            // jump: left the ground going upward
            if (!grounded && _wasGrounded && _player.rb != null
                && _player.rb.linearVelocity.y > 0.01f)
                Play(jump.For(age));

            // landing
            if (grounded && !_wasGrounded)
                Play(landing.For(age));
            _wasGrounded = grounded;

            if (_anim != null)
            {
                var state = _anim.GetCurrentAnimatorStateInfo(0);

                bool dj = state.IsName("DoubleJump");
                if (dj && !_wasDoubleJump) Play(fart);
                _wasDoubleJump = dj;

                bool dashing = state.IsName("Dash");
                if (dashing && !_wasDash) Play(dash);
                _wasDash = dashing;
            }

            // grapple attach / release
            bool grappling = _player.IsGrappling;
            if (grappling && !_wasGrappling) Play(grappleAttached);
            if (!grappling && _wasGrappling) Play(grappleReleased);
            _wasGrappling = grappling;

            // footsteps
            if (grounded && _player.rb != null
                && Mathf.Abs(_player.rb.linearVelocity.x) > 0.1f)
            {
                _stepTimer -= Time.deltaTime;
                if (_stepTimer <= 0f)
                {
                    Play(walking.For(age));
                    _stepTimer = stepInterval;
                }
            }
            else
                _stepTimer = 0f;
        }

        public void Play(Sfx sfx)
        {
            var clip = Pick(sfx);
            if (clip == null || _source == null) return;
            _source.pitch = 1f + Random.Range(-sfx.pitchJitter, sfx.pitchJitter);
            _source.PlayOneShot(clip, sfx.volume);
        }

        public void PlayDie() =>
            PlayDetached(die.For(_player != null ? _player.CurrentAge : AgeStage.Elderly));

        public void PlayVictory() => PlayDetached(victory);

        // plays on a temp object that survives the scene reload following
        // death / level completion
        public void PlayDetached(Sfx sfx)
        {
            var clip = Pick(sfx);
            if (clip == null) return;
            var go = new GameObject("PP_SfxOneShot");
            DontDestroyOnLoad(go);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.pitch = 1f + Random.Range(-sfx.pitchJitter, sfx.pitchJitter);
            src.PlayOneShot(clip, sfx.volume);
            Destroy(go, clip.length + 0.5f);
        }

        static AudioClip Pick(Sfx sfx)
        {
            if (sfx == null || sfx.variations == null || sfx.variations.Length == 0)
                return null;
            return sfx.variations[Random.Range(0, sfx.variations.Length)];
        }
    }
}
