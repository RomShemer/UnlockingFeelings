using UnityEngine;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [Header("Optional UI")]
    [SerializeField] CanvasGroup pauseMenu;   // קנבס של תפריט פאוז (אפשר להשאיר ריק אם אין)

    bool isPaused = false;

    void Start()
    {
        SetPauseMenu(false);
        // ודא שהזמן רץ כשנכנסים לסצנה (אם חזרת מסצנה אחרת בה היה pause)
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    // לחבר לכפתור "Pause"
    public void Pause()
    {
        if (isPaused) return;
        isPaused = true;

        // עצירת המשחק
        Time.timeScale = 0f;         // עוצר פיזיקה, אנימציות, WaitForSeconds רגיל
        AudioListener.pause = true;  // עוצר את כל האודיו שלא מתעלם מה-Listener

        SetPauseMenu(true);
    }

    // לחבר לכפתור "Resume"
    public void Resume()
    {
        if (!isPaused) return;
        isPaused = false;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        SetPauseMenu(false);
    }

    // כפתור אחד שמדליק/מכבה
    public void TogglePause()
    {
        if (isPaused) Resume();
        else Pause();
    }

    // אופציונלי: מקש ESC גם יעבוד (למחשב)
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    void SetPauseMenu(bool show)
    {
        if (!pauseMenu) return;
        pauseMenu.gameObject.SetActive(true);
        pauseMenu.alpha = show ? 1f : 0f;
        pauseMenu.interactable = show;
        pauseMenu.blocksRaycasts = show;
        if (!show) pauseMenu.gameObject.SetActive(false);
    }
}