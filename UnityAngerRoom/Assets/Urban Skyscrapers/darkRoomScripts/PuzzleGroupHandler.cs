using UnityEngine;
using System.Collections.Generic;

public class PuzzleGroupHandler : MonoBehaviour
{
    private List<PuzzlePieceHandler> pieces = new List<PuzzlePieceHandler>();

    //public void AddPiece(PuzzlePieceHandler piece)
    //{
    //    if (pieces.Contains(piece)) return;

    //    pieces.Add(piece);
    //    piece.SetGroup(this);
    //}

    public void AddPiece(PuzzlePieceHandler piece)
    {
        pieces.Add(piece);
        piece.SetGroup(this);
        piece.transform.SetParent(this.transform);

        // בטלי התנגשות עם שאר החלקים בקבוצה
        foreach (var other in pieces)
        {
            if (other != piece)
            {
                Physics.IgnoreCollision(
                    piece.GetComponent<Collider>(),
                    other.GetComponent<Collider>(),
                    true);
            }
        }
    }


    public void AddGroupTriggerCollider()
    {
        var collider = gameObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;

        // אם את רוצה – התאימי גודל לפי Bounding Box של כל הילדים
        Bounds groupBounds = new Bounds(transform.position, Vector3.zero);
        foreach (Transform child in transform)
        {
            Renderer r = child.GetComponent<Renderer>();
            if (r != null)
                groupBounds.Encapsulate(r.bounds);
        }
        collider.center = transform.InverseTransformPoint(groupBounds.center);
        ((BoxCollider)collider).size = groupBounds.size;
    }

    public void AddKinematicRigidbody()
    {
        var rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    public List<PuzzlePieceHandler> GetPieces()
    {
        return pieces;
    }
}
