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

        // אם כבר מחוברים — לא עושים כלום
        if (parentPiece.IsConnectedTo(otherPiece))
            return;

        // אם PuzzleManager לא קיים
        if (PuzzleManagerDarkRoom.Instance == null || PuzzleManagerDarkRoom.Instance.connectionsManager == null)
            return;

        // בודק אם החלקים יכולים להתחבר לפי ההגדרות
        var connectionsManager = PuzzleManagerDarkRoom.Instance.connectionsManager;
        if (!connectionsManager.CanConnect(parentPiece.pieceID, otherPiece.pieceID))
            return;

        // מבצע את החיבור דרך MagneticPuzzlePiece
        parentPiece.ConnectTo(otherPiece);

        // עדכון הוויזואלי של החלק
        PuzzleVisualManager.Instance?.ShowConnectedPiece(parentPiece.pieceID);
        PuzzleVisualManager.Instance?.ShowConnectedPiece(otherPiece.pieceID);
    }
}
