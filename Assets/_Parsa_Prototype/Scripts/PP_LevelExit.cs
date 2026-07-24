using UnityEngine;
using UnityEngine.SceneManagement;

namespace ParsaProto
{
    /// <summary>
    /// Level exit: touch it to complete the level. Tells the GameManager to
    /// advance the sequence, stops the level timer, and returns to the hub.
    /// Safe to use when playtesting a level directly (no GameManager alive):
    /// it just goes back to the hub without advancing anything.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PP_LevelExit : MonoBehaviour
    {
        [Tooltip("Scene to return to after completing the level.")]
        [SerializeField] private string returnScene = "MainRoom";

        bool _used;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_used) return;
            if (other.GetComponentInParent<Player>() == null) return;
            _used = true;

            if (GameManager.Instance != null)
                GameManager.Instance.LevelComplete();

            var timer = Object.FindAnyObjectByType<LevelAgeController>();
            if (timer != null)
                timer.OnLevelCompleted();

            var sfx = other.GetComponentInParent<PP_SfxPlayer>();
            if (sfx != null) sfx.PlayVictory();

            SceneManager.LoadScene(returnScene);
        }
    }
}
