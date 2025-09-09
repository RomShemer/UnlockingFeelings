//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.XR;           
//using UnityEngine.UI;          
//using UnityEngine.SceneManagement;

//public class VRPauseManager : MonoBehaviour
//{
//    [Header("UI")]
//    [Tooltip("Canvas / Root של תפריט ה-Pause (יודלק/יכבה אוטומטית)")]
//    public GameObject pauseMenuRoot;   // גרור לכאן את קאנבס ה-Pause

//    [Header("Behavior")]
//    [Tooltip("האם לעצור גם את האודיו בזמן Pause")]
//    public bool pauseAudio = true;

//    [Tooltip("כדי למנוע חזרה מהירה כשמחזיקים את הכפתור")]
//    public float buttonDebounceSeconds = 0.25f;

//    [Header("Navigation")]
//    [Tooltip("שם סצינת התפריט אליה נחזור בכפתור Quit")]
//    public string menuSceneName = "menuScene";

//    bool isPaused = false;
//    float lastToggleTime = -999f;

//    InputDevice rightController;
//    bool lastBPressed = false;

//    void Awake()
//    {
//        if (pauseMenuRoot != null) pauseMenuRoot.SetActive(false);
//        FindRightController();
//    }

//    void Update()
//    {
//        // אם איבדנו רפרנס לבקר – ננסה שוב
//        if (!rightController.isValid)
//            FindRightController();

//        // קורא את כפתור B (secondaryButton)
//        if (rightController.isValid &&
//            rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool bPressed))
//        {
//            // Edge detection: מעבר מ-לא לחוץ ללחוץ
//            if (bPressed && !lastBPressed && Time.unscaledTime - lastToggleTime >= buttonDebounceSeconds)
//            {
//                TogglePause();
//                lastToggleTime = Time.unscaledTime;
//            }
//            lastBPressed = bPressed;
//        }
//        else
//        {
//            lastBPressed = false;
//        }
//    }

//    void FindRightController()
//    {
//        var list = new List<InputDevice>();
//        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, list);
//        if (list.Count > 0) rightController = list[0];
//    }

//    public void TogglePause()
//    {
//        isPaused = !isPaused;

//        // זמן משחק
//        Time.timeScale = isPaused ? 0f : 1f;

//        // UI
//        if (pauseMenuRoot != null)
//            pauseMenuRoot.SetActive(isPaused);

//        // אודיו
//        if (pauseAudio)
//            AudioListener.pause = isPaused;

//        // עדכון UI שלא תלוי ב-timeScale
//        Canvas.ForceUpdateCanvases();
//    }

//    // חיבור לכפתור Resume בתפריט
//    public void Resume()
//    {
//        if (isPaused) TogglePause();
//    }

//    // חיבור לכפתור Quit בתפריט — מעבר לתפריט עם Fade
//    public void QuitToMenu()
//    {
//        // בטל Pause לחלוטין
//        isPaused = false;
//        Time.timeScale = 1f;
//        if (pauseAudio) AudioListener.pause = false;

//        // כבה את תפריט ה-Pause כדי שלא יופיע מעל הפייד
//        if (pauseMenuRoot != null)
//            pauseMenuRoot.SetActive(false);

//        // מעבר עם פייד אם יש Fader; אחרת מעבר רגיל
//        if (ScreenFader.Instance != null)
//        {
//            ScreenFader.Instance.FadeToScene(menuSceneName);
//        }
//        else
//        {
//            SceneManager.LoadScene(menuSceneName);
//        }
//    }

//    void OnDisable()
//    {
//        // להבטיח שלא נישאר ב-Pause אם הסקריפט ירד
//        Time.timeScale = 1f;
//        if (pauseAudio) AudioListener.pause = false;
//        if (pauseMenuRoot != null) pauseMenuRoot.SetActive(false);
//        isPaused = false;
//    }
//}

//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.XR;
//using UnityEngine.UI;
//using UnityEngine.SceneManagement;

//public class VRPauseManager : MonoBehaviour
//{
//    public enum WhichButton { A_Primary, B_Secondary }

//    [Header("UI")]
//    [Tooltip("Canvas / Root של תפריט ה-Pause (יודלק/יכבה אוטומטית)")]
//    public GameObject pauseMenuRoot;

//    [Header("Behavior")]
//    [Tooltip("האם לעצור גם את האודיו בזמן Pause")]
//    public bool pauseAudio = true;

