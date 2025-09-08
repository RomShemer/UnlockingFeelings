using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;           
using UnityEngine.UI;          
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

    bool isPaused = false;
    float lastToggleTime = -999f;

    InputDevice rightController;
    bool lastBPressed = false;

    void Awake()
    {
        if (pauseMenuRoot != null) pauseMenuRoot.SetActive(false);
        FindRightController();
    }

    void Update()
    {
        // אם איבדנו רפרנס לבקר – ננסה שוב
        if (!rightController.isValid)
            FindRightController();

        // קורא את כפתור B (secondaryButton)
        if (rightController.isValid &&
            rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool bPressed))
        {
            // Edge detection: מעבר מ-לא לחוץ ללחוץ
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
        Time.timeScale = isPaused ? 0f : 1f;

        // UI
        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(isPaused);

        // אודיו
        if (pauseAudio)
            AudioListener.pause = isPaused;

        // עדכון UI שלא תלוי ב-timeScale
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
        Time.timeScale = 1f;
        if (pauseAudio) AudioListener.pause = false;

        // כבה את תפריט ה-Pause כדי שלא יופיע מעל הפייד
        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);

        // מעבר עם פייד אם יש Fader; אחרת מעבר רגיל
        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeToScene(menuSceneName);
        }
        else
        {
            SceneManager.LoadScene(menuSceneName);
        }
    }

    void OnDisable()
    {
        // להבטיח שלא נישאר ב-Pause אם הסקריפט ירד
        Time.timeScale = 1f;
        if (pauseAudio) AudioListener.pause = false;
        if (pauseMenuRoot != null) pauseMenuRoot.SetActive(false);
        isPaused = false;
    }
}
