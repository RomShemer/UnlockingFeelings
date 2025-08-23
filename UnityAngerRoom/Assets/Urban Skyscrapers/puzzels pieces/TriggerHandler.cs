using UnityEngine;

public class TriggerHandler : MonoBehaviour
{
    [HideInInspector]
    public MagneticPuzzlePiece parentPiece;

    private void OnTriggerEnter(Collider other)
    {
        MagneticPuzzlePiece otherPiece = other.GetComponent<MagneticPuzzlePiece>();
        if (otherPiece != null && otherPiece != parentPiece)
        {
            parentPiece.OnTriggerDetected(otherPiece);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        MagneticPuzzlePiece otherPiece = other.GetComponent<MagneticPuzzlePiece>();
        if (otherPiece != null && otherPiece != parentPiece)
        {
            parentPiece.OnTriggerDetected(otherPiece);
        }
    }
}