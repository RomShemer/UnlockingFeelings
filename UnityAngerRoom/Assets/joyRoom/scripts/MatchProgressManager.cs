using UnityEngine;
using UnityEngine.UI;

public class MatchProgressManager : MonoBehaviour
{
    public static MatchProgressManager Instance { get; private set; }

    [Header("UI")]
    public Image fillImage;                  // ה-Image של ה-fill (לא חובה אם משתמשים רק ב-ProgressBarUI)
    public ProgressBG progressBarUI;      // ← הוסף שדה והגרור את הקומפוננטה של הבאנר

    [Header("Logic")]
    public int totalGoals = 0;
    public bool autoCountAtStart = true;
    public float fillLerpSpeed = 6f;

    [Header("When Done")]
    public DoorOpener doorToOpen;

    int current;
    float targetFill;
    bool opened;

    void Awake() { Instance = this; }

    void Start()
    {
        if (autoCountAtStart || totalGoals <= 0)
            totalGoals = Mathf.Max(1, FindObjectsOfType<ColorKeySnapGoal>(true).Length);

        // אתחל גם את הבאנר אם מחובר
        progressBarUI?.Init(totalGoals);

        SetFill(0f);
    }

    void Update()
    {
        if (fillImage)
            fillImage.fillAmount = Mathf.MoveTowards(fillImage.fillAmount, targetFill, Time.deltaTime * fillLerpSpeed);
    }

    void SetFill(float f)
    {
        targetFill = Mathf.Clamp01(f);
        if (fillImage) fillImage.fillAmount = targetFill;
    }

    public void ReportCorrect()
    {
        current = Mathf.Min(current + 1, totalGoals);

        // עדכן את שני ה־UI-ים (אם מחוברים)
        progressBarUI?.ReportOne();
        SetFill((float)current / Mathf.Max(1, totalGoals));
        
        if (!opened && current >= totalGoals)
        {
            opened = true;
        	Debug.Log("[MatchProgress] All goals met -> opening door", this);
            if (doorToOpen) doorToOpen.Open();
            else Debug.LogWarning("[MatchProgress] doorToOpen not assigned!", this);
        }
    }
}
