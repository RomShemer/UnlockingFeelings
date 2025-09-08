using UnityEngine;

public class BookPodiumSnap : MonoBehaviour
{
    [Tooltip("עוגן על הפודיום אליו הספר יינעל")]
    public Transform podiumAnchor;

    [Tooltip("בדוק תגיות? אם true – נדרוש שהשורש יהיה מתויג 'Book'")]
    public bool requireBookTag = false;

    void OnTriggerEnter(Collider other)
    {
        TrySnap(other);
    }

    void OnTriggerStay(Collider other) // גיבוי אם פספסנו פריים
    {
        TrySnap(other);
    }

    void TrySnap(Collider other)
    {
        // קח את השורש (אם הקוליידר שייך לילד של הספר)
        Transform root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform.root;

        if (requireBookTag && !root.CompareTag("Book")) return;

        var switcher = root.GetComponentInChildren<BookVariantSwitcher>(true);
        var reader   = root.GetComponentInChildren<EspImuReader>(true);
        var rb       = root.GetComponentInChildren<Rigidbody>(true);

        // פתח ונעל דרך הסוויצ'ר (מבטל גרב/קוליידרים לפי הדגלים)
        if (switcher != null)
        {
            // וודא שהדגלים מכוונים באינספקטור:
            // switcher.disableGrabWhenOpen = true;
            // switcher.disableCollidersWhenOpen = true; // אם תרצה
            switcher.OpenAndHardLock();
        }

        // עצירת פיזיקה “בכוח”
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        // נעל ESP לפודיום (מתעלם מהחיישן)
        if (reader != null && podiumAnchor != null)
            reader.SnapToPodium(podiumAnchor);

        // הצבה מידית על העוגן (בלי לחכות ל-FixedUpdate)
        if (podiumAnchor != null)
            root.SetPositionAndRotation(podiumAnchor.position, podiumAnchor.rotation);
    }
}