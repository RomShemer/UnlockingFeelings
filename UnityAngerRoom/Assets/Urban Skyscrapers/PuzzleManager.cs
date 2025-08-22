using UnityEngine;
using UnityEngine.Events;

public class PuzzleManager : MonoBehaviour
{
    public int totalPieces;             // ��� ����� ��
    private int collectedPieces = 0;

    public UnityEvent onAllPiecesCollected; // �� ����� ������ �����

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
