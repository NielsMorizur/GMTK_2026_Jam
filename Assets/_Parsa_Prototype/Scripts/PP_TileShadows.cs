using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace ParsaProto
{
    /// <summary>
    /// Gives every tilemap collider in the scene a 2D shadow caster at
    /// runtime, so candle/player lights get blocked by platforms. The caster
    /// takes its shape from the collider that's already there.
    /// </summary>
    public class PP_TileShadows : MonoBehaviour
    {
        void Start()
        {
            foreach (var col in FindObjectsByType<TilemapCollider2D>(FindObjectsInactive.Exclude))
            {
                if (col.GetComponent<ShadowCaster2D>() == null)
                    col.gameObject.AddComponent<ShadowCaster2D>();
            }
        }
    }
}
