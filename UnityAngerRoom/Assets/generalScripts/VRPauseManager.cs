using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SceneManagement;

public class VRPauseManager : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Canvas / Root של תפריט ה-Pause (יודלק/יכבה אוטומטית)")]
    public GameObject pauseMenuRoot;   // גרור לכאן את קאנבס ה-Pause

    [Header("Behavior")]
    [Tooltip("האם לעצור גם את האודיו בזמן Pause")]
    public bool pauseAudio = true;

    [Tooltip("כדי למנוע חזרה מהירה כשמחזיקים את הכפתור")]
    public float buttonDebounceSeconds = 0.25f;

    [Header("Navigation")]
    [Tooltip("שם סצינת התפריט אליה נחזור בכפתור Quit")]
    public string menuSceneName = "menuScene";

    [Header("Integration")]
    [Tooltip("רפרנס ל-InstructionsOverlay כדי לחסום Y בזמן Pause ולסגור הוראות כש-Pause נפתח")]
    public InstructionsOverlay instructionsOverlay;   // גרור מהאינספקטור (או יימצא אוטומטית)

    bool isPaused = false;
    float lastToggleTime = -999f;

    InputDevice rightController;
    bool lastBPressed = false;

    void Awake()
    {
        if (pauseMenuRoot != null) pauseMenuRoot.SetActive(false);
        if (!instructionsOverlay)
            instructionsOverlay = FindObjectOfType<InstructionsOverlay>(true); // מוצא גם אם Disabled
        FindRightController();
    }

    void Update()
    {
        if (!rightController.isValid)
            FindRightController();

        // כפתור B (secondaryButton ביד ימין)
        if (rightController.isValid &&
            rightController.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool bPressed))
        {
            if (bPressed && !lastBPressed && Time.unscaledTime - lastToggleTime >= buttonDebounceSeconds)
            {
                TogglePause();
                lastToggleTime = Time.unscaledTime;
            }
            lastBPressed = bPressed;
        }
        else
        {
            lastBPressed = false;
        }
    }

    void FindRightController()
    {
        var list = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, list);
        if (list.Count > 0) rightController = list[0];
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        // זמן משחק
        if (isPaused)
        {
            Time.timeScale = 0f;
        }
        else
        {
            // אם עדיין יש נעילת התחלה של מסך ההוראות – השאר timeScale=0
            if (instructionsOverlay != null && instructionsOverlay.IsInitialLockActive)
                Time.timeScale = 0f;
            else
                Time.timeScale = 1f;
        }

        // UI
        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(isPaused);

        // אודיו
        if (pauseAudio)
            AudioListener.pause = isPaused;

        // אינטגרציה עם מסך הוראות:
        if (isPaused) instructionsOverlay?.NotifyPauseOpened();   // יחסום Y ויסגור הוראות אם פתוח
        else          instructionsOverlay?.NotifyPauseClosed();   // יחזיר אפשרות לפתוח הוראות

        Canvas.ForceUpdateCanvases();
    }

    // חיבור לכפתור Resume בתפריט
    public void Resume()
    {
        if (isPaused) TogglePause();
    }

    // חיבור לכפתור Quit בתפריט — מעבר לתפריט עם Fade
    public void QuitToMenu()
    {
        // בטל Pause לחלוטין
        isPaused = false;

        // אם עדיין יש נעילת התחלה של ההוראות – שמור על timeScale=0, אחרת 1
        if (instructionsOverlay != null && instructionsOverlay.IsInitialLockActive)
            Time.timeScale = 0f;
        else
            Time.timeScale = 1f;

        if (pauseAudio) AudioListener.pause = false;

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);

        // עדכן את ההוראות שה-Pause נסגר
        instructionsOverlay?.NotifyPauseClosed();

        if (ScreenFader.Instance != null)
            ScreenFader.Instance.FadeToScene(menuSceneName);
        else
            SceneManager.LoadScene(menuSceneName);
    }

    void OnDisable()
    {
        // להבטיח שלא נישאר ב-Pause אם הסקריפט ירד
        isPaused = false;
        if (instructionsOverlay != null && instructionsOverlay.IsInitialLockActive)
            Time.timeScale = 0f;
        else
            Time.timeScale = 1f;

        if (pauseAudio) AudioListener.pause = false;
        if (pauseMenuRoot != null) pauseMenuRoot.SetActive(false);

        instructionsOverlay?.NotifyPauseClosed();
    }
}
