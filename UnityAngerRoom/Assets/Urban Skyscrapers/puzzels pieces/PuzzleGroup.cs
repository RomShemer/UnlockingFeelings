using System.Collections.Generic;
using UnityEngine;

public class PuzzleGroup : MonoBehaviour
{
    [Header("Full Puzzle Prefab")]
    public GameObject fullPuzzlePrefab;

    [Header("Connections Manager")]
    public PuzzleConnections connectionsManager;

    private HashSet<int> groupPossibleConnections = new HashSet<int>();
    private GameObject fullPuzzleInstance;
    private Dictionary<int, GameObject> fullPuzzlePieces = new Dictionary<int, GameObject>();

    public List<MagneticPuzzlePiece> pieces = new List<MagneticPuzzlePiece>();
    public MagneticPuzzlePiece leader;

    private Vector3 lastLeaderPosition;
    private bool isBeingDragged = false;

    // ================================
    // יצירת קבוצה עם שני חלקים ראשונים
    // ================================

    public void Initialize(List<MagneticPuzzlePiece> initialPieces)
    {
        foreach (var piece in initialPieces)
        {
            AddPiece(piece);
        }
    }
    public void Initialize(MagneticPuzzlePiece piece1, MagneticPuzzlePiece piece2)
    {
        pieces.Clear();
        pieces.Add(piece1);
        pieces.Add(piece2);

        piece1.SetPuzzleGroup(this);
        piece2.SetPuzzleGroup(this);

        leader = piece1;
        lastLeaderPosition = leader.transform.position;

        SetupFullPuzzle();
        piece1.gameObject.SetActive(false);
        piece2.gameObject.SetActive(false);

        ShowPiece(piece1.pieceID);
        ShowPiece(piece2.pieceID);

        UpdateGroupConnections(piece1.pieceID);
        UpdateGroupConnections(piece2.pieceID);

        Debug.Log($"Group created with {piece1.pieceID} + {piece2.pieceID}");
    }

    private void SetupFullPuzzle()
    {
        if (fullPuzzleInstance != null) return;

        fullPuzzleInstance = Instantiate(fullPuzzlePrefab, transform.position, Quaternion.identity);
        fullPuzzleInstance.transform.SetParent(transform);

        fullPuzzlePieces.Clear();

        foreach (Transform child in fullPuzzleInstance.transform)
        {
            if (child.name.StartsWith("Piece_"))
            {
                string idStr = child.name.Replace("Piece_", "");
                if (int.TryParse(idStr, out int id))
                {
                    fullPuzzlePieces[id] = child.gameObject;
                    child.gameObject.SetActive(false);
                }
            }
        }

        fullPuzzleInstance.SetActive(true);
    }

    public void AddPiece(MagneticPuzzlePiece newPiece)
    {
        if (pieces.Contains(newPiece)) return;

        pieces.Add(newPiece);
        newPiece.SetPuzzleGroup(this);
        newPiece.gameObject.SetActive(false);

        ShowPiece(newPiece.pieceID);
        UpdateGroupConnections(newPiece.pieceID);

        Debug.Log($"Added {newPiece.pieceID} to group");
    }

    private void UpdateGroupConnections(int pieceID)
    {
        if (connectionsManager == null) return;

        List<int> connections = connectionsManager.GetConnections(pieceID);
        foreach (int targetID in connections)
            groupPossibleConnections.Add(targetID);
    }

    private void ShowPiece(int pieceID)
    {
        if (fullPuzzlePieces.TryGetValue(pieceID, out GameObject piece))
        {
            piece.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"Piece {pieceID} not found in full puzzle prefab!");
        }
    }

    public void RemovePiece(MagneticPuzzlePiece piece)
    {
        pieces.Remove(piece);
        piece.SetPuzzleGroup(null);

        if (pieces.Count <= 1)
        {
            foreach (var p in pieces)
                p.SetPuzzleGroup(null);

            Destroy(fullPuzzleInstance);
            Destroy(gameObject);
        }
        else if (leader == piece)
        {
            leader = pieces[0];
        }
    }

    public void StartDragging(MagneticPuzzlePiece draggedPiece)
    {
        leader = draggedPiece;
        lastLeaderPosition = leader.transform.position;
        isBeingDragged = true;

        foreach (var piece in pieces)
            piece.SetDraggingState(piece == leader);

        Debug.Log($"Group leader: {leader.pieceID}");
    }

    public void StopDragging()
    {
        isBeingDragged = false;
        foreach (var piece in pieces)
            piece.SetDraggingState(false);
    }

    public void MoveGroup()
    {
        if (!isBeingDragged || leader == null) return;

        Vector3 movement = leader.transform.position - lastLeaderPosition;

        foreach (var piece in pieces)
        {
            if (piece != leader)
                piece.transform.position += movement;
        }

        lastLeaderPosition = leader.transform.position;
    }

    // ================================
    // חיבור בין קבוצות או הוספת חלק
    // ================================
    public void CheckForNearbyPieces()
    {
        if (!isBeingDragged) return;

        MagneticPuzzlePiece[] allPieces = FindObjectsOfType<MagneticPuzzlePiece>();

        foreach (var outsidePiece in allPieces)
        {
            if (pieces.Contains(outsidePiece)) continue;

            float distance = Vector3.Distance(leader.transform.position, outsidePiece.transform.position);

            if (distance > leader.snapDistance) continue;
            if (!groupPossibleConnections.Contains(outsidePiece.pieceID)) continue;

            // אם החלק שייך לקבוצה אחרת → מאחדים קבוצות
            if (outsidePiece.puzzleGroup != null && outsidePiece.puzzleGroup != this)
            {
                MergeWith(outsidePiece.puzzleGroup);
                return;
            }

            // אחרת, מוסיפים חלק בודד לקבוצה
            Vector3 direction = (outsidePiece.transform.position - leader.transform.position).normalized;
            outsidePiece.GetComponent<Rigidbody>().AddForce(direction * leader.snapForce * 0.5f, ForceMode.Force);

            if (distance <= 0.8f)
            {
                AddPiece(outsidePiece);
                outsidePiece.isSnapped = true;

                if (outsidePiece.snapSound && outsidePiece.GetComponent<AudioSource>())
                    outsidePiece.GetComponent<AudioSource>().PlayOneShot(outsidePiece.snapSound);

                return;
            }
        }
    }

    // ================================
    // איחוד קבוצות
    // ================================
    public void MergeWith(PuzzleGroup otherGroup)
    {
        Debug.Log($"Merging groups: {this.name} + {otherGroup.name}");

        foreach (var piece in otherGroup.pieces)
        {
            AddPiece(piece);
        }

        Destroy(otherGroup.fullPuzzleInstance);
        Destroy(otherGroup.gameObject);
    }
}
