using UnityEngine;

public class PlayerAgeTimer : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private float secondsPerStage = 10f;

    private static readonly AgeStage[] ageOrder =
    {
        AgeStage.Elderly, AgeStage.Adult, AgeStage.Teen, AgeStage.Baby
    };

    private int currentIndex = 0;
    private float timer;

    private void Start()
    {
        currentIndex = System.Array.IndexOf(ageOrder, player.CurrentAge);
        if (currentIndex < 0) currentIndex = 0;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= secondsPerStage)
        {
            timer = 0f;
            currentIndex = (currentIndex + 1) % ageOrder.Length; // wraps back to 0 automatically
            player.SetAge(ageOrder[currentIndex]);
        }
    }
}