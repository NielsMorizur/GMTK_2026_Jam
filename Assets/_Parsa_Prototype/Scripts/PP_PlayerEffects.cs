using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Player particle effects:
    ///  - PP_WalkDirt child: emits by distance while grounded; world sim space
    ///    leaves the dust behind in the world.
    ///  - Landing + gas bursts: spawned unparented at an offset from the player,
    ///    so they live purely in world space and never follow him around.
    /// </summary>
    public class PP_PlayerEffects : MonoBehaviour
    {
        [Header("World-space burst spawns")]
        public GameObject landingPrefab;
        public Vector2 landingOffset = new Vector2(0f, -0.95f);
        public GameObject gasPrefab;
        public Vector2 gasOffset = new Vector2(0.45f, -0.2f);

        Player _player;
        Animator _anim;
        ParticleSystem _walk;
        bool _wasGrounded = true;
        bool _wasDoubleJump;

        void Awake()
        {
            _player = GetComponent<Player>();
            _anim = GetComponentInChildren<Animator>();
            var t = transform.Find("PP_WalkDirt");
            _walk = t != null ? t.GetComponent<ParticleSystem>() : null;
        }

        void Update()
        {
            if (_player == null) return;

            bool grounded = _player.groundDetected;

            if (_walk != null)
            {
                var em = _walk.emission;
                em.enabled = grounded;      // no dust trails in mid-air
            }

            if (grounded && !_wasGrounded)
                SpawnBurst(landingPrefab, landingOffset);   // touchdown poof
            _wasGrounded = grounded;

            if (_anim != null)
            {
                bool dj = _anim.GetCurrentAnimatorStateInfo(0).IsName("DoubleJump");
                if (dj && !_wasDoubleJump)
                    SpawnBurst(gasPrefab, gasOffset);       // canister puff
                _wasDoubleJump = dj;
            }
        }

        void SpawnBurst(GameObject prefab, Vector2 offset)
        {
            if (prefab == null) return;
            // the player flips by rotating 180 on Y, so transform.right gives facing
            float dir = transform.right.x >= 0f ? 1f : -1f;
            Vector3 pos = transform.position + new Vector3(offset.x * dir, offset.y, 0f);
            Instantiate(prefab, pos, prefab.transform.rotation);
        }
    }
}
