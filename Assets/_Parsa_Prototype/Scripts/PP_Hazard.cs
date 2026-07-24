using UnityEngine;
using UnityEngine.SceneManagement;

namespace ParsaProto
{
    /// <summary>
    /// Hazard layer: anything painted on this tilemap kills on touch.
    /// Works with both players: the team player (Player) dies by reloading
    /// the level, same as LevelAgeController's own reset; the old prototype
    /// player (PP_AgeLoop) uses its level-restart.
    /// </summary>
    public class PP_Hazard : MonoBehaviour
    {
        void OnTriggerEnter2D(Collider2D other)
        {
            var teamPlayer = other.GetComponentInParent<Player>();
            if (teamPlayer != null)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }

            var loop = other.GetComponent<PP_AgeLoop>();
            if (loop != null) loop.RestartLevel();
        }
    }
}
