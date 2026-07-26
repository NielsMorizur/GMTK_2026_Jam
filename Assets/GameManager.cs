using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private string[] levelSequence = {"Level1", "Level2", "Level3", "Level4", "End"};
    [SerializeField] private int currentLevelIndex = 0;

    // currentLevelIndex already tracks this: it starts at 0 (no levels done)
    // and increments by 1 each time LevelComplete() runs, so its value IS
    // the count of levels completed so far.
    public int LevelsCompleted => currentLevelIndex;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // keeps same GameManager when switching scenes
    }

    public void LevelComplete()
    {
        currentLevelIndex++;
    }

    public string GetLevel()
    {
        return levelSequence[currentLevelIndex];
    }
}