//    [Tooltip("כדי למנוע טריגר כפול כשמחזיקים את הכפתור")]
//    public float buttonDebounceSeconds = 0.25f;

//    [Header("Navigation")]
//    [Tooltip("שם סצנת התפריט אליה נחזור בכפתור Quit")]
//    public string menuSceneName = "menuScene";

//    [Header("Input")]
//    [Tooltip("איזה כפתור בבקר ימין מפעיל את ה-Pause (A או B)")]
//    public WhichButton button = WhichButton.B_Secondary;

//    [Tooltip("במצבי דיבוג: הדפס מצב כפתורים למסוף")]
//    public bool logDebug = false;

//    bool isPaused = false;
//    float lastToggleTime = -999f;

//    InputDevice rightController;
//    bool lastPressed = false;

//    void Awake()
//    {
//        if (pauseMenuRoot != null) pauseMenuRoot.SetActive(false);
//        FindRightController();
//    }

//    void Update()
//    {
//        // אם אבד רפרנס – נסה לגלות מחדש
//        if (!rightController.isValid)
//            FindRightController();

//        bool pressedNow = false;

//        if (rightController.isValid)
//        {
//            // קריאה בטוחה לפי הכפתור שנבחר
//            if (button == WhichButton.A_Primary)
//                rightController.TryGetFeatureValue(CommonUsages.primaryButton, out pressedNow);
//            else
//                rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out pressedNow);

//            if (logDebug)
//            {
//                rightController.TryGetFeatureValue(CommonUsages.primaryButton, out bool a);
//                rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool b);
//                Debug.Log($"[VRPauseManager] Right valid={rightController.isValid}  A(primary)={a}  B(secondary)={b}");
//            }

//            // Edge detection + דיבאונס
//            if (pressedNow && !lastPressed && Time.unscaledTime - lastToggleTime >= buttonDebounceSeconds)
//            {
//                TogglePause();
//                lastToggleTime = Time.unscaledTime;
//            }
//            lastPressed = pressedNow;
//        }
//        else
//        {
//            lastPressed = false;
//        }
//    }

//    // חיפוש בקר ימין באופן יציב (Characteristics) + פולבאק ל-XRNode
//    void FindRightController()
//    {
//        var list = new List<InputDevice>();

//        InputDevices.GetDevicesWithCharacteristics(
//            InputDeviceCharacteristics.Controller |
//            InputDeviceCharacteristics.HeldInHand |
//            InputDeviceCharacteristics.Right, list);

//        if (list.Count > 0)
//        {
//            rightController = list[0];
//            return;
//        }

//        // פולבאק אם לא נמצא
//        list.Clear();
//        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, list);
//        if (list.Count > 0) rightController = list[0];
//    }

//    public void TogglePause()
//    {
//        isPaused = !isPaused;

//        // זמן משחק
//        Time.timeScale = isPaused ? 0f : 1f;

//        // UI
//        if (pauseMenuRoot != null)
//            pauseMenuRoot.SetActive(isPaused);

//        // אודיו
//        if (pauseAudio)
//            AudioListener.pause = isPaused;

//        // עדכון UI שלא תלוי ב-timeScale
//        Canvas.ForceUpdateCanvases();
//    }

//    // חיבור לכפתור Resume בתפריט
//    public void Resume()
//    {
//        if (isPaused) TogglePause();
//    }

//    // חיבור לכפתור Quit בתפריט — מעבר לתפריט (עם/בלי פייד)
//    public void QuitToMenu()
//    {
//        // בטל Pause לחלוטין
//        isPaused = false;
//        Time.timeScale = 1f;
//        if (pauseAudio) AudioListener.pause = false;

//        if (pauseMenuRoot != null)
//            pauseMenuRoot.SetActive(false);

//        var fader = FindObjectOfType<ScreenFader>(); // אם אין סינגלטון
//        if (fader != null)
//            fader.FadeToScene(menuSceneName);
//        else
//            SceneManager.LoadScene(menuSceneName);
//    }

