using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class MagneticPuzzlePiece : MonoBehaviour
{
    [Header("Puzzle Settings")]
    public int pieceID;
    public List<int> connectablePieces;

    [Header("Magnetic Settings")]
    public float snapDistance = 4f;
    public float snapForce = 10f;
    public AudioClip snapSound;

    [Header("Physics Settings")]
    public Collider triggerCollider;
    public Collider physicsCollider;

    private Rigidbody rb;
    private AudioSource audioSource;
    public bool isSnapped = false;
    private bool isDragging = false;

    public PuzzleGroup puzzleGroup;
    private List<MagneticPuzzlePiece> connectedPieces = new List<MagneticPuzzlePiece>();

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

        // הגדרות פיזיקה התחלתיות
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // יצירת Colliders אם צריך
        if (triggerCollider == null || physicsCollider == null)
            SetupColliders();
    }

    void SetupColliders()
    {
        Collider[] colliders = GetComponents<Collider>();

        if (colliders.Length == 1)
        {
            physicsCollider = colliders[0];
            physicsCollider.isTrigger = false;

            GameObject triggerObj = new GameObject("TriggerZone");
            triggerObj.transform.SetParent(transform);
            triggerObj.transform.localPosition = Vector3.zero;
            triggerObj.transform.localRotation = Quaternion.identity;
            triggerObj.layer = gameObject.layer;

            SphereCollider sphereCollider = triggerObj.AddComponent<SphereCollider>();
            sphereCollider.radius = 0.4f;
            sphereCollider.isTrigger = true;
            triggerCollider = sphereCollider;

            TriggerHandler handler = triggerObj.AddComponent<TriggerHandler>();
            handler.parentPiece = this;
        }
        else if (colliders.Length >= 2)
        {
            physicsCollider = colliders[0];
            triggerCollider = colliders[1];

            physicsCollider.isTrigger = false;
            triggerCollider.isTrigger = true;
        }
    }

    void Update()
    {
        if (puzzleGroup != null && puzzleGroup.leader == this && isDragging)
        {
            puzzleGroup.MoveGroup();
            puzzleGroup.CheckForNearbyPieces();
        }
        // בזמן גרירה — שומרים גובה קבוע
        else if (isDragging && !isSnapped)
        {
            CheckForNearbyPieces();
            Vector3 pos = transform.position;
            pos.y = Mathf.Clamp(pos.y, 0.5f, 2f);
            transform.position = pos;
        }
    }

    void CheckForNearbyPieces()
    {
        MagneticPuzzlePiece[] allPieces = FindObjectsOfType<MagneticPuzzlePiece>();

        foreach (MagneticPuzzlePiece piece in allPieces)
        {
            if (piece == this || piece.isSnapped || piece.puzzleGroup != null) continue;


            Vector3 thisPos = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 piecePos = new Vector3(piece.transform.position.x, 0, piece.transform.position.z);

            float distance = Vector3.Distance(thisPos, piecePos);

            if (distance <= snapDistance && CanConnectTo(piece))
            {
                Vector3 direction = (piecePos - thisPos).normalized;
                rb.AddForce(direction * snapForce, ForceMode.Force);

                Debug.DrawLine(transform.position, piece.transform.position, Color.yellow, 0.1f);

                if (distance <= 0.8f)
                {
                    SnapToPiece(piece);
                    return;
                }
            }

            //// מרחק במישור XZ בלבד
            //Vector3 a = transform.position; a.y = 0f;
            //Vector3 b = piece.transform.position; b.y = 0f;

            //float distance = Vector3.Distance(a, b);

            //if (distance <= snapDistance && CanConnectTo(piece))
            //{
            //    Vector3 direction = (b - a).normalized;
            //    rb.AddForce(direction * snapForce * 0.1f, ForceMode.Force);
            //    Debug.DrawLine(transform.position, piece.transform.position, Color.yellow, 0.1f);
            //}
        }
    }

    public void OnTriggerDetected(MagneticPuzzlePiece otherPiece)
    {
        if (isDragging && !isSnapped && CanConnectTo(otherPiece))
        {
            float distance = Vector3.Distance(transform.position, otherPiece.transform.position);
            Debug.Log($"trying to connect = {distance}, snapDistance = {snapDistance}");

            if (distance <= snapDistance)
            {
                SnapToPiece(otherPiece);
            }
        }
    }

    public bool CanConnectTo(MagneticPuzzlePiece otherPiece)
    {
        return connectablePieces.Contains(otherPiece.pieceID);
    }

    //void SnapToPiece(MagneticPuzzlePiece targetPiece)
    //{
    //    Debug.Log($"connect from {pieceID} -> to {targetPiece.pieceID}");

    //    Vector3 snapPosition = CalculateSnapPosition(targetPiece);
    //    transform.position = snapPosition;

    //    CreateOrJoinGroup(targetPiece);

    //    // ברגע שהחלק מתחבר → כיבוי פיזיקה מלא
    //    rb.linearVelocity = Vector3.zero;
    //    rb.angularVelocity = Vector3.zero;
    //    rb.isKinematic = true;
    //    rb.useGravity = false;
    //    rb.constraints = RigidbodyConstraints.None;

    //    isSnapped = true;
    //    isDragging = false;

    //    if (!connectedPieces.Contains(targetPiece))
    //        connectedPieces.Add(targetPiece);

    //    if (!targetPiece.connectedPieces.Contains(this))
    //        targetPiece.connectedPieces.Add(this);

    //    if (snapSound && audioSource)
    //        audioSource.PlayOneShot(snapSound);

    //    StartCoroutine(SnapEffect());

    //    Debug.Log($"חלק {pieceID} התחבר לחלק {targetPiece.pieceID}!");
    //}

    void SnapToPiece(MagneticPuzzlePiece targetPiece)
    {
        Debug.Log($"connect from {pieceID} -> to {targetPiece.pieceID}");

        // אם שניהם כבר בקבוצה — אין מה לעשות
        if (puzzleGroup != null && targetPiece.puzzleGroup == puzzleGroup)
            return;

        // אם לי יש קבוצה → מוסיפים את החלק השני לקבוצה שלי
        if (puzzleGroup != null)
        {
            puzzleGroup.AddPiece(targetPiece);
            return;
        }

        // אם לחלק השני יש קבוצה → מצטרפים אליה
        if (targetPiece.puzzleGroup != null)
        {
            targetPiece.puzzleGroup.AddPiece(this);
            return;
        }

        // אחרת → יוצרים קבוצה חדשה עם שנינו
        GameObject groupObj = new GameObject($"PuzzleGroup_{pieceID}_{targetPiece.pieceID}");
        PuzzleGroup newGroup = groupObj.AddComponent<PuzzleGroup>();
        newGroup.fullPuzzlePrefab = PuzzleManagerDarkRoom.Instance.fullPuzzlePrefab; // הפניה ל־fullPuzzle
        newGroup.connectionsManager = PuzzleManagerDarkRoom.Instance.connectionsManager; // הפניה ל־PuzzleConnections
        newGroup.Initialize(this, targetPiece);
    }



    void CreateOrJoinGroup(MagneticPuzzlePiece targetPiece)
    {
        if (targetPiece.puzzleGroup != null)
        {
            // מצטרף לקבוצה קיימת
            targetPiece.puzzleGroup.AddPiece(this);
        }
        else if (puzzleGroup != null)
        {
            // מוסיף את החלק השני לקבוצה שלי
            puzzleGroup.AddPiece(targetPiece);
        }
        else
        {
            // יוצר קבוצה חדשה
            GameObject groupObj = new GameObject($"PuzzleGroup_{pieceID}_{targetPiece.pieceID}");
            puzzleGroup = groupObj.AddComponent<PuzzleGroup>();
            puzzleGroup.Initialize(this, targetPiece);
        }
    }

    Vector3 CalculateSnapPosition(MagneticPuzzlePiece targetPiece)
    {
        Vector3 direction = (transform.position - targetPiece.transform.position).normalized;
        //Vector3 snapPos = targetPiece.transform.position + direction * GetComponent<Collider>().bounds.size.x;
        Vector3 snapPos = targetPiece.transform.position + direction * 0.1f;
        return snapPos;
    }

    IEnumerator SnapEffect()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Color originalColor = renderer.material.color;
            renderer.material.color = Color.green;
            yield return new WaitForSeconds(0.2f);
            renderer.material.color = originalColor;
        }
        //Renderer renderer = GetComponent<Renderer>();
        //Color originalColor = renderer.material.color;

        //renderer.material.color = Color.green;
        //yield return new WaitForSeconds(0.2f);
        //renderer.material.color = originalColor;
    }

    // XR Grab: תפיסת האובייקט
    public void OnSelectEntered()
    {
        if (puzzleGroup != null)
        {
            // תופס את כל הקבוצה
            puzzleGroup.StartDragging(this);
        }
        else if (!isSnapped)
        {
            isDragging = true;
            rb.isKinematic = false;
            rb.useGravity = true;

            // בזמן גרירה — נועלים גובה, משחררים סיבוב
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        }
    }

    // XR Grab: שחרור האובייקט
    public void OnSelectExited()
    {
        Debug.Log($"release piece {pieceID}");

        if (puzzleGroup != null)
        {
            puzzleGroup.StopDragging();
        }
        else
        {
            isDragging = false;
            if (!isSnapped)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
            }
        }

        //if (!isSnapped)
        //{
        //    // אם האובייקט לא מחובר → חוזרים לפיזיקה רגילה
        //    rb.isKinematic = false;
        //    rb.useGravity = true;

        //    // אין נעילת גובה → יפול לרצפה חופשי
        //    rb.constraints = RigidbodyConstraints.FreezeRotation;
        //}
    }

    public void StartDragging()
    {
        isDragging = true;
        rb.isKinematic = false;
    }

    public void StopDragging()
    {
        isDragging = false;
        if (!isSnapped)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }

    [ContextMenu("Disconnect")]
    public void Disconnect()
    {
        if (puzzleGroup != null)
        {
            puzzleGroup.RemovePiece(this);
        }

        isSnapped = false;
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        foreach (var piece in connectedPieces)
            piece.connectedPieces.Remove(this);

        connectedPieces.Clear();
        puzzleGroup = null;
    }

    public void SetPuzzleGroup(PuzzleGroup group)
    {
        puzzleGroup = group;
    }

    public void SetDraggingState(bool dragging)
    {
        isDragging = dragging;
        if (dragging)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        }
        else
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    public bool IsConnectedTo(MagneticPuzzlePiece otherPiece)
    {
        return connectedPieces.Contains(otherPiece);
    }

    public void ConnectTo(MagneticPuzzlePiece other)
    {
        // אם כבר מחוברים — לא עושים כלום
        if (IsConnectedTo(other))
            return;

        // אם לשניהם אין קבוצה → יוצרים קבוצה חדשה
        if (puzzleGroup == null && other.puzzleGroup == null)
        {
            PuzzleManagerDarkRoom.Instance.CreateGroup(this, other);
        }
        // אם רק לנו יש קבוצה → מוסיפים את השני לקבוצה שלנו
        else if (puzzleGroup != null && other.puzzleGroup == null)
        {
            puzzleGroup.AddPiece(other);
        }
        // אם רק לשני יש קבוצה → מצטרפים לקבוצה שלו
        else if (puzzleGroup == null && other.puzzleGroup != null)
        {
            other.puzzleGroup.AddPiece(this);
        }
        // אם שנינו בקבוצות שונות → מאחדים קבוצות
        else
        {
            PuzzleManagerDarkRoom.Instance.MergeGroups(puzzleGroup, other.puzzleGroup);
        }
    }

}
