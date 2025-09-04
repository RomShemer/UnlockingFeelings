using UnityEngine;
using System.Collections.Generic;

public class PuzzleGroupHandler : MonoBehaviour
{
    private List<PuzzlePieceHandler> pieces = new List<PuzzlePieceHandler>();

    public void AddPiece(PuzzlePieceHandler piece)
    {
        if (pieces.Contains(piece)) return;

        pieces.Add(piece);
        piece.SetGroup(this);
    }

    public List<PuzzlePieceHandler> GetPieces()
    {
        return pieces;
    }
}
