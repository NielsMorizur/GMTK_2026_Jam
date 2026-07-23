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
        }

        [Tooltip("Aging order: index 0 is what you respawn as.")]
        public PP_Form[] forms;

        [Tooltip("Seconds per form. GDD says ~30; set low (e.g. 8) for testing.")]
        public float formDuration = 30f;

        [Tooltip("Optional world-space label showing form + countdown.")]
        public TextMesh label;

        [Tooltip("Countdown speed multiplier while the fast-forward key (E) is held.")]
        public float fastForwardMultiplier = 2f;

        PP_PlayerController _controller;
        SpriteRenderer _sprite;
        Rigidbody2D _rb;
        Vector3 _spawnPoint;

        int _formIndex;
        float _timer;

        public int FormIndex => _formIndex;
        public PP_Form CurrentForm =>
            (forms != null && forms.Length > 0) ? forms[_formIndex] : null;

        void Awake()
        {
            _controller = GetComponent<PP_PlayerController>();
            _sprite = GetComponent<SpriteRenderer>();
            _rb = GetComponent<Rigidbody2D>();
            _spawnPoint = transform.position;
        }

        void Start()
        {
            ApplyForm(0);
        }

        void Update()
        {
            if (forms == null || forms.Length == 0) return;

            var kb = Keyboard.current;

            if (kb != null && kb.rKey.wasPressedThisFrame)
            {
                RestartLevel();
                return;
            }

            bool fast = kb != null && kb.eKey.isPressed;
            _timer -= Time.deltaTime * (fast ? fastForwardMultiplier : 1f);
            if (_timer <= 0f)
            {
                if (_formIndex + 1 < forms.Length)
                    ApplyForm(_formIndex + 1);   // age down
                else
                    Respawn();                   // baby's time ran out: die, loop
            }

            if (label != null && CurrentForm != null)
                label.text = $"{CurrentForm.formName}  {Mathf.CeilToInt(Mathf.Max(_timer, 0f))}{(fast ? " >>" : "")}";
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

            // keep the label readable: cancel the parent's scale and sit above the head
            if (label != null)
            {
                label.transform.localScale = new Vector3(1f / f.scale.x, 1f / f.scale.y, 1f);
                label.transform.localPosition = new Vector3(0f, 0.5f + 0.9f / f.scale.y, 0f);
            }
        }

        void Respawn()
        {
            ReturnToStart();
            ApplyForm(0);
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
        }

        /// <summary>Hazards: send the body back to spawn without resetting the loop.</summary>
        public void ReturnToStart()
        {
            transform.position = _spawnPoint;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
        }
    }
}
