using UnityEngine;

public class BookPodiumSnap : MonoBehaviour
{
    [Tooltip("עוגן על הפודיום אליו הספר יינעל")]
    public Transform podiumAnchor;

    [Tooltip("לדרוש שהשורש יהיה מתויג 'Book' כדי להתעלם מידיים/שחקן")]
    public bool requireBookTag = true;

    void OnTriggerEnter(Collider other) => TrySnap(other);
    void OnTriggerStay(Collider other)  => TrySnap(other);

    void TrySnap(Collider other)
    {
        // קח תמיד את השורש של האובייקט שנכנס לטריגר (אם יש Rigidbody – זה יהיה השורש שלו)
        Transform root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform.root;

        // סינון 1: תגית Book (מומלץ)
        if (requireBookTag && !root.CompareTag("Book")) return;

        // סינון 2: חייב להיות עליו ספר אמיתי (BookVariantSwitcher)
        var switcher = root.GetComponentInChildren<BookVariantSwitcher>(true);
        if (switcher == null) return; // לא ספר -> להתעלם (כך היד/ה-XR Origin לא יסתנפו)

        // עכשיו בטוח שזה הספר.
        var reader = root.GetComponentInChildren<EspImuReader>(true);
        var rb     = root.GetComponentInChildren<Rigidbody>(true);

        // פתח ונעל דרך הסוויצ'ר
        switcher.OpenAndHardLock();

        // עצירת פיזיקה מוחלטת
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints     = RigidbodyConstraints.FreezeAll;
        }

        // נעל את ה-ESP למצב PodiumLocked (מנתק מהחיישן)
        if (reader != null && podiumAnchor != null)
            reader.SnapToPodium(podiumAnchor);

        // הצבה מידית על העוגן
        if (podiumAnchor != null)
            root.SetPositionAndRotation(podiumAnchor.position, podiumAnchor.rotation);

        // Debug (לא חובה)
        // Debug.Log($"[PodiumSnap] Snapped book '{root.name}' to '{podiumAnchor.name}'");
    }
}
