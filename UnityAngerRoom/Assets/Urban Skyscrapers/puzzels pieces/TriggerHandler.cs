using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TriggerHandler : MonoBehaviour
{
    [HideInInspector]
    public MagneticPuzzlePiece parentPiece;

    private void OnTriggerEnter(Collider other)
    {
        HandleTrigger(other);
    }

    private void OnTriggerStay(Collider other)
    {
        HandleTrigger(other);
    }

    private void HandleTrigger(Collider other)
    {
        MagneticPuzzlePiece otherPiece = other.GetComponent<MagneticPuzzlePiece>();
        if (otherPiece == null || otherPiece == parentPiece)
            return;

        // �� ��� ������� � �� ����� ����
        if (parentPiece.IsConnectedTo(otherPiece))
            return;

        // �� PuzzleManager �� ����
        if (PuzzleManagerDarkRoom.Instance == null || PuzzleManagerDarkRoom.Instance.connectionsManager == null)
            return;

        // ���� �� ������ ������ ������ ��� �������
        var connectionsManager = PuzzleManagerDarkRoom.Instance.connectionsManager;
        if (!connectionsManager.CanConnect(parentPiece.pieceID, otherPiece.pieceID))
            return;

        // ���� �� ������ ��� MagneticPuzzlePiece
        parentPiece.ConnectTo(otherPiece);

        // ����� ��������� �� ����
        PuzzleVisualManager.Instance?.ShowConnectedPiece(parentPiece.pieceID);
        PuzzleVisualManager.Instance?.ShowConnectedPiece(otherPiece.pieceID);
    }
}
