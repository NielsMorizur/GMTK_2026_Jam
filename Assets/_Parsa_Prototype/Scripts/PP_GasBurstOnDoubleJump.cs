using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Fires the gas burst when the player's double-jump animation starts.
    /// Watches the player's Animator from the OUTSIDE: no player scripts,
    /// prefab, or state machine are involved. Drop one of these in a scene.
    /// </summary>
    public class PP_GasBurstOnDoubleJump : MonoBehaviour
    {
        [Tooltip("The particle burst prefab to spawn.")]
        public GameObject burstPrefab;

        [Tooltip("Where the burst appears, relative to the player (mirrors with facing).")]
        public Vector2 offset = new Vector2(0.45f, -0.2f);

        Player _player;
        Animator _anim;
        bool _wasInDoubleJump;

        void Update()
        {
            if (_player == null)
            {
                _player = Object.FindFirstObjectByType<Player>();
                if (_player != null) _anim = _player.GetComponentInChildren<Animator>();
                if (_anim == null) return;
            }

            bool inDoubleJump = _anim.GetCurrentAnimatorStateInfo(0).IsName("DoubleJump");
            if (inDoubleJump && !_wasInDoubleJump && burstPrefab != null)
            {
                Vector3 pos = _player.transform.TransformPoint(offset);
                Instantiate(burstPrefab, pos, Quaternion.identity);
            }
            _wasInDoubleJump = inDoubleJump;
        }
    }
}
