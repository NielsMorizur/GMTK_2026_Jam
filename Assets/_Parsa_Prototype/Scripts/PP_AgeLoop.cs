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
            [Tooltip("Body size in blocks. Taller than 1 = can't fit 1-block gaps.")]
            public Vector2 scale = Vector2.one;
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

        [Tooltip("Falling below this y returns the player to the level spawn.")]
        public float fallKillY = -20f;

        PP_PlayerController _controller;
        PP_Grapple _grapple;
        SpriteRenderer _sprite;
        Rigidbody2D _rb;
        Vector3 _spawnPoint;

        /// <summary>Forms available this level (set by PP_LevelManager). 0 = all.</summary>
        [System.NonSerialized] public int activeFormCount;

        /// <summary>True in the hub: no countdown, no aging.</summary>
        [System.NonSerialized] public bool timerPaused;

        /// <summary>Playground mode: no countdown, E cycles all forms freely.</summary>
        [System.NonSerialized] public bool freeCycle;

        /// <summary>Ready gate: level is armed but waits for Space.</summary>
        [System.NonSerialized] public bool waitingToStart;

        bool _startRequested;

        int MaxForms => activeFormCount > 0
            ? Mathf.Min(activeFormCount, forms.Length)
            : forms.Length;

        int _formIndex;
        float _timer;

        public int FormIndex => _formIndex;
        public PP_Form CurrentForm =>
            (forms != null && forms.Length > 0) ? forms[_formIndex] : null;

        void Awake()
        {
            _controller = GetComponent<PP_PlayerController>();
            _grapple = GetComponent<PP_Grapple>();
            _sprite = GetComponent<SpriteRenderer>();
            _rb = GetComponent<Rigidbody2D>();
            _spawnPoint = transform.position;
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

            if (transform.position.y < fallKillY)
                RestartLevel();                       // pit fall = death

            var kb = Keyboard.current;

            if (waitingToStart)
            {
                if (kb != null && kb.spaceKey.wasPressedThisFrame)
                    _startRequested = true;
                if (label != null && CurrentForm != null)
                    label.text = CurrentForm.formName + "  - press Space";
                return;
            }

            if (kb != null && kb.rKey.wasPressedThisFrame)
            {
                RestartLevel();
                return;
            }

            if (freeCycle)
            {
                if (kb != null && kb.eKey.wasPressedThisFrame)
                    ApplyForm((_formIndex + 1) % forms.Length);
                if (label != null && CurrentForm != null)
                    label.text = CurrentForm.formName;
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
                        ApplyForm(_formIndex + 1);   // age down
                    else
                        RestartLevel();              // last allowed form expired: die, level resets
                }
            }

            if (label != null && CurrentForm != null)
                label.text = timerPaused
                    ? CurrentForm.formName
                    : $"{CurrentForm.formName}  {Mathf.CeilToInt(Mathf.Max(_timer, 0f))}";
        }

        void LateUpdate()
        {
            // unlock AFTER every Update ran this frame, so the starting Space
            // press can never double as a buffered jump
            if (_startRequested)
            {
                _startRequested = false;
                waitingToStart = false;
                _controller.inputLocked = false;
                _timer = formDuration;    // countdown starts NOW, not at level entry
            }
        }

        void ApplyForm(int index)
        {
            _formIndex = index;
            _timer = formDuration;

            PP_Form f = forms[index];
            transform.localScale = new Vector3(f.scale.x, f.scale.y, 1f);
            if (_sprite != null) _sprite.color = f.color;
            _controller.moveSpeed = f.moveSpeed;
            _controller.jumpHeightBlocks = f.jumpHeightBlocks;
            _controller.dashEnabled = f.canDash;
            _controller.airJumpEnabled = f.canAirJump;
            if (_grapple != null)
            {
                _grapple.grappleEnabled = f.canGrapple;
                if (!f.canGrapple) _grapple.ForceRelease();
            }

            // keep the label readable: cancel the parent's scale and sit above the head
            if (label != null)
            {
                label.transform.localScale = new Vector3(1f / f.scale.x, 1f / f.scale.y, 1f);
                label.transform.localPosition = new Vector3(0f, 0.5f + 0.9f / f.scale.y, 0f);
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

            // resets auto-start; the ready gate is armed separately on first load
            waitingToStart = false;
            _controller.inputLocked = false;
        }

        /// <summary>Freeze at spawn until Space. Used only for the initial load.</summary>
        public void ArmReadyGate()
        {
            if (timerPaused) return;   // the hub needs no gate
            waitingToStart = true;
            _controller.inputLocked = true;
        }

        /// <summary>Hazards: send the body back to spawn without resetting the loop.</summary>
        public void ReturnToStart()
        {
            transform.position = _spawnPoint;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
        }
    }
}
