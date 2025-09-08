using UnityEngine;

public class ButterflyId : MonoBehaviour
{
    [Header("זיהוי")]
    [Tooltip("red / blue / green / yellow / pink / purple וכו'")]
    public string colorKey = "red";

    [Header("רכיבים (ימולאו אוטומטית)")]
    public Rigidbody rb;
    public Collider[] colliders;

    // רכיבי אינטראקציה אופציונליים (אם קיימים בפרויקט)
    [Tooltip("למשל ISDK_RayGrabInteraction / HandGrab (Meta)")]
    public Behaviour metaRayGrab;
    [Tooltip("Snap Interactor (Meta) אם קיים על הפרפר")]
    public Behaviour metaSnapInteractor;
    [Tooltip("XRGrabInteractable (XRI) אם קיים")]
    public Behaviour xriGrab;

    void Reset()  { AutoWire(); }
    void Awake()  { if (rb == null || colliders == null || colliders.Length == 0) AutoWire(); }

    void AutoWire()
    {
        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>(true);

        // חיפוש לפי שם טיפוס, כדי שלא נהיה תלויים באסמבליז
        metaRayGrab        = FindBehaviourByName("ISDK_RayGrab");
        if (metaRayGrab == null) metaRayGrab = FindBehaviourByName("HandGrab");
        metaSnapInteractor = FindBehaviourByName("SnapInteractor");
        xriGrab            = FindBehaviourByName("XRGrabInteractable");
    }

    Behaviour FindBehaviourByName(string token)
    {
        foreach (var b in GetComponentsInChildren<Behaviour>(true))
        {
            var n = b.GetType().FullName;
            if (!string.IsNullOrEmpty(n) &&
                n.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return b;
        }
        return null;
    }

    /// הצמדת הפרפר לנקודת הסנאפ ונעילה
    public void FreezeAt(Transform snapPoint)
    {
        if (snapPoint == null) snapPoint = transform;

        // כיבוי אינטראקציות אם קיימות
        if (metaSnapInteractor) metaSnapInteractor.enabled = false;
        if (metaRayGrab)        metaRayGrab.enabled        = false;
        if (xriGrab)            xriGrab.enabled            = false;

        // עצירת פיזיקה
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
            rb.linearVelocity        = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // נטרל קוליידרים כדי שלא יזוז ממגעים
        if (colliders != null)
            foreach (var c in colliders) if (c) c.enabled = false; // אפשר להחליף ל: c.isTrigger = true;

        // הצמדה סופית
        transform.SetPositionAndRotation(snapPoint.position, snapPoint.rotation);
    }
}
