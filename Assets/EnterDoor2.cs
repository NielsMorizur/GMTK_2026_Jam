using UnityEngine;
using UnityEngine.SceneManagement;

public class EnterDoor2 : MonoBehaviour
{
    [SerializeField] private Player player;

    public string levelToLoad = "Level2";
    private bool playerInRange = false;

    GameManager gm;

    void Awake()
    {
        gm = GameManager.Instance;   // may still be null here; Update retries

        if (player == null)
            player = FindAnyObjectByType<Player>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInRange = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInRange = false;
    }

    void Update()
    {
        if (!playerInRange) return;

        // resolved late, not cached in Awake: in a build the GameManager can
        // wake after the doors do, and a cached null (or a stale one from the
        // last run) makes E do nothing at all
        if (player == null) player = FindAnyObjectByType<Player>();
        if (gm == null || gm.gameObject == null) gm = GameManager.Instance;
        if (player == null || player.input == null) return;

        if (!player.input.Player.Interact.WasPerformedThisFrame()) return;
        if (gm != null && gm.GetLevel() != levelToLoad) return;

        ParsaProto.PP_ScreenFade.LoadScene(levelToLoad);
    }
}