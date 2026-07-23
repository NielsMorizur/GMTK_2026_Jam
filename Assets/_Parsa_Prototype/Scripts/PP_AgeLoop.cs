using UnityEngine;
using UnityEngine.InputSystem;

namespace ParsaProto
{
    /// <summary>
    /// The reverse-aging countdown loop (GMTK 2026 "Countdown").
    /// Cycles old man -> adult -> teen -> baby on a fixed timer, applying each
    /// form's stats to the controller. When the baby's time runs out the player
    /// dies and respawns at the spawn point as the old man, and the loop repeats.
    /// </summary>
    [RequireComponent(typeof(PP_PlayerController))]
    public class PP_AgeLoop : MonoBehaviour
    {
        [System.Serializable]
        public class PP_Form
        {
            public string formName;
            public Color color = Color.white;
            public float moveSpeed = 7f;
            public int jumpHeightBlocks = 3;
            [Tooltip("Signature ability: dash (teen).")]
            public bool canDash = false;
            [Tooltip("Signature ability: fart jump / mid-air boost (old man).")]
            public bool canAirJump = false;
            [Tooltip("Signature ability: grappling gun (adult).")]
            public bool canGrapple = false;
        }

        [Tooltip("Aging order: index 0 is what you respawn as.")]
        public PP_Form[] forms;

        [Tooltip("Seconds per form. GDD says ~30; set low (e.g. 8) for testing.")]
        public float formDuration = 30f;

        [Tooltip("Optional world-space label showing form + countdown.")]
        public TextMesh label;

        [Tooltip("Die after falling this many units below the level's spawn point.")]
        public float fallKillDepth = 25f;

        [Tooltip("Real-time freeze on every form swap, to sell the transition.")]
        public float swapFreezeTime = 0.18f;

        [Tooltip("The baby (last form) shrinks to this fraction of the player's normal size.")]
        public float babySizeMultiplier = 0.7f;

        [Tooltip("Fullscreen tint overlay (sprite under the camera); colored per form.")]
        public SpriteRenderer screenTint;

        [Tooltip("How strong the screen tint is. 0 = off.")]
        [Range(0f, 0.5f)] public float tintAlpha = 0.1f;

        float _freezeUntil = -1f;

        PP_PlayerController _controller;
        PP_Grapple _grapple;
        SpriteRenderer _sprite;
        Rigidbody2D _rb;
        Vector3 _spawnPoint;
        Vector3 _baseScale;

        /// <summary>Forms available this level (set by PP_LevelManager). 0 = all.</summary>
        [System.NonSerialized] public int activeFormCount;

        /// <summary>True in the hub: no countdown, no aging.</summary>
        [System.NonSerialized] public bool timerPaused;

        /// <summary>Playground mode: no countdown, E cycles all forms freely.</summary>
        [System.NonSerialized] public bool freeCycle;

        /// <summary>Ready gate: level is armed but waits for Space.</summary>
        [System.NonSerialized] public bool waitingToStart;


        int MaxForms => activeFormCount > 0
            ? Mathf.Min(activeFormCount, forms.Length)
            : forms.Length;

        int _formIndex;
        float _timer;

        public int FormIndex => _formIndex;
        public float TimeLeftInForm => _timer;
        public int FormsThisLevel => MaxForms;
        public PP_Form CurrentForm =>
            (forms != null && forms.Length > 0) ? forms[_formIndex] : null;

        void Awake()
        {
            _controller = GetComponent<PP_PlayerController>();
            _grapple = GetComponent<PP_Grapple>();
            _sprite = GetComponent<SpriteRenderer>();
            _rb = GetComponent<Rigidbody2D>();
            _spawnPoint = transform.position;
            _baseScale = transform.localScale;
        }

        void Start()
        {
            ApplyForm(0);
        }

        /// <summary>PP_LevelManager points this at the current level's spawn.</summary>
        public void SetSpawn(Vector3 p)
        {
            _spawnPoint = p;
        }

