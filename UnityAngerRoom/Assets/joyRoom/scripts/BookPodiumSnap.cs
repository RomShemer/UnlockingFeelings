using UnityEngine;

public class BookPodiumSnap : MonoBehaviour
{
    public Transform podiumAnchor; // גרור את ה-Anchor שעל הפודיום

    private void OnTriggerEnter(Collider other)
    {
        // לבדוק אם זה הספר
        if (other.CompareTag("Book"))
        {
            // למצוא את BookVariantSwitcher
            var switcher = other.GetComponentInParent<BookVariantSwitcher>();
            if (switcher != null)
                switcher.Open();

            // יישור למיקום ולזווית של הפודיום
            other.transform.SetPositionAndRotation(podiumAnchor.position, podiumAnchor.rotation);

            // הפיכת Rigidbody ל-Kinematic כדי שלא ייפול
            var rb = other.attachedRigidbody;
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}