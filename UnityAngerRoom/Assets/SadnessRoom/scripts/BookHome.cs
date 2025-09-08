using UnityEngine;

public class BookHome : MonoBehaviour
{
    [Header("Home")]
    [Tooltip("נקודת הבית (BookPlace) של הספר הסגור")]
    public Transform bookPlace;

    [Header("Lock At Home")]
    [Tooltip("רכיבי אחיזה/אינטראקציה לכבות כשננעל בבית (XRGrabInteractable / OVRGrabbable וכו')")]
    public Behaviour[] grabComponentsToDisable;
    [Tooltip("אופציונלי: שכבה בלתי-אחיזה בזמן נעילה (מומלץ ליצור NoGrab)")]
    public string noGrabLayerName = "NoGrab";

    Rigidbody _rb;
    bool _savedKinematic;
    RigidbodyConstraints _savedConstraints;
    int _savedLayer;
    bool _savedLayerValid;
    public bool IsLockedAtHome { get; private set; }

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb)
        {
            _savedKinematic   = _rb.isKinematic;
            _savedConstraints = _rb.constraints;
        }
        _savedLayer = gameObject.layer;
        _savedLayerValid = true;
    }

    /// <summary>נעל את הספר בבית: טלפורט לבית, הקפאה מלאה, כיבוי אחיזה, שכבת NoGrab.</summary>
    public void LockToHomeNow()
    {
        if (bookPlace)
            transform.SetPositionAndRotation(bookPlace.position, bookPlace.rotation);

        if (_rb)
        {
#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = Vector3.zero;
#else
            _rb.velocity = Vector3.zero;
#endif
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
            _rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        if (grabComponentsToDisable != null)
            foreach (var b in grabComponentsToDisable) if (b) b.enabled = false;

        if (!string.IsNullOrEmpty(noGrabLayerName))
        {
            int l = LayerMask.NameToLayer(noGrabLayerName);
            if (l != -1)
            {
                _savedLayer = gameObject.layer; _savedLayerValid = true;
                gameObject.layer = l;
            }
        }

        Physics.SyncTransforms();
        IsLockedAtHome = true;
    }

    /// <summary>שחרור הנעילה מהבית (למשל בעת בחירה מהכפתור).</summary>
    public void UnlockFromHome()
    {
        if (_savedLayerValid) gameObject.layer = _savedLayer;

        if (grabComponentsToDisable != null)
            foreach (var b in grabComponentsToDisable) if (b) b.enabled = true;

        if (_rb)
        {
            _rb.constraints = _savedConstraints;
            _rb.isKinematic = _savedKinematic;
        }

        IsLockedAtHome = false;
    }
}
