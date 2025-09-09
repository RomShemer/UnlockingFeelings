using System.Collections;
using UnityEngine;

public class UIFadeSwitcher : MonoBehaviour
{
    [Header("Canvas Groups")]
    [Tooltip("CanvasGroup של תפריט ה-Menu")]
    public CanvasGroup menuCanvas;

    [Tooltip("CanvasGroup של מסך ההוראות")]
    public CanvasGroup instructionsCanvas;

    [Header("Fade")]
    [Tooltip("זמן פייד בשניות")]
    public float fadeDuration = 0.5f;
    [Tooltip("עקומת האינטרפולציה של הפייד (לא חובה)")]
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    bool isBusy;

    void Awake()
    {
        // מצב פתיחה: מציגים תפריט, מסתירים הוראות
        SetCanvasGroup(menuCanvas, 1f, true, true);
        SetCanvasGroup(instructionsCanvas, 0f, false, false);
        if (instructionsCanvas) instructionsCanvas.gameObject.SetActive(false);
    }

    // מחובר ל-Button OnClick של "New Game"
    public void OnNewGame()
    {
        if (isBusy) return;
        StartCoroutine(FadeSwap(menuCanvas, instructionsCanvas));
    }

    // פונקציות עזר
    static void SetCanvasGroup(CanvasGroup cg, float alpha, bool interactable, bool blocksRaycast)
    {
        if (!cg) return;
        cg.alpha = alpha;
        cg.interactable = interactable;
        cg.blocksRaycasts = blocksRaycast;
    }

    IEnumerator Fade(CanvasGroup from, CanvasGroup to, float duration)
    {
        isBusy = true;

        if (from) SetCanvasGroup(from, from.alpha, false, true);
        if (to)   SetCanvasGroup(to, to.alpha, false, true);

        float t = 0f;
        float startA = from ? from.alpha : 0f;
        float startB = to   ? to.alpha   : 0f;
        float endA   = 0f;
        float endB   = 1f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            float e = fadeCurve != null ? fadeCurve.Evaluate(k) : k;

            if (from) from.alpha = Mathf.Lerp(startA, endA, e);
            if (to)   to.alpha   = Mathf.Lerp(startB, endB, e);
            yield return null;
        }

        if (from)
        {
            SetCanvasGroup(from, 0f, false, false);
            from.gameObject.SetActive(false);   // ← ממש מכבה את הקאנבס
        }

        if (to)
        {
            to.gameObject.SetActive(true);      // ← ודא שהקאנבס מופעל
            SetCanvasGroup(to, 1f, true, true);
        }

        isBusy = false;
    }

    IEnumerator FadeSwap(CanvasGroup from, CanvasGroup to)
    {
        if (to) to.gameObject.SetActive(true);
        if (from && !from.gameObject.activeSelf) from.gameObject.SetActive(true);

        yield return StartCoroutine(Fade(from, to, fadeDuration));
    }

    public void ShowMenu()
    {
        if (!isBusy) StartCoroutine(FadeSwap(instructionsCanvas, menuCanvas));
    }

    public void ShowInstructions()
    {
        if (!isBusy) StartCoroutine(FadeSwap(menuCanvas, instructionsCanvas));
    }
}
