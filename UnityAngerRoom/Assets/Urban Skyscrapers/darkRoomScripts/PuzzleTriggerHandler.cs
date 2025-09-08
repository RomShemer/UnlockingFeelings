using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PuzzleTriggerHandler : MonoBehaviour
{
    [HideInInspector] public PuzzlePieceHandler parentPiece;

    private void Awake()
    {
        parentPiece = GetComponent<PuzzlePieceHandler>();
    }


    public void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Trigger detected between {name} and {other.name}");

        //HandleTrigger(other);
    }

    public void OnTriggerExit(Collider other)
    {
        Debug.Log($"Trigger detected between-exit {name} and {other.name}");

        HandleTrigger(other);
    }

    //public void OnTriggerStay(Collider other)
    //{
    //    Debug.Log("trigger stay from: " + other.name);

    //    HandleTrigger(other);
    //}

    private void HandleTrigger(Collider other)
    {
        PuzzlePieceHandler otherPiece = other.GetComponent<PuzzlePieceHandler>();
        if (otherPiece == null || parentPiece.isConnected || otherPiece.isConnected)
            return;

        Debug.Log("trigger- trying to connect: " + other.name);

        PuzzleGameManager.Instance.TryConnect(parentPiece, otherPiece);
    }
}
