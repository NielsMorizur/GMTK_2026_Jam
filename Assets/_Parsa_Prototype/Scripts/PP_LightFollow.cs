using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Keeps a light (or anything) glued to the player with an offset.
    /// </summary>
    public class PP_LightFollow : MonoBehaviour
    {
        public Vector2 offset = Vector2.zero;

        Player _player;
        float _refindTimer;

        void LateUpdate()
        {
            if (_player == null)
            {
                _refindTimer -= Time.unscaledDeltaTime;
                if (_refindTimer <= 0f)
                {
                    _player = FindAnyObjectByType<Player>();
                    _refindTimer = 0.5f;
                }
                if (_player == null) return;
            }
            transform.position = _player.transform.position + (Vector3)offset;
        }
    }
}
