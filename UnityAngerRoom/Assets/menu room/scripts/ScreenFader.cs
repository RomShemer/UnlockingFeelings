using UnityEngine;
using System.Collections;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [SerializeField] CanvasGroup fadeGroup;
    [SerializeField] float defaultDuration = 0.6f;

    public float DefaultDuration => defaultDuration;   // אופציונלי: לקריאה מבחוץ

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadeGroup == null) fadeGroup = GetComponentInChildren<CanvasGroup>(true);

        // ודא פעיל
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (!fadeGroup.gameObject.activeSelf) fadeGroup.gameObject.SetActive(true);

        // נתחיל שקוף
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;
    }

    // אם אתה רוצה פייד-אין אוטומטי בכל סצנה חדשה, השאר:
    void Start()
    {
        StartCoroutine(FadeRoutine(1f, 0f, defaultDuration));
    }

    // --- API שמחזיר IEnumerator (חשוב!) ---
    public IEnumerator FadeOutRoutine(float duration = -1f)
    {
        float d = duration < 0 ? defaultDuration : duration;
        yield return FadeRoutine(fadeGroup.alpha, 1f, d);
    }

    public IEnumerator FadeInRoutine(float duration = -1f)
    {
        float d = duration < 0 ? defaultDuration : duration;
        yield return FadeRoutine(fadeGroup.alpha, 0f, d);
    }

    // אפשר להשאיר גם פונקציות נוחות שמחזירות Coroutine (לא בשימוש אצלנו ל-yield)
    public Coroutine FadeOut(float duration = -1f) => StartCoroutine(FadeOutRoutine(duration));
    public Coroutine FadeIn(float duration = -1f)  => StartCoroutine(FadeInRoutine(duration));

    // העבודה האמיתית
    IEnumerator FadeRoutine(float from, float to, float duration)
    {
        fadeGroup.gameObject.SetActive(true);
        fadeGroup.blocksRaycasts = true;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            fadeGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        fadeGroup.alpha = to;
        fadeGroup.blocksRaycasts = to > 0.001f;
        if (fadeGroup.alpha <= 0.001f) fadeGroup.gameObject.SetActive(false);
    }
}
