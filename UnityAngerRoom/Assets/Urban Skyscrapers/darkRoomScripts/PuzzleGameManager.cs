using UnityEngine;
using System.Collections.Generic;
using Meta.WitAi;

public class PuzzleGameManager : MonoBehaviour
{
    public static PuzzleGameManager Instance { get; private set; }

    [Header("Connections Config")]
    public PuzzleConnectionsConfig connectionsConfig;

    [Header("UI")]
    public ProgressBarUI progressBarUI;

    [SerializeField] private DoorScript.Door door;

    private readonly List<PuzzleGroupHandler> allGroups = new List<PuzzleGroupHandler>();
    private int totalPieces = 16; 
    private int connectedPieces = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        progressBarUI.Init(16); // 16 חלקים
    }

    // =============================
    // חיבור בין שני חלקי פאזל
    // =============================

    //עובד אבל החלקים נדחפים בהתנגשות
    //public void TryConnect(PuzzlePieceHandler pieceA, PuzzlePieceHandler pieceB)
    //{
    //    Debug.Log("puzzleGameManager: enter to TryConnect " + pieceA.PieceID + " " + pieceB.PieceID);

    //    if (pieceA == null || pieceB == null)
    //    {
    //        Debug.Log("Step 3: One of the pieces is NULL");
    //        return;
    //    }

    //    // לא מחברים את אותו חלק
    //    if (pieceA == pieceB)
    //    {
    //        Debug.Log("Step 4: same piece");

    //        return;
    //    }

    //    if (!connectionsConfig.CanConnect(pieceA.PieceID, pieceB.PieceID))
    //    {
    //        Debug.Log("Step 4: CanConnect returned FALSE");
    //        return;
    //    }


    //    // אם החלקים כבר באותה קבוצה ? אין מה לעשות
    //    if (pieceA.CurrentGroup != null && pieceA.CurrentGroup == pieceB.CurrentGroup)
    //    {
    //        Debug.Log("Step 6: allready in the same group");
    //        return;

    //    }

    //    // שני חלקים בלי קבוצה ? יוצרים קבוצה חדשה
    //    if (pieceA.CurrentGroup == null && pieceB.CurrentGroup == null)
    //    {
    //        Debug.Log("Step 5: Trying to actually connect...");
    //        Debug.Log("puzzleGameManager: create new group from " + pieceA.PieceID + " " + pieceB.PieceID);

    //        CreateNewGroup(pieceA, pieceB);

    //        Debug.Log("puzzleGameManager: return from create new group " + pieceA.PieceID + " " + pieceB.PieceID);

    //        return;
    //    }

    //    // אם ל-A יש קבוצה ול-B אין ? מוסיפים את B
    //    if (pieceA.CurrentGroup != null && pieceB.CurrentGroup == null)
    //    {
    //        Debug.Log("puzzleGameManager: addPiece to group-" + pieceA.PieceID + " " + pieceB.PieceID);

    //        pieceA.CurrentGroup.AddPiece(pieceB);
    //        progressBarUI?.ReportOne();

    //        Debug.Log("puzzleGameManager: return from addPiece to group-" + pieceA.PieceID + " " + pieceB.PieceID);

    //        return;
    //    }

    //    // אם ל-B יש קבוצה ול-A אין ? מוסיפים את A
    //    if (pieceA.CurrentGroup == null && pieceB.CurrentGroup != null)
    //    {
    //        Debug.Log("puzzleGameManager: addPiece to group-" + pieceA.PieceID + " " + pieceB.PieceID);

    //        pieceB.CurrentGroup.AddPiece(pieceA);
    //        progressBarUI?.ReportOne();

    //        Debug.Log("puzzleGameManager: return from addPiece to group-" + pieceA.PieceID + " " + pieceB.PieceID);

    //        return;
    //    }

    //    Debug.Log("puzzleGameManager: merge groups-" + pieceA.PieceID + " " + pieceB.PieceID);

    //    // אם לשניהם יש קבוצות שונות ? ממזגים אותן
    //    MergeGroups(pieceA.CurrentGroup, pieceB.CurrentGroup);

    //    Debug.Log("puzzleGameManager: return from merge groups-" + pieceA.PieceID + " " + pieceB.PieceID);

    //}

    //ניסיון ללא דחיפה
    public void TryConnect(PuzzlePieceHandler pieceA, PuzzlePieceHandler pieceB)
    {
        Debug.Log("puzzleGameManager: enter to TryConnect " + pieceA.PieceID + " " + pieceB.PieceID);

        if (pieceA == null || pieceB == null)
        {
            Debug.Log("Step 3: One of the pieces is NULL");
            return;
        }

        if (pieceA == pieceB)
        {
            Debug.Log("Step 4: same piece");
            return;
        }

        if (!connectionsConfig.CanConnect(pieceA.PieceID, pieceB.PieceID))
        {
            Debug.Log("Step 4: CanConnect returned FALSE");
            return;
        }

        if (pieceA.CurrentGroup != null && pieceA.CurrentGroup == pieceB.CurrentGroup)
        {
            Debug.Log("Step 6: already in the same group");
            return;
        }

        // ✅ תוספת - ייצוב לפני כל פעולה
        PreparePiecesForConnection(pieceA, pieceB);

        if (pieceA.CurrentGroup == null && pieceB.CurrentGroup == null)
        {
            Debug.Log("Step 5: Trying to actually connect...");
            Debug.Log("puzzleGameManager: create new group from " + pieceA.PieceID + " " + pieceB.PieceID);

            CreateNewGroup(pieceA, pieceB);
            Debug.Log("puzzleGameManager: return from create new group " + pieceA.PieceID + " " + pieceB.PieceID);

            Debug.Log("report one to progressBar");
            ReportConnection();

            //temp-check:
            if ((pieceA.PieceID == 15 && pieceB.PieceID == 16) || (pieceA.PieceID == 16 && pieceB.PieceID == 15))
            {
                Debug.Log("temp check- connect pieces 15-16 and starting opening door");
                door?.OpenDoor();  // קריאה לפתיחה
                Debug.Log("temp check-after open door");
            }
            ////////////////////////////////////////////////////////////////////////////////////////////

            Debug.Log("return from report one to progressBar");

            return;
        }

        if (pieceA.CurrentGroup != null && pieceB.CurrentGroup == null)
        {
            Debug.Log("puzzleGameManager: addPiece to group-" + pieceA.PieceID + " " + pieceB.PieceID);
            pieceA.CurrentGroup.AddPiece(pieceB);
            ReportConnection();
            Debug.Log("puzzleGameManager: return from addPiece to group-" + pieceA.PieceID + " " + pieceB.PieceID);
            return;
        }

        if (pieceA.CurrentGroup == null && pieceB.CurrentGroup != null)
        {
            Debug.Log("puzzleGameManager: addPiece to group-" + pieceA.PieceID + " " + pieceB.PieceID);
            pieceB.CurrentGroup.AddPiece(pieceA);
            ReportConnection();
            Debug.Log("puzzleGameManager: return from addPiece to group-" + pieceA.PieceID + " " + pieceB.PieceID);
            return;
        }

        Debug.Log("puzzleGameManager: merge groups-" + pieceA.PieceID + " " + pieceB.PieceID);
        MergeGroups(pieceA.CurrentGroup, pieceB.CurrentGroup);
        Debug.Log("puzzleGameManager: return from merge groups-" + pieceA.PieceID + " " + pieceB.PieceID);
    }

    private void PreparePiecesForConnection(PuzzlePieceHandler pieceA, PuzzlePieceHandler pieceB)
    {
        Rigidbody rbA = pieceA.GetComponent<Rigidbody>();
        Rigidbody rbB = pieceB.GetComponent<Rigidbody>();

        rbA.isKinematic = true;
        rbB.isKinematic = true;

        rbA.linearVelocity = Vector3.zero;
        rbB.linearVelocity = Vector3.zero;

        rbA.angularVelocity = Vector3.zero;
        rbB.angularVelocity = Vector3.zero;

        pieceA.isConnected = true;
        pieceB.isConnected = true;

        // אופציונלי: התעלמות מקוליידר לאחר חיבור
        Collider colA = pieceA.GetComponent<Collider>();
        Collider colB = pieceB.GetComponent<Collider>();
        if (colA != null && colB != null)
        {
            Physics.IgnoreCollision(colA, colB, true);
        }
    }

    // =============================
    // יצירת קבוצה חדשה
    // =============================
    private void CreateNewGroup(PuzzlePieceHandler a, PuzzlePieceHandler b)
    {
        Debug.Log("puzzleGameManager: enter create new group" + a.PieceID + " " + b.PieceID);

        GameObject groupGO = new GameObject("PuzzleGroup");
        PuzzleGroupHandler newGroup = groupGO.AddComponent<PuzzleGroupHandler>();
        allGroups.Add(newGroup);

        newGroup.AddPiece(a);
        newGroup.AddPiece(b);
        //ReportConnection();
    }

    // =============================
    // מיזוג קבוצות קיימות
    // =============================
    private void MergeGroups(PuzzleGroupHandler groupA, PuzzleGroupHandler groupB)
    {
        Debug.Log("puzzleGameManager: enter merge groups");

        if (groupA == groupB) return;

        foreach (var piece in groupB.GetPieces())
            groupA.AddPiece(piece);

        allGroups.Remove(groupB);
        Destroy(groupB.gameObject);
    }

    private void ReportConnection()
    {
        connectedPieces++;
        progressBarUI?.ReportOne();

        if (connectedPieces >= totalPieces)
        {
            Debug.Log("all puzzles connected");
            OnPuzzleComplete();
        }
    }

    private void OnPuzzleComplete()
    {
        Debug.Log("finish puzzel- open door");
        door?.OpenDoor();  // קריאה לפתיחה
    }
}
