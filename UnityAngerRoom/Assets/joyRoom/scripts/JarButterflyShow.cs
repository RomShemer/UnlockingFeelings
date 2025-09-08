using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class JarButterflyShow : MonoBehaviour
{
    [Header("Scene Refs")]
    [Tooltip("המכסה (Transform) שיש לו ציר פתיחה מקומי")]
    public Transform jarCap;
    [Tooltip("גוף הצנצנת / עוגן ברירת מחדל לשורה")]
    public Transform jarBody;
    [Tooltip("שורשי הפרפרים שישתחררו")]
    public List<Transform> butterflies = new List<Transform>();

    [Header("One Shot")]
    [Tooltip("האם לאפשר רק פעם אחת")]
    public bool oneShot = true;
    private bool hasPlayed = false;
    private bool running = false;

    [Header("Cap Opening")]
    [Tooltip("ציר סיבוב מקומי של המכסה")]
    public Vector3 hingeLocalAxis = new Vector3(1, 0, 0);
    [Tooltip("זווית פתיחה במעלות")]
    public float capOpenAngle = 95f;
    [Tooltip("זמן פתיחה (שניות)")]
    public float capOpenTime = 0.6f;
    [Tooltip("הרמה קלה למעלה בזמן פתיחה (מטרים)")]
    public float capLift = 0.03f;

    [Header("Butterfly Exit (Arc)")]
    [Tooltip("השהייה בין פרפר לפרפר (שניות)")]
    public float perButterflyDelay = 0.06f;
    [Tooltip("גובה הקשת (מטרים)")]
    public float arcHeight = 0.25f;
    [Tooltip("סטייה צדית אקראית במסלול (מטרים)")]
    public float arcSide = 0.25f;
    [Tooltip("זמן טיסה בקשת (שניות)")]
    public float exitTime = 0.8f;

    [Header("Fixed Lineup (relative to anchor)")]
    [Tooltip("אם ריק -> jarBody ואם גם הוא ריק -> this.transform")]
    public Transform lineupAnchor;
    [Tooltip("מרכז השורה במרחב המקומי של העוגן: X=ימין, Y=למעלה, Z=קדימה")]
    public Vector3 lineupLocalOffset = new Vector3(0f, 0.6f, 0.0f);
    [Tooltip("מרווח אופקי בין פרפרים (מטרים)")]
    public float horizontalSpacing = 0.32f;
    [Tooltip("זמן ההתיישרות לנקודת הקבע (שניות)")]
    public float lineupTime = 0.7f;

    [Header("Orientation (all butterflies)")]
    [Tooltip("אם דולק – כולם יפנו למעלה (Forward=WorldUp)")]
    public bool forceFaceUp = true;
    [Tooltip("Yaw קבוע (סיבוב סביב Y) לכל הפרפרים, אם רוצים שיפנו גם ימינה/שמאלה באופן אחיד")]
    public float fixedYawDeg = 0f;
    [Tooltip("תיקון גלובלי אם ה-FBX לא מיושר. לרוב X=±90 או Z=±90")]
    public Vector3 modelRotationOffsetEuler = Vector3.zero;

    [Header("Events (Optional)")]
    public UnityEvent onReleasedOnce;

    // ---------- חדש: שליטה באינטראקציה ----------
    [Header("Interaction Lock")]
    [Tooltip("נעל תפיסה עד סיום ההתיישרות בשורה")]
    public bool lockGrabUntilLineup = true;
    [Tooltip("לכבות את המכסה אחרי הפתיחה")]
    public bool deactivateCapAfterOpen = true;

    void Reset()
    {
        if (lineupAnchor == null)
            lineupAnchor = jarBody != null ? jarBody : transform;
    }

    void Start()
    {
        // בתחילת הסצנה – נטרל תפיסה לכל הפרפרים (כשהם עדיין בתוך הצנצנת)
        if (lockGrabUntilLineup && butterflies != null)
        {
            foreach (var b in butterflies)
                SetButterflyGrabActive(b, false);
        }
    }

    /// לחבר ל-UI Button (OnClick)
    public void OnUIButtonClick()
    {
        if (running) return;
        if (oneShot && hasPlayed) return;
        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        running = true;
        if (oneShot) hasPlayed = true;

        // 1) פתיחת מכסה
        if (jarCap != null)
            yield return StartCoroutine(AnimateCapOpen()); // יהפוך ל-inactive בסוף אם מסומן

        // 2) יציאה + התייצבות מעל הצנצנת
        if (butterflies != null && butterflies.Count > 0)
        {
            Transform anchor = lineupAnchor != null ? lineupAnchor :
                               (jarBody != null ? jarBody : transform);

            Matrix4x4 m = Matrix4x4.TRS(anchor.position, anchor.rotation, Vector3.one);
            Vector3 center = m.MultiplyPoint3x4(lineupLocalOffset);
            Vector3 right  = anchor.right;
            float totalWidth = (butterflies.Count - 1) * horizontalSpacing;

            // רוטציית יעד אחידה לכולם
            Quaternion targetLook = BuildFaceUpRotation();

            for (int i = 0; i < butterflies.Count; i++)
            {
                Transform b = butterflies[i];
                if (b == null) continue;

                Vector3 preLine = center + right * (-totalWidth * 0.5f + i * horizontalSpacing);
                StartCoroutine(ButterflyArcThenLine(b, preLine, targetLook));
                yield return new WaitForSeconds(perButterflyDelay);
            }
        }

        // חכה שכל מי שהתחיל – יסיים (מרווח ביטחון קטן)
        yield return new WaitForSeconds(exitTime + lineupTime + 0.2f);

        if (oneShot) onReleasedOnce?.Invoke();
        running = false;
    }

    private Quaternion BuildFaceUpRotation()
    {
        if (forceFaceUp)
        {
            Quaternion lookUp = Quaternion.LookRotation(Vector3.up, Vector3.forward);
            return Quaternion.Euler(0f, fixedYawDeg, 0f) * lookUp * Quaternion.Euler(modelRotationOffsetEuler);
        }
        else
        {
            return Quaternion.Euler(0f, fixedYawDeg, 0f) * Quaternion.Euler(modelRotationOffsetEuler);
        }
    }

    private IEnumerator AnimateCapOpen()
    {
        Quaternion startRot = jarCap.localRotation;
        Vector3 startPos    = jarCap.localPosition;

        Vector3 axis = hingeLocalAxis.sqrMagnitude > 0f ? hingeLocalAxis.normalized : Vector3.right;
        Quaternion targetRot = Quaternion.AngleAxis(capOpenAngle, axis) * startRot;
        Vector3    targetPos = startPos + Vector3.up * capLift;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, capOpenTime);
            float s = Mathf.SmoothStep(0f, 1f, t);
            jarCap.localRotation = Quaternion.Slerp(startRot, targetRot, s);
            jarCap.localPosition = Vector3.Lerp(startPos, targetPos, s);
            yield return null;
        }

        jarCap.localRotation = targetRot;
        jarCap.localPosition = targetPos;

        // NEW: לכבות את המכסה לאחר שהסתיים האנימציה
        if (deactivateCapAfterOpen && jarCap.gameObject.activeSelf)
            jarCap.gameObject.SetActive(false);
    }

    private IEnumerator ButterflyArcThenLine(Transform b, Vector3 preLineTarget, Quaternion targetLook)
    {
        Vector3 p0 = b.position;

        Vector3 forwardFlat = Vector3.ProjectOnPlane(targetLook * Vector3.forward, Vector3.up).normalized;
        if (forwardFlat.sqrMagnitude < 1e-6f) forwardFlat = Vector3.forward;
        Vector3 side = Vector3.Cross(Vector3.up, forwardFlat).normalized;

        Vector3 p1 = p0 + Vector3.up * arcHeight + side * Random.Range(-arcSide, arcSide);
        Vector3 p2 = preLineTarget + Vector3.up * (arcHeight * 0.4f);

        Quaternion startRot = b.rotation;

        // שלב 1: קשת (Bezier)
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, exitTime);
            float s = Mathf.SmoothStep(0f, 1f, t);
            b.position = Bezier(p0, p1, p2, s);
            b.rotation = Quaternion.Slerp(startRot, targetLook, s);
            yield return null;
        }

        // שלב 2: התייצבות לנקודת הקבע
        Vector3 start = b.position;
        Vector3 final = preLineTarget;
        Quaternion r0 = b.rotation, r1 = targetLook;
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, lineupTime);
            float s = Mathf.SmoothStep(0f, 1f, t);
            b.position = Vector3.Lerp(start, final, s);
            b.rotation = Quaternion.Slerp(r0, r1, s);
            yield return null;
        }

        b.rotation = r1; // ודא נעילה לרוטציית היעד

        // NEW: בשלב הזה מותר לתפוס
        if (lockGrabUntilLineup)
            SetButterflyGrabActive(b, true);
    }

    private Vector3 Bezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1f - t;
        return (u * u * p0) + (2f * u * t * p1) + (t * t * p2);
    }

    // --------- NEW: הפעלה/כיבוי של רכיבי תפיסה רלוונטיים ---------
    private void SetButterflyGrabActive(Transform root, bool active)
    {
        if (root == null) return;

        // כבה/הפעל כל קומפוננטה רלוונטית לתפיסה (Interaction SDK / Meta XR / Oculus / XR)
        foreach (var beh in root.GetComponentsInChildren<Behaviour>(true))
        {
            if (beh == null) continue;
            string n = beh.GetType().Name;

            // דוגמאות נפוצות: Grabbable, RayInteractable, GrabInteractable, RayGrabInteractable, OVRGrabbable, XRGrabInteractable, וכו'
            if (n.Contains("Grabbable") ||
                n.Contains("GrabInteractable") ||
                n.Contains("RayInteractable") ||
                n.Contains("RayGrab"))
            {
                beh.enabled = active;
            }
        }
    }
}
