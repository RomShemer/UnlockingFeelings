using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class JarButterflyShow : MonoBehaviour
{
    [Header("Scene Refs")]
    [Tooltip("המכסה (Transform) שיש לו ציר פתיחה מקומי")]
    public Transform jarCap;                     // המכסה
    [Tooltip("גוף הצנצנת (לא חובה, רק לעזר)")]
    public Transform jarBody;                    // גוף הצנצנת (לא חובה)
    [Tooltip("כל הפרפרים שיוצאים מהצנצנת")]
    public List<Transform> butterflies = new List<Transform>();  // הפרפרים
    [Tooltip("ראש/מצלמה של השחקן (XR Main Camera/CenterEye)")]
    public Transform playerHead;                 // המצלמה

    [Header("One Shot")]
    [Tooltip("אם מסומן – הרצף ירוץ פעם אחת בלבד")]
    public bool oneShot = true;
    private bool hasPlayed = false;              // האם הרצף כבר רץ
    private bool running = false;                // האם הרצף כרגע רץ

    [Header("Cap Opening")]
    [Tooltip("ציר סיבוב מקומי של המכסה (לרוב X/Y/Z לפי המודל)")]
    public Vector3 hingeLocalAxis = new Vector3(1, 0, 0);
    [Tooltip("זווית פתיחה במעלות")]
    public float capOpenAngle = 95f;
    [Tooltip("זמן פתיחה בשניות")]
    public float capOpenTime = 0.6f;
    [Tooltip("הרמה קלה למעלה בזמן פתיחה (במטרים)")]
    public float capLift = 0.03f;

    [Header("Butterfly Exit (Arc)")]
    [Tooltip("השהייה בין שחרור פרפר לפרפר (שניות)")]
    public float perButterflyDelay = 0.06f;
    [Tooltip("גובה הקשת (מטרים)")]
    public float arcHeight = 0.25f;
    [Tooltip("סטייה צדית אקראית למסלול (מטרים)")]
    public float arcSide = 0.25f;
    [Tooltip("זמן הקשת (שניות) עד נקודת ביניים לפני השורה")]
    public float exitTime = 0.8f;

    [Header("Line Formation")]
    [Tooltip("מרחק מול הראש (מטרים)")]
    public float distanceInFront = 1.6f;
    [Tooltip("מרווח אופקי בין פרפרים (מטרים)")]
    public float horizontalSpacing = 0.32f;
    [Tooltip("היסט אנכי ביחס לגובה העיניים (מטרים)")]
    public float verticalOffset = 0.0f;
    [Tooltip("זמן ההתיישרות לשורה (שניות)")]
    public float lineupTime = 0.7f;

    [Header("Events (Optional)")]
    [Tooltip("נקרא בסיום ההפעלה הראשונה (למשל כדי להסתיר את הכפתור/קנבס)")]
    public UnityEvent onReleasedOnce;
    void Reset()
    {
        if (playerHead == null && Camera.main != null)
            playerHead = Camera.main.transform;
    }

    /// <summary>
    /// זו הפונקציה שחייבים לחבר ל-UI Button (OnClick)
    /// </summary>
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
            yield return StartCoroutine(AnimateCapOpen());

        // 2) יציאת פרפרים + התייצבות לשורה מול הראש
        if (playerHead != null && butterflies != null && butterflies.Count > 0)
        {
            Vector3 fwdFlat = Vector3.ProjectOnPlane(playerHead.forward, Vector3.up).normalized;
            if (fwdFlat.sqrMagnitude < 0.0001f) fwdFlat = playerHead.forward.normalized; // גיבוי
            Vector3 right = Vector3.Cross(Vector3.up, fwdFlat).normalized;
            Vector3 center = playerHead.position + fwdFlat * distanceInFront + Vector3.up * verticalOffset;

            float totalWidth = (butterflies.Count - 1) * horizontalSpacing;

            for (int i = 0; i < butterflies.Count; i++)
            {
                Transform b = butterflies[i];
                if (b == null) continue;

                Vector3 preLine = center + right * (-totalWidth * 0.5f + i * horizontalSpacing);
                StartCoroutine(ButterflyArcThenLine(b, preLine, fwdFlat));
                yield return new WaitForSeconds(perButterflyDelay);
            }
        }

        // זמן ביטחון לסיום כל הקורוטינות
        yield return new WaitForSeconds(exitTime + lineupTime + 0.2f);

        // טריגר לאירועים חיצוניים (למשל הסתרת הכפתור/קנבס)
        if (oneShot) onReleasedOnce?.Invoke();
        
        running = false;
    }

    private IEnumerator AnimateCapOpen()
    {
        Quaternion startRot = jarCap.localRotation;
        Vector3 startPos = jarCap.localPosition;

        Vector3 axis = hingeLocalAxis.normalized;
        Quaternion targetRot = Quaternion.AngleAxis(capOpenAngle, axis) * startRot;
        Vector3 targetPos = startPos + Vector3.up * capLift;

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
    }

    private IEnumerator ButterflyArcThenLine(Transform b, Vector3 preLineTarget, Vector3 faceDir)
    {
        Vector3 p0 = b.position;
        Vector3 side = Vector3.Cross(Vector3.up, faceDir).normalized;
        Vector3 p1 = p0 + Vector3.up * arcHeight + side * Random.Range(-arcSide, arcSide);
        Vector3 p2 = preLineTarget + Vector3.up * (arcHeight * 0.4f);

        Quaternion startRot = b.rotation;
        Quaternion look = Quaternion.LookRotation(faceDir, Vector3.up);

        // שלב 1: קשת החוצה (Bezier קואדרטי)
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, exitTime);
            float s = Mathf.SmoothStep(0f, 1f, t);
            b.position = Bezier(p0, p1, p2, s);
            b.rotation = Quaternion.Slerp(startRot, look, s);
            yield return null;
        }

        // שלב 2: התייצבות לשורה
        Vector3 start = b.position;
        Vector3 final = preLineTarget;
        Quaternion r0 = b.rotation, r1 = look;
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, lineupTime);
            float s = Mathf.SmoothStep(0f, 1f, t);
            b.position = Vector3.Lerp(start, final, s);
            b.rotation = Quaternion.Slerp(r0, r1, s);
            yield return null;
        }
    }

    private Vector3 Bezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1f - t;
        return (u * u * p0) + (2f * u * t * p1) + (t * t * p2);
    }
}
