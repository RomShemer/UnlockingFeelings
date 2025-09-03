using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [Header("Defaults")]
    public Color fadeColor = Color.black;
    public float defaultFadeOut = 0.8f;
    public float defaultHold    = 0.15f;
    public float defaultFadeIn  = 0.8f;

    [Tooltip("אם true – Overlay (לא תלוי מצלמה). אם false – World Space מול המצלמה.")]
    public bool useOverlayInsteadOfWorldSpace = false;

    [Header("World-Space Settings")]
    [Tooltip("גודל הקנבס ב-Units (פיקסלים של UI). הגדל אם רואים קצוות.")]
    public Vector2 worldSpaceSize = new Vector2(6000, 4000);
    [Tooltip("מרחק הקנבס מהמצלמה (מטרים). 0.6–1.2 מומלץ ב-VR.")]
    public float worldSpaceDistanceMeters = 0.8f;
    [Tooltip("סקייל של הקנבס. 0.001 זה 1px ≈ 1mm בערך.")]
    public float worldSpaceScale = 0.001f;

    [Header("Overlay Settings")]
    [Tooltip("רזולוציית ייחוס לסקיילר (Overlay).")]
    public Vector2 overlayReferenceResolution = new Vector2(1920, 1080);

    [Header("Sorting")]
    [Tooltip("סדר ציור. השאר גבוה כדי להיות מעל כל ה-UI.")]
    public int sortingOrder = 5000;

    Canvas canvas;
    Image img;
    CanvasGroup cg;
    CanvasScaler scaler;
    Coroutine currentRoutine;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildCanvas();
        AttachToCurrentCamera();
        FadeImmediate(0f);

        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        AttachToCurrentCamera();
    }

    void BuildCanvas()
    {
        if (canvas != null) return;

        var root = new GameObject("ScreenFaderCanvas");
        root.transform.SetParent(transform, false);

        canvas = root.AddComponent<Canvas>();
        scaler = root.AddComponent<CanvasScaler>();
        root.AddComponent<GraphicRaycaster>();
        cg = root.AddComponent<CanvasGroup>();

        canvas.sortingOrder = sortingOrder;

        if (useOverlayInsteadOfWorldSpace)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = overlayReferenceResolution;
        }
        else
        {
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = worldSpaceSize; // <<< גדול ברירת מחדל
        }

        var imgGO = new GameObject("FadeImage");
        imgGO.transform.SetParent(root.transform, false);
        img = imgGO.AddComponent<Image>();
        img.color = fadeColor;

        var irt = img.GetComponent<RectTransform>();
        irt.anchorMin = Vector2.zero;
        irt.anchorMax = Vector2.one;
        irt.offsetMin = Vector2.zero;
        irt.offsetMax = Vector2.zero;

        img.raycastTarget = true; // חוסם קליקים בזמן פייד
    }

    void AttachToCurrentCamera()
    {
        if (canvas == null) return;

        if (useOverlayInsteadOfWorldSpace)
        {
            // Overlay לא תלוי מצלמה
            return;
        }

        var cam = FindCamera();
        if (cam == null) return;

        canvas.worldCamera = cam;
        canvas.transform.SetParent(cam.transform, false);

        // מיקום מול המצלמה במרחק נתון
        canvas.transform.localPosition = new Vector3(0, 0, Mathf.Max(0.01f, worldSpaceDistanceMeters));
        canvas.transform.localRotation = Quaternion.identity;
        canvas.transform.localScale = Vector3.one * Mathf.Max(0.0001f, worldSpaceScale);

        // ודא שה-Rect מספיק גדול
        var rt = canvas.GetComponent<RectTransform>();
        rt.sizeDelta = worldSpaceSize;
    }

    Camera FindCamera()
    {
        string[] names = { "CenterEyeAnchor", "Main Camera", "Camera" };
        foreach (var n in names)
        {
            var go = GameObject.Find(n);
            if (go && go.TryGetComponent(out Camera c)) return c;
        }
        if (Camera.main) return Camera.main;
        var any = FindObjectOfType<Camera>();
        return any ? any : null;
    }

    // ===== API =====

    public void FadeImmediate(float alpha)
    {
        BuildCanvas();
        cg.alpha = Mathf.Clamp01(alpha);
    }

    public Coroutine Fade(float to, float seconds, Color? colorOverride = null)
    {
        BuildCanvas();
        AttachToCurrentCamera();
        if (colorOverride.HasValue) img.color = colorOverride.Value;

        if (currentRoutine != null) StopCoroutine(currentRoutine);
        if (to >= 0.999f && cg.alpha >= 0.999f) cg.alpha = 0f; // הבטח שנראה FadeOut
        currentRoutine = StartCoroutine(FadeRoutine(to, Mathf.Max(0f, seconds)));
        return currentRoutine;
    }

    IEnumerator FadeRoutine(float to, float seconds)
    {
        float from = cg.alpha;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, seconds > 0 ? t / seconds : 1f);
            yield return null;
        }
        cg.alpha = to;
        currentRoutine = null;
    }

    public void FadeToScene(string sceneName, float fadeOut = -1f, float hold = -1f, float fadeIn = -1f, Color? colorOverride = null)
    {
        BuildCanvas();
        AttachToCurrentCamera();
        if (colorOverride.HasValue) img.color = colorOverride.Value;

        if (currentRoutine != null) StopCoroutine(currentRoutine);

        float fo = fadeOut < 0 ? defaultFadeOut : fadeOut;
        float ho = hold    < 0 ? defaultHold    : hold;
        float fi = fadeIn  < 0 ? defaultFadeIn  : fadeIn;

        currentRoutine = StartCoroutine(FadeToSceneRoutine(sceneName, fo, ho, fi));
    }

    IEnumerator FadeToSceneRoutine(string sceneName, float fadeOut, float hold, float fadeIn)
    {
        if (cg.alpha >= 0.999f) cg.alpha = 0f; // אם כבר שחור – התחל משקוף

        yield return FadeRoutine(1f, fadeOut);
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, hold));

        Time.timeScale = 1f;
        AudioListener.pause = false;

        var op = SceneManager.LoadSceneAsync(sceneName);
        yield return op;

        AttachToCurrentCamera();
        yield return FadeRoutine(0f, fadeIn);

        currentRoutine = null;
    }

    // קיצורים נוחים
    public Coroutine FadeOut(float duration = -1f) => Fade(1f, duration < 0 ? defaultFadeOut : duration);
    public Coroutine FadeIn (float duration = -1f) => Fade(0f, duration < 0 ? defaultFadeIn  : duration);
}
