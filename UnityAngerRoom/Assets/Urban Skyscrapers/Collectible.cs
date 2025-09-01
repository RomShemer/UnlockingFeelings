using UnityEngine;

public class Collectible : MonoBehaviour
{
    public PuzzleManager puzzleManager;

    public void Collect()
    {
        if (puzzleManager != null)
            puzzleManager.RegisterPieceCollected();

        gameObject.SetActive(false); // להעלים את החלק מהמשחק
    }
}
