using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomRunManager : MonoBehaviour
{
    public static RoomRunManager Instance { get; private set; }

    [Header("Include / Exclude Filters")]
    [Tooltip("אם לא ריק – ניקח רק סצינות שהנתיב שלהן מכיל אחד מהמקטעים האלה (למשל \"Scenes/Rooms\")")]
    public List<string> includePathTokens = new List<string>();     // דוגמה: { "Scenes/Rooms" }

    [Tooltip("אם הרשימה לא ריקה – ניקח רק שמות שמופיעים פה (מתעלם מה-includePathTokens)")]
    public List<string> includeExactNames = new List<string>();     // דוגמה: { "JoyRoom", "AngerRoom", "FearRoom", "SadnessRoom" }

    [Tooltip("שמות סצינה שלא ייכנסו להגרלה")]
    public List<string> excludeExactNames = new List<string>
    {
        "menu", "menuScene", "mainmenu", "end", "credits", "bootstrap", "loading"
    };

    [Tooltip("אם שם הסצינה מתחיל באחד מהערכים – תוחרג")]
    public List<string> excludeNamePrefixes = new List<string> { "dev_", "test_", "ui_" };

    [Tooltip("אם שם הסצינה מכיל אחד מהערכים – תוחרג")]
    public List<string> excludeNameSubstrings = new List<string> { "demo", "prototype" };

    [Header("Flow")]
    [Tooltip("שם סצינת התפריט הראשי לחזרה בסוף הריצה")]
    public string mainMenuScene = "menu";

    [Tooltip("סצינת סיום (אופציונלי). אם ריק – חוזרים לתפריט בסוף")]
    public string endScene = "";

    [Tooltip("לאבחון/בדיקות: סדר אקראי קבוע")]
    public bool useFixedSeed = false;

    public int fixedSeed = 12345;

    [Header("Fade (ScreenFader)")]
    [Tooltip("משך ה-FadeOut לפני טעינת סצינה")]
    public float fadeOut = 0.8f;

    [Tooltip("שהייה על שחור לפני/אחרי טעינה")]
    public float fadeHold = 0.15f;

    [Tooltip("משך ה-FadeIn אחרי טעינה")]
    public float fadeIn = 0.8f;

    public Color fadeColor = Color.black;

    // ===== מצב ריצה =====
    private readonly List<string> remaining = new List<string>();
    private bool runActive = false;
    private bool transitionLock = false; // מונע טרנזישן כפול

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // בטיחות: אם הסצינה שנטענה איכשהו עוד ברשימת החדרים – הסר כדי שלא נחזור אליה
        if (runActive)
        {
            int idx = remaining.IndexOf(scene.name);
            if (idx >= 0) remaining.RemoveAt(idx);
        }
        // שחרור הנעילה אחרי טעינה
        transitionLock = false;
    }

    /// <summary>
    /// התחלת ריצה מלאה: בונה Pool מתוך Build Settings, מערבל, טוען את הראשון עם פייד, ומסיר אותו מהרשימה.
    /// </summary>
    public void StartNewRun()
    {
        var pool = BuildRoomPool();
        if (pool.Count == 0)
        {
            Debug.LogError("[RoomRunManager] No candidate room scenes. Check filters / Build Settings.");
            return;
        }

        remaining.Clear();
        remaining.AddRange(Shuffle(pool));

        runActive = true;

        string first = remaining[0];
        remaining.RemoveAt(0);
        LoadWithFade(first);
    }

    /// <summary>
    /// אופציונלי: אם כבר טעינת ידנית חדר ראשון (ע\"י סקריפט אחר),
    /// תמשיך ריצה בלי לחזור לחדר הנוכחי.
    /// </summary>
    public void StartRunKeepingCurrent()
    {
        var pool = BuildRoomPool();
        if (pool.Count == 0)
        {
            Debug.LogError("[RoomRunManager] No candidate room scenes. Check filters / Build Settings.");
            return;
        }

        string current = SceneManager.GetActiveScene().name;
        pool.RemoveAll(n => n == current);

        remaining.Clear();
        remaining.AddRange(Shuffle(pool));
        runActive = true;
    }

    /// <summary>
    /// קריאה מהדלת/טריגר של סוף חדר – טוען את הבא עם פייד. כשנגמרים חדרים: סצינת סיום/תפריט.
    /// </summary>
    public void LoadNextRoom()
    {
        if (!runActive)
        {
            Debug.LogWarning("[RoomRunManager] Run is not active. Ignoring LoadNextRoom.");
            return;
        }
        if (transitionLock) return;
        transitionLock = true;

        if (remaining.Count == 0)
        {
            runActive = false;
            var target = string.IsNullOrEmpty(endScene) ? mainMenuScene : endScene;
            LoadWithFade(target);
            return;
        }

        string next = remaining[0];
        remaining.RemoveAt(0);
        LoadWithFade(next);
    }

    /// <summary>
    /// חזרה יזומה לתפריט (למשל מכפתור Pause).
    /// </summary>
    public void ReturnToMenu()
    {
        runActive = false;
        remaining.Clear();
        if (!transitionLock)
        {
            transitionLock = true;
            LoadWithFade(mainMenuScene);
        }
    }

    // ===== עזרי טעינה =====
    private void LoadWithFade(string sceneName)
    {
        // אם אתה מריץ סצינת חדר ישירות ללא תפריט: ודא שיש פיידר
        EnsureFader(); // בטל שורה זו אם תמיד טוענים דרך ה-Menu

        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.fadeColor = fadeColor;
            ScreenFader.Instance.FadeToScene(sceneName, fadeOut, fadeHold, fadeIn, fadeColor);
        }
        else
        {
            Debug.LogWarning("[RoomRunManager] ScreenFader.Instance == null → fallback load (no fade).");
            SceneManager.LoadScene(sceneName);
            transitionLock = false; // שחרור ידני בפולבק
        }
    }

    private void EnsureFader()
    {
        if (ScreenFader.Instance != null) return;

        // צור פיידר סינגלטון בזמן ריצה כדי להבטיח פייד גם כשמריצים סצינת חדר ישירות
        var go = new GameObject("ScreenFader");
        go.AddComponent<ScreenFader>(); // ה-Awake של ScreenFader כבר עושה DontDestroyOnLoad + חיבור מצלמה
    }

    // ===== בניית מאגר החדרים =====
    private List<string> BuildRoomPool()
    {
        var list = new List<string>();
        int count = SceneManager.sceneCountInBuildSettings;

        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);  // e.g. Assets/Scenes/Rooms/JoyRoom.unity
            string name = Path.GetFileNameWithoutExtension(path);    // e.g. JoyRoom
            if (string.IsNullOrWhiteSpace(name)) continue;

            if (!IsEligible(name, path)) continue;
            list.Add(name);
        }

        return list.Distinct().ToList();
    }

    private bool IsEligible(string sceneName, string scenePath)
    {
        // 1) White-list לפי שם
        if (includeExactNames != null && includeExactNames.Count > 0)
        {
            if (!includeExactNames.Contains(sceneName)) return false;
        }
        else
        {
            // 2) include לפי נתיב
            if (includePathTokens != null && includePathTokens.Count > 0)
            {
                string p = scenePath.Replace("\\", "/");
                bool match = includePathTokens.Any(tok =>
                    !string.IsNullOrEmpty(tok) &&
                    p.IndexOf(tok, System.StringComparison.OrdinalIgnoreCase) >= 0);
                if (!match) return false;
            }
        }

        // 3) Blacklist – שם מדויק
        if (excludeExactNames != null && excludeExactNames.Contains(sceneName)) return false;

        // 4) Blacklist – תחיליות
        if (excludeNamePrefixes != null && excludeNamePrefixes.Any(pre =>
                !string.IsNullOrEmpty(pre) && sceneName.StartsWith(pre))) return false;

        // 5) Blacklist – מחרוזות בתוך השם
        if (excludeNameSubstrings != null && excludeNameSubstrings.Any(sub =>
                !string.IsNullOrEmpty(sub) &&
                sceneName.IndexOf(sub, System.StringComparison.OrdinalIgnoreCase) >= 0)) return false;

        return true;
    }

    private List<string> Shuffle(List<string> src)
    {
        var a = new List<string>(src);
        System.Random rng = useFixedSeed ? new System.Random(fixedSeed) : new System.Random();
        for (int i = a.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (a[i], a[j]) = (a[j], a[i]);
        }
        return a;
    }
}
