using UnityEngine;
using UnityEngine.SceneManagement;

public class EnterDoor4 : MonoBehaviour
{
    [SerializeField] private Player player;

    public string levelToLoad = "ExitRoom";
    private bool playerInRange = false;

    GameManager gm;

    void Awake()
    {
        gm = GameManager.Instance;

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
        if (playerInRange && player.input.Player.Interact.WasPerformedThisFrame() && gm.GetLevel() == levelToLoad)
        {
            ParsaProto.PP_ScreenFade.LoadScene(levelToLoad);
        }
    }
}