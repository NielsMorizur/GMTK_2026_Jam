using UnityEngine;
using UnityEngine.InputSystem;

namespace ParsaProto
{
    /// <summary>
    /// The old man's walker: a movable block only the old man can pick up (Q)
    /// and put down. While carried it floats above his head with no collision.
    /// Placed, it's a solid block any form can jump on. It is world state:
    /// the age loop never resets it.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PP_Walker : MonoBehaviour
    {
        [Tooltip("The player's age loop (identifies the current form).")]
        public PP_AgeLoop player;

        [Tooltip("Max distance for the old man to grab it.")]
        public float pickupRange = 1.5f;

        [Tooltip("Gap between the player's head and the carried walker.")]
        public float carryHeight = 0.4f;

        Rigidbody2D _rb;
        Collider2D _col;
        PP_PlayerController _controller;
        bool _carried;
        Vector3 _startPos;

        public bool IsCarried => _carried;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<Collider2D>();
            _startPos = transform.position;
        }

        void Start()
        {
            if (player != null)
                _controller = player.GetComponent<PP_PlayerController>();
        }

        void Update()
        {
            if (player == null) return;

            // only the old man holds it; growing up drops it on the spot
            if (_carried && player.FormIndex != 0)
            {
                Drop();
                return;
            }

            var kb = Keyboard.current;
            if (kb == null || !kb.qKey.wasPressedThisFrame) return;

            if (_carried)
            {
                Drop();
            }
            else if (player.FormIndex == 0 &&
                     Vector2.Distance(player.transform.position, transform.position) <= pickupRange)
            {
                Pickup();
            }
        }

        void LateUpdate()
        {
            if (!_carried || player == null) return;
            float head = player.transform.localScale.y * 0.5f;
            transform.position = player.transform.position
                                 + new Vector3(0f, head + carryHeight + transform.localScale.y * 0.5f, 0f);
        }

        /// <summary>Level restart: back to the spawn spot, dropped and solid.</summary>
        public void ResetToStart()
        {
            _carried = false;
            _col.enabled = true;
            _rb.simulated = true;
            _rb.linearVelocity = Vector2.zero;
            transform.position = _startPos;
        }

        void Pickup()
        {
            _carried = true;
            _col.enabled = false;
            _rb.simulated = false;
        }

        void Drop()
        {
            _carried = false;
            int facing = _controller != null ? _controller.Facing : 1;
            transform.position = player.transform.position + new Vector3(facing * 1.1f, 0.3f, 0f);
            _col.enabled = true;
            _rb.simulated = true;
            _rb.linearVelocity = Vector2.zero;   // falls straight down onto the ground
        }
    }
}