        void Update()
        {
            if (forms == null || forms.Length == 0) return;

            // swap hitstop: everything waits, then resumes
            if (_freezeUntil >= 0f)
            {
                if (Time.unscaledTime >= _freezeUntil)
                {
                    Time.timeScale = 1f;
                    _freezeUntil = -1f;
                }
                else return;
            }

            if (transform.position.y < _spawnPoint.y - fallKillDepth)
                RestartLevel();                       // pit fall = death (relative to this level)

            var kb = Keyboard.current;

            if (waitingToStart)
            {
                bool inputMade = kb != null &&
                    (kb.aKey.isPressed || kb.dKey.isPressed
                     || kb.leftArrowKey.isPressed || kb.rightArrowKey.isPressed
                     || kb.wKey.isPressed || kb.sKey.isPressed
                     || kb.upArrowKey.isPressed || kb.downArrowKey.isPressed
                     || kb.spaceKey.isPressed || kb.leftShiftKey.isPressed
                     || kb.eKey.isPressed);
                if (inputMade)
                {
                    waitingToStart = false;
                    _timer = formDuration;    // the clock starts on the first input
                }
                else
                {
                    if (label != null) label.text = "- move to start -";
                    return;
                }
            }

            if (kb != null && kb.rKey.wasPressedThisFrame)
            {
                RestartLevel();
                return;
            }

            if (freeCycle)
            {
                if (kb != null && kb.eKey.wasPressedThisFrame)
                {
                    ApplyForm((_formIndex + 1) % forms.Length);
                    HitStop();
                }
                if (label != null)
                    label.text = "";
                return;
            }

            if (!timerPaused)
            {
                bool ageDownPressed = kb != null && kb.eKey.wasPressedThisFrame
                                      && _formIndex + 1 < MaxForms;   // E does nothing on the final form

                _timer -= Time.deltaTime;
                if (_timer <= 0f || ageDownPressed)
                {
                    if (_formIndex + 1 < MaxForms)
                    {
                        ApplyForm(_formIndex + 1);   // age down
                        HitStop();
                    }
                    else
                    {
                        RestartLevel();              // last allowed form expired: die, level resets
                    }
                }
            }

            if (label != null)
                label.text = "";
        }

        void ApplyForm(int index)
        {
            _formIndex = index;
            _timer = formDuration;

            PP_Form f = forms[index];

            // only the baby (last form) changes size; everyone else keeps the authored scale
            bool isBaby = index == forms.Length - 1;
            transform.localScale = isBaby ? _baseScale * babySizeMultiplier : _baseScale;

            // the form shows on the screen, not the character: a light fullscreen tint
            if (screenTint != null)
                screenTint.color = new Color(f.color.r, f.color.g, f.color.b, tintAlpha);
            _controller.moveSpeed = f.moveSpeed;
            _controller.jumpHeightBlocks = f.jumpHeightBlocks;
            _controller.dashEnabled = f.canDash;
            _controller.airJumpEnabled = f.canAirJump;
            if (_grapple != null)
            {
                _grapple.grappleEnabled = f.canGrapple;
                if (!f.canGrapple) _grapple.ForceRelease();
            }

        }

        /// <summary>Full level reset (R): player to spawn as old man, walker back to its start.</summary>
        public void RestartLevel()
        {
            ReturnToStart();
            ApplyForm(0);
            foreach (var w in Object.FindObjectsByType<PP_Walker>(FindObjectsSortMode.None))
                w.ResetToStart();
            foreach (var b in Object.FindObjectsByType<PP_PushBlock>(FindObjectsSortMode.None))
                b.ResetToStart();

            Time.timeScale = 1f;
            _freezeUntil = -1f;

            // every level (re)start waits for the first input to start the clock
            waitingToStart = !timerPaused;
        }

        void HitStop()
        {
            if (swapFreezeTime <= 0f) return;
            Time.timeScale = 0f;
            _freezeUntil = Time.unscaledTime + swapFreezeTime;
        }

        /// <summary>Hazards: send the body back to spawn without resetting the loop.</summary>
        public void ReturnToStart()
        {
            transform.position = _spawnPoint;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
        }
    }
}
