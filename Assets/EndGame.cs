using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionTrigger : MonoBehaviour
{
    [Header("Scene to load")]
    [SerializeField] private string levelToLoad = "Outro";

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag("Player")) return;

        hasTriggered = true;

        var player = other.GetComponent<Player>();
        if (player != null)
            FreezePlayer(player);

        ParsaProto.PP_ScreenFade.LoadScene(levelToLoad);
    }

    private void FreezePlayer(Player player)
    {
        // Kill any residual velocity so the body doesn't keep coasting once
        // input/state updates stop.
        if (player.rb != null)
        {
            player.rb.linearVelocity = new Vector2 (0f, player.rb.linearVelocityX);
        }

        // Disable input reading and the state machine's Update/FixedUpdate.
        player.enabled = false;
    }

}