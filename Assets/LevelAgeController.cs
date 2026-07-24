using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelAgeController : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private float secondsPerStage = 15f;

    [SerializeField] private AgeStage[] ageSequence;

    private int currentIndex = 0;
    private float timer;
    private bool timerActive = true;

    public float TimeRemainingInStage => secondsPerStage - timer;

    private void Start()
    {
        if (ageSequence == null || ageSequence.Length == 0)
        {
            Debug.LogWarning("LevelAgeController: no age sequence assigned for this level.");
            timerActive = false;
            return;
        }

        currentIndex = 0;
        timer = 0f;
        player.SetAge(ageSequence[currentIndex]);
    }

    private void Update()
    {
        if (!timerActive) return;

        timer += Time.deltaTime;

        if (timer >= secondsPerStage)
        {
            timer = 0f;
            currentIndex++;

            if (currentIndex >= ageSequence.Length)
            {
                ResetLevel();
                return;
            }

            player.SetAge(ageSequence[currentIndex]);
        }
    }

    private void ResetLevel()
    {
        // soft reset when the player carries the reset component; falls back
        // to the old scene reload otherwise
        var reset = player != null ? player.GetComponent<ParsaProto.PP_LevelReset>() : null;
        if (reset != null)
        {
            var sfx = player.GetComponent<ParsaProto.PP_SfxPlayer>();
            if (sfx != null) sfx.PlayDie();

            reset.DieAndReset();
            return;
        }

        timerActive = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Call this from your level-complete trigger/goal so the timer stops
    // counting down once the player has actually finished the level.
    public void OnLevelCompleted()
    {
        timerActive = false;
    }
}