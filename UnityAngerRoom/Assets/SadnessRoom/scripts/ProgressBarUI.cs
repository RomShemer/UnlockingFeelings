using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProgressBarUI : MonoBehaviour
{
    [Header("UI")]
    public Slider slider;                 // גרור את ה-Slider של הבר
    public TextMeshProUGUI percentText;   // אופציונלי (אפשר להשאיר ריק)

    [Header("Logic")]
    [Min(1)] public int totalSteps = 10;  // כמה צעדים עד להשלמה (למשל 10 תווים)
    public bool resetOnMistake = true;    // טעות מאפסת
    public bool lockWhenComplete = true;  // אחרי השלמה לא מגיב יותר

    int currentSteps = 0;
    bool completed = false;

    void Awake()
    {
        if (slider)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
        }
        UpdateUI();
    }

    void UpdateUI()
    {
        float v = (totalSteps > 0) ? (float)currentSteps / totalSteps : 0f;
        if (slider) slider.value = v;
        if (percentText) percentText.text = Mathf.RoundToInt(v * 100f) + "%";
    }

    // קרא לזה מהלוגיקה של הפסנתר כשיש תו נכון
    public void OnCorrectNote()
    {
        if (completed && lockWhenComplete) return;

        currentSteps = Mathf.Clamp(currentSteps + 1, 0, totalSteps);

        if (currentSteps >= totalSteps)
        {
            completed = true;
            currentSteps = totalSteps;
            UpdateUI();
            return;
        }

        UpdateUI();
    }

    // קרא לזה מהלוגיקה כשיש טעות
    public void OnWrongNote()
    {
        if (completed && lockWhenComplete) return;
        if (!resetOnMistake) return;

        currentSteps = 0;
        completed = false;
        UpdateUI();
    }

    // אם תרצה לאפס ידנית
    public void ResetProgress()
    {
        currentSteps = 0;
        completed = false;
        UpdateUI();
    }

    // עוזרים קטנים (לא חובה)
    public bool IsCompleted => completed;
    public float Progress01 => (totalSteps > 0) ? (float)currentSteps / totalSteps : 0f;

#if UNITY_EDITOR
    // בדיקת מקשים בעורך: + להתקדמות, - לאיפוס
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus)) OnCorrectNote();
        if (Input.GetKeyDown(KeyCode.Minus)  || Input.GetKeyDown(KeyCode.Underscore)) OnWrongNote();
    }
#endif
}
