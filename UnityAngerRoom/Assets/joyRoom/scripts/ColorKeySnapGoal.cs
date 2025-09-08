using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class ColorKeySnapGoal : MonoBehaviour
{
    [Tooltip("הצבע שהשיח מקבל (לדוגמה: red / blue / green...)")]
    public string colorKey = "red";

    [Tooltip("אופציונלי: נקודת סנאפ מדויקת; אם ריק נשתמש ב-transform של האובייקט")]
    public Transform snapPose;

    [Tooltip("שכבות שמותר להן להפעיל את היעד (בחר את שכבת הפרפרים אם יש)")]
    public LayerMask butterflyMask = ~0;

    bool filled;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[{name}] OnTriggerEnter עם {other.name}");
        TryAccept(other);
    }

    void OnTriggerStay(Collider other)
    {
        if (!filled)
        {
            Debug.Log($"[{name}] OnTriggerStay עם {other.name}");
            TryAccept(other);
        }
    }

    void TryAccept(Collider other)
    {
        if (filled)
        {
            Debug.Log($"[{name}] כבר מולא, מתעלם");
            return;
        }

        // בדיקת מסכה
        if (((1 << other.gameObject.layer) & butterflyMask.value) == 0)
        {
            Debug.Log($"[{name}] {other.name} לא בשכבת butterflyMask");
            return;
        }

        var butterfly = other.GetComponentInParent<ButterflyId>();
        if (butterfly == null)
        {
            Debug.Log($"[{name}] {other.name} לא מכיל ButterflyId");
            return;
        }

        var want = (colorKey ?? "").Trim().ToLowerInvariant();
        var got  = (butterfly.colorKey ?? "").Trim().ToLowerInvariant();
        if (want != got)
        {
            Debug.Log($"[{name}] צבע לא תואם: want={want}, got={got}");
            return;
        }

        // סנאפ סופי + נעילה
        var t = snapPose ? snapPose : transform;
        butterfly.FreezeAt(t);
        filled = true;

        Debug.Log($"[{name}] ✅ התאמה הצליחה! מדווח למנהל MatchProgress");

        MatchProgressManager.Instance?.ReportCorrect();
    }
}
