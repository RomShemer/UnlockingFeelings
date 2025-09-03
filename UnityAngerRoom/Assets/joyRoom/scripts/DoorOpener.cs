// DoorOpener.cs
using System.Collections;
using UnityEngine;

public class DoorOpener : MonoBehaviour
{
    [Header("Door")]
    public Transform door;                 // ציר/דלת
    public Vector3 localAxis = Vector3.up;
    public float openAngle = 90f;
    public float duration = 0.75f;
    public AudioSource sfx;

    [Header("Fade UI (optional)")]
    [Tooltip("CanvasGroup של הקנבס 'unlock your next feeling'")]
    public CanvasGroup fadeCanvas;         // גרור לפה את ה-CanvasGroup של הקנבס
    public float fadeInDuration = 0.6f;    // זמן הפייד-אין
    public bool fadeAfterFullyOpen = false;// אם true – להתחיל פייד רק אחרי שהדלת סיימה להיפתח

    bool opened;

    void Awake()
    {
        // ודא שה-UI כבוי בתחילת הסצנה
        if (fadeCanvas != null)
        {
            fadeCanvas.alpha = 0f;
            fadeCanvas.interactable = false;
            fadeCanvas.blocksRaycasts = false;
            fadeCanvas.gameObject.SetActive(false);
        }
    }

    public void Open()
    {
        if (opened || door == null) return;
        opened = true;
        StartCoroutine(OpenRoutine());

        // פייד בזמן פתיחה (אם לא מחכים לסיום)
        if (fadeCanvas != null && !fadeAfterFullyOpen)
            StartFadeIn();
    }

    IEnumerator OpenRoutine()
    {
        var start = door.localRotation;
        var target = Quaternion.AngleAxis(openAngle, localAxis.normalized) * start;

        sfx?.Play();
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, duration);
            door.localRotation = Quaternion.Slerp(start, target, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }
        door.localRotation = target;

        // פייד רק אחרי שהדלת נפתחה לגמרי (אם מסומן)
        if (fadeCanvas != null && fadeAfterFullyOpen)
            StartFadeIn();
    }

    Coroutine fadeCo;   

    void StartFadeIn()
    {
        if (!fadeCanvas.gameObject.activeSelf)
            fadeCanvas.gameObject.SetActive(true);

        // עצור רק פייד קודם (אם רץ), לא את שאר הקורוטינות
        if (fadeCo != null) StopCoroutine(fadeCo);
        fadeCo = StartCoroutine(FadeInRoutine());
    }

    IEnumerator FadeInRoutine()
    {
        float t = 0f;
        float d = Mathf.Max(0.01f, fadeInDuration);
        // ודא שמתחילים מ-0
        fadeCanvas.alpha = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / d;
            fadeCanvas.alpha = Mathf.SmoothStep(0f, 1f, t);
            yield return null;
        }
        fadeCanvas.alpha = 1f;
        fadeCanvas.interactable = true;
        fadeCanvas.blocksRaycasts = true;
        fadeCo = null; // סיום
    }
}
