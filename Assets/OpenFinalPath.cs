using UnityEngine;
using UnityEngine.Tilemaps;

public class OpenFinalPath : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private Vector3Int[] tilePositions;
    [SerializeField] private int requiredLevelsCompleted = 4;

    private void Start()
    {
        CheckAndRemoveTiles();
    }

    private void CheckAndRemoveTiles()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("DisappearingTiles: no GameManager instance found.");
            return;
        }

        if (GameManager.Instance.LevelsCompleted >= requiredLevelsCompleted)
        {
            foreach (var pos in tilePositions)
                tilemap.SetTile(pos, null);
        }
    }
}
