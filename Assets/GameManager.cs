using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private string[] levelSequence = {"Level1", "Level2", "Level3", "Level4", "End"};
    [SerializeField] private int currentLevelIndex = 0;

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


    void Update()
    {
        
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
