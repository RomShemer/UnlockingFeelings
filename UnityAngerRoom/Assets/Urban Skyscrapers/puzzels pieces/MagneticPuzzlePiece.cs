using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MagneticPuzzlePiece : MonoBehaviour
{
    [Header("Puzzle Settings")]
    public int pieceID;
    public List<int> connectablePieces;

    [Header("Magnetic Settings")]
    public float snapDistance = 2f;
    public float snapForce = 10f;
    public AudioClip snapSound;

    [Header("Physics Settings")]
    public Collider triggerCollider;
    public Collider physicsCollider;

    private Rigidbody rb;
    private AudioSource audioSource;
    private bool isSnapped = false;
    private bool isDragging = false;

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
        // בזמן גרירה — שומרים גובה קבוע
        if (isDragging && !isSnapped)
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
            if (piece == this || piece.isSnapped) continue;

            // מרחק במישור XZ בלבד
            Vector3 a = transform.position; a.y = 0f;
            Vector3 b = piece.transform.position; b.y = 0f;

            float distance = Vector3.Distance(a, b);

            if (distance <= snapDistance && CanConnectTo(piece))
            {
                Vector3 direction = (b - a).normalized;
                rb.AddForce(direction * snapForce * 0.1f, ForceMode.Force);
                Debug.DrawLine(transform.position, piece.transform.position, Color.yellow, 0.1f);
            }
        }
    }

    public void OnTriggerDetected(MagneticPuzzlePiece otherPiece)
    {
        if (isDragging && !isSnapped && CanConnectTo(otherPiece))
        {
            float distance = Vector3.Distance(transform.position, otherPiece.transform.position);
            if (distance <= snapDistance)
            {
                SnapToPiece(otherPiece);
            }
        }
    }

    bool CanConnectTo(MagneticPuzzlePiece otherPiece)
    {
        return connectablePieces.Contains(otherPiece.pieceID);
    }

    void SnapToPiece(MagneticPuzzlePiece targetPiece)
    {
        Vector3 snapPosition = CalculateSnapPosition(targetPiece);
        transform.position = snapPosition;

        // ברגע שהחלק מתחבר → כיבוי פיזיקה מלא
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.None;

        isSnapped = true;
        isDragging = false;

        if (!connectedPieces.Contains(targetPiece))
            connectedPieces.Add(targetPiece);

        if (!targetPiece.connectedPieces.Contains(this))
            targetPiece.connectedPieces.Add(this);

        if (snapSound && audioSource)
            audioSource.PlayOneShot(snapSound);

        StartCoroutine(SnapEffect());

        Debug.Log($"חלק {pieceID} התחבר לחלק {targetPiece.pieceID}!");
    }

    Vector3 CalculateSnapPosition(MagneticPuzzlePiece targetPiece)
    {
        Vector3 direction = (transform.position - targetPiece.transform.position).normalized;
        Vector3 snapPos = targetPiece.transform.position + direction * GetComponent<Collider>().bounds.size.x;
        return snapPos;
    }

    IEnumerator SnapEffect()
    {
        Renderer renderer = GetComponent<Renderer>();
        Color originalColor = renderer.material.color;

        renderer.material.color = Color.green;
        yield return new WaitForSeconds(0.2f);
        renderer.material.color = originalColor;
    }

    // XR Grab: תפיסת האובייקט
    public void OnSelectEntered()
    {
        if (!isSnapped)
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
        isDragging = false;

        if (!isSnapped)
        {
            // אם האובייקט לא מחובר → חוזרים לפיזיקה רגילה
            rb.isKinematic = false;
            rb.useGravity = true;

            // אין נעילת גובה → יפול לרצפה חופשי
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
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
        isSnapped = false;
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        foreach (var piece in connectedPieces)
            piece.connectedPieces.Remove(this);

        connectedPieces.Clear();
    }
}
