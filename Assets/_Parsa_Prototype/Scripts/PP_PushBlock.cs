using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Heavy block only the adult's super strength can push. Everyone else
    /// treats it as an immovable wall. It slides horizontally when pushed
    /// from the side, falls under gravity, and stops quickly when released.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PP_PushBlock : MonoBehaviour
    {
        [Tooltip("Form index allowed to push. 1 = adult.")]
        public int pushableByForm = 1;

        [Tooltip("Damping while sliding, so it stops quickly when released.")]
        public float slideDamping = 3f;

        [Tooltip("Slide speed while the adult actively pushes, units/sec.")]
        public float pushSpeed = 2.5f;

        Rigidbody2D _rb;
        Vector3 _startPos;
        bool _beingPushed;
        float _pushDir;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _startPos = transform.position;
            _rb.linearDamping = slideDamping;
            Lock();
        }

        void FixedUpdate()
        {
            // OnCollisionStay2D re-proves the push every physics step;
            // no adult actively pushing this step means we lock again.
            if (_beingPushed)
            {
                _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                _rb.linearVelocity = new Vector2(pushSpeed * _pushDir, _rb.linearVelocity.y);
            }
            else
            {
                Lock();
            }
            _beingPushed = false;
        }

        void OnCollisionStay2D(Collision2D c)
        {
            var loop = c.collider.GetComponent<PP_AgeLoop>();
            if (loop == null || loop.FormIndex != pushableByForm) return;

            var ctrl = c.collider.GetComponent<PP_PlayerController>();
            if (ctrl == null) return;

            // must be actively walking INTO the block, not just touching it
            float toBlock = Mathf.Sign(transform.position.x - c.transform.position.x);
            if (ctrl.MoveInput * toBlock <= 0.01f) return;

            foreach (var contact in c.contacts)
            {
                // side contact = a push; standing on top doesn't count
                if (Mathf.Abs(contact.normal.x) > 0.5f)
                {
                    _pushDir = toBlock;
                    _beingPushed = true;
                    return;
                }
            }
        }

        void Lock()
        {
            _rb.constraints = RigidbodyConstraints2D.FreezePositionX
                              | RigidbodyConstraints2D.FreezeRotation;
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
        }

        /// <summary>Level restart: back to the spawn spot, locked.</summary>
        public void ResetToStart()
        {
            _rb.linearVelocity = Vector2.zero;
            transform.position = _startPos;
            Lock();
        }
    }
}
