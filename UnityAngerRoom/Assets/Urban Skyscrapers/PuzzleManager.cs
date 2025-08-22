using UnityEngine;
using UnityEngine.Events;

public class PuzzleManager : MonoBehaviour
{
    public int totalPieces;             // כמה חלקים יש
    private int collectedPieces = 0;

    public UnityEvent onAllPiecesCollected; // מה לעשות כשכולם נאספו

    public void RegisterPieceCollected()
    {
        collectedPieces++;
        Debug.Log($"Collected {collectedPieces}/{totalPieces}");

        if (collectedPieces >= totalPieces)
        {
            Debug.Log("All pieces collected!");
            onAllPiecesCollected?.Invoke();
        }
    }
}