//    void OnDisable()
//    {
//        // להבטיח שלא נישאר ב-Pause אם הסקריפט ירד
//        Time.timeScale = 1f;
//        if (pauseAudio) AudioListener.pause = false;
//        if (pauseMenuRoot != null) pauseMenuRoot.SetActive(false);
//        isPaused = false;
//    }
//}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class VRPauseManager : MonoBehaviour
{
    public enum WhichButton { A_Primary, B_Secondary }

    public static VRPauseManager Instance { get; private set; }

    [Header("UI")]
    [Tooltip("Canvas / Root של תפריט ה-Pause (יודלק/יכבה אוטומטית)")]
    public GameObject pauseMenuRoot;

    [Header("Behavior")]
    public bool pauseAudio = true;
    public float buttonDebounceSeconds = 0.25f;

    [Header("Navigation")]
    public string menuSceneName = "menuScene";

    [Header("Input")]
    public WhichButton button = WhichButton.B_Secondary;
    public bool logDebug = false;

    bool isPaused = false;
    float lastToggleTime = -999f;

    InputDevice rightController;
    bool lastPressed = false;

    void Awake()
    {
        // Singleton guard – מופע יחיד
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // התחברות לאירועים של טעינת סצנה/שינוי התקן
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        InputDevices.deviceConnected += OnDeviceChanged;
        InputDevices.deviceConfigChanged += OnDeviceChanged;

        // חיבור ראשוני
        BindPauseMenuInCurrentScene();
        if (pauseMenuRoot != null) pauseMenuRoot.SetActive(false);

        FindRightController();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            InputDevices.deviceConnected -= OnDeviceChanged;
            InputDevices.deviceConfigChanged -= OnDeviceChanged;
        }
    }

    void OnActiveSceneChanged(Scene prev, Scene next)
    {
        // אתחלי קנבס לפי הסצנה החדשה
        BindPauseMenuInCurrentScene();

        // תני “טיפה זמן” למערכת להרים את הבקר ואז תחפשי שוב
        Invoke(nameof(FindRightController), 0.1f);

        // אם היינו בפאוז, השאירי מצב עקבי
        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
        if (pauseAudio) AudioListener.pause = isPaused;
    }

    void OnDeviceChanged(InputDevice _) => FindRightController();

    void Update()
    {
        if (!rightController.isValid)
            FindRightController();

        bool pressedNow = false;

        if (rightController.isValid)
        {
            if (button == WhichButton.A_Primary)
                rightController.TryGetFeatureValue(CommonUsages.primaryButton, out pressedNow);
            else
                rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out pressedNow);

            if (logDebug)
            {
                rightController.TryGetFeatureValue(CommonUsages.primaryButton, out bool a);
                rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool b);
                Debug.Log($"[VRPause] Right valid={rightController.isValid}  A={a}  B={b}");
            }

            if (pressedNow && !lastPressed && Time.unscaledTime - lastToggleTime >= buttonDebounceSeconds)
            {
                TogglePause();
                lastToggleTime = Time.unscaledTime;
            }
            lastPressed = pressedNow;
        }
        else
        {
            lastPressed = false;
        }
    }

    void FindRightController()
    {
        var list = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Controller |
            InputDeviceCharacteristics.HeldInHand |
            InputDeviceCharacteristics.Right, list);

        if (list.Count > 0)
        {
            rightController = list[0];
            return;
        }

        list.Clear();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, list);
        if (list.Count > 0) rightController = list[0];
    }

    void BindPauseMenuInCurrentScene()
    {
        // חפש את הקנבס המסומן בסצנה הנוכחית (גם אם הוא Inactive)
        var marker = FindObjectOfType<PauseMenuRootMarker>(true);
        if (marker != null)
        {
            pauseMenuRoot = marker.gameObject;
        }
        else
        {
            // לא חובה, אבל נחמד לדיבוג
            Debug.LogWarning("[VRPause] PauseMenuRootMarker לא נמצא בסצנה הזו.");
            pauseMenuRoot = null;
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        Time.timeScale = isPaused ? 0f : 1f;

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(isPaused);

        if (pauseAudio)
            AudioListener.pause = isPaused;

        Canvas.ForceUpdateCanvases();
    }

    public void Resume()
    {
        if (isPaused) TogglePause();
    }

    public void QuitToMenu()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pauseAudio) AudioListener.pause = false;
        if (pauseMenuRoot != null) pauseMenuRoot.SetActive(false);

        var fader = FindObjectOfType<ScreenFader>();
        if (fader != null) fader.FadeToScene(menuSceneName);
        else SceneManager.LoadScene(menuSceneName);
    }

    void OnDisable()
    {
        // אם מישהו כיבה זמנית את האובייקט – הבטיחי שאין “פאוז תקוע”
        Time.timeScale = 1f;
        if (pauseAudio) AudioListener.pause = false;
        if (pauseMenuRoot != null) pauseMenuRoot.SetActive(false);
        isPaused = false;
    }
}