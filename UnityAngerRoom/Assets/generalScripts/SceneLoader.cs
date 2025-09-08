//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using UnityEngine;
//using UnityEngine.SceneManagement;

//public class SceneLoader : MonoBehaviour
//{
//    [Header("Auto-pick rooms from Build Settings")]
//    [Tooltip("שמות סצינות שלא ייכנסו להגרלה (למשל תפריטים)")]
//    public List<string> excludeNames = new List<string> { "menuScene", "menu", "mainmenu" };

//    [Tooltip("אל תגריל את הסצינה הנוכחית")]
//    public bool excludeCurrentScene = true;

//    [Tooltip("אל תחזור על אותו חדר פעמיים ברצף")]
//    public bool avoidRepeatLastPick = true;

//    const string LastPickKey = "last_random_scene";

//    // קריאה מהכפתור: OnClick -> SceneLoader.LoadRandomScene()
//    public void LoadRandomScene()
//    {
//        var pool = GetCandidateScenes();

//        if (pool.Count == 0)
//        {
//            Debug.LogError("[SceneLoader] No candidate scenes. Check Build Settings / excludeNames.");
//            return;
//        }

//        // הימנעות מחזרה רצופה
//        if (avoidRepeatLastPick && PlayerPrefs.HasKey(LastPickKey) && pool.Count > 1)
//        {
//            string last = PlayerPrefs.GetString(LastPickKey);
//            pool.Remove(last);
//        }

//        string chosen = pool[Random.Range(0, pool.Count)];

//        if (avoidRepeatLastPick)
//            PlayerPrefs.SetString(LastPickKey, chosen);

//        // טעינה עם פייד אם קיים
//        if (ScreenFader.Instance != null)
//            ScreenFader.Instance.FadeToScene(chosen);
//        else
//            SceneManager.LoadScene(chosen);
//    }

//    // אופציונלי: טעינה ישירה בשם סצינה
//    public void LoadSceneByName(string sceneName)
//    {
//        if (ScreenFader.Instance != null)
//            ScreenFader.Instance.FadeToScene(sceneName);
//        else
//            SceneManager.LoadScene(sceneName);
//    }

//    // בונה רשימת מועמדים מתוך Build Settings
//    List<string> GetCandidateScenes()
//    {
//        var list = new List<string>();
//        int count = SceneManager.sceneCountInBuildSettings;

//        string current = SceneManager.GetActiveScene().name;

//        for (int i = 0; i < count; i++)
//        {
//            string path = SceneUtility.GetScenePathByBuildIndex(i);                   // e.g. Assets/Scenes/joyroom1.unity
//            string name = Path.GetFileNameWithoutExtension(path);                     // e.g. joyroom1
//            if (string.IsNullOrWhiteSpace(name)) continue;

//            // החרגות
//            if (excludeNames.Any(x => string.Equals(x, name)))
//                continue;

//            if (excludeCurrentScene && name == current)
//                continue;

//            list.Add(name);
//        }

//        // במקרה חריג – הסר כפילויות
//        return list.Distinct().ToList();
//    }
//}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Loader מרכזי: שומר אילו סצנות ביקרנו בהן, ומטעין רק אם מותר.
/// מומלץ להציב אותו במסך התפריט ולסמן Persist Across Scenes.
/// </summary>
[DefaultExecutionOrder(-300)]
public class SceneLoader : MonoBehaviour
{
    [Header("Lifetime")]
    [Tooltip("להשאיר את ה-SceneLoader חי בין סצנות (מונע כפילויות).")]
    public bool persistAcrossScenes = true;

    // Singleton-קליל כדי שלא יהיו כפילויות
    public static SceneLoader Instance { get; private set; }

    [Header("Visited control")]
    [Tooltip("למנוע כניסה חוזרת לסצנה שכבר ביקרנו בה.")]
    public bool preventReenterVisited = true;

    [Tooltip("לנסות יישור רצף קבוע כך שהבא בתור יתקדם מאחרי הסצנה הזו (אם משתמשים בסדר קבוע).")]
    public bool alignFixedOrderOnLoad = true;

    // מפתח לשמירה
    const string VisitedKey = "visited_scenes_csv";

    // סט שמות הסצנות שביקרנו בהן (Case-Insensitive)
    readonly HashSet<string> _visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    // ---------- אופציונלי: תמיכה ברצף קבוע ----------
    [Header("Fixed order (optional)")]
    public bool useFixedOrder = false;
    public bool loopFixedOrder = true;
    public bool persistFixedProgress = true;
    public int startFixedIndex = 0;
    public List<string> fixedOrder = new List<string>();

    const string FixedIdxKey = "fixed_order_index";
    int _volatileFixedIndex = -1;

    // ---------- Lifecycle ----------
    void Awake()
    {
        if (persistAcrossScenes)
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Instance = this;
        }

        LoadVisitedFromPrefs();
    }

    // ==================== PUBLIC API ====================

    /// <summary>
    /// מנסה לטעון את הסצנה:
    /// - אם כבר ביקרנו בה (ו-preventReenterVisited=TRUE) -> לא טוען ומחזיר FALSE.
    /// - אחרת: מסמן כמבוקרת, שומר, (אופציונלי) מיישר רצף קבוע, ומבצע טעינה. מחזיר TRUE.
    /// </summary>
    public bool LoadIfAllowed(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[SceneLoader] Empty scene name.");
            return false;
        }

        if (!IsInBuildSettings(sceneName))
        {
            Debug.LogWarning($"[SceneLoader] Scene '{sceneName}' is not in Build Settings.");
            return false;
        }

        if (preventReenterVisited && _visited.Contains(sceneName))
        {
            // כבר ביקרנו – לא טוען
            Debug.Log($"[SceneLoader] Already visited '{sceneName}'. Not loading.");
            return false;
        }

        // מסמן ביקור ושומר
        _visited.Add(sceneName);
        SaveVisitedToPrefs();

        // יישור רצף קבוע (אם בשימוש)
        if (alignFixedOrderOnLoad && useFixedOrder)
            AlignFixedOrderStartFrom(sceneName);

        // טעינה בפועל
        LoadWithOptionalFade(sceneName);
        return true;
    }

    public void LoadMenu()
    {
        LoadWithOptionalFade("menuScene");
    }

    /// <summary>בודק אם כבר ביקרנו בסצנה.</summary>
    public bool IsVisited(string sceneName) => _visited.Contains(sceneName);

    /// <summary>איפוס ביקורים + איפוס רצף קבוע (ל-New Game).</summary>
    public void ResetVisitedAndProgress()
    {
        _visited.Clear();
        SaveVisitedToPrefs();

        if (persistFixedProgress) PlayerPrefs.DeleteKey(FixedIdxKey);
        _volatileFixedIndex = -1;
        Debug.Log("[SceneLoader] Visited & progress reset.");
    }

    // ==================== INTERNALS ====================

    void LoadWithOptionalFade(string sceneName)
    {
        if (ScreenFader.Instance != null)
            ScreenFader.Instance.FadeToScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    void SaveVisitedToPrefs()
    {
        PlayerPrefs.SetString(VisitedKey, string.Join("|", _visited));
        PlayerPrefs.Save();
    }

    void LoadVisitedFromPrefs()
    {
        _visited.Clear();
        var s = PlayerPrefs.GetString(VisitedKey, "");
        if (string.IsNullOrEmpty(s)) return;
        foreach (var n in s.Split('|'))
            if (!string.IsNullOrWhiteSpace(n)) _visited.Add(n.Trim());
    }

    bool IsInBuildSettings(string sceneName)
    {
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = Path.GetFileNameWithoutExtension(path);
            if (string.Equals(name, sceneName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    // ---------- Fixed order helpers (optional) ----------
    public void AlignFixedOrderStartFrom(string sceneName)
    {
        if (!useFixedOrder) return;
        var seq = GetFixedSequencePool();
        if (seq.Count == 0) return;

        int idx = seq.FindIndex(n => string.Equals(n, sceneName, StringComparison.OrdinalIgnoreCase));
        if (idx < 0) return;

        SetCurrentFixedIndex(NextIndex(idx, seq.Count));
    }

    public void LoadNextInFixedOrder()
    {
        var seq = GetFixedSequencePool();
        if (seq.Count == 0) return;

        int idx = GetCurrentFixedIndex(seq.Count);
        string chosen = seq[idx];
        SetCurrentFixedIndex(NextIndex(idx, seq.Count));
        LoadWithOptionalFade(chosen);
    }

    List<string> GetFixedSequencePool()
    {
        if (fixedOrder != null && fixedOrder.Count > 0)
            return fixedOrder.Where(n => !string.IsNullOrWhiteSpace(n))
                             .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        int count = SceneManager.sceneCountInBuildSettings;
        var list = new List<string>(count);
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = Path.GetFileNameWithoutExtension(path);
            if (!string.IsNullOrWhiteSpace(name)) list.Add(name);
        }
        return list;
    }

    int GetCurrentFixedIndex(int length)
    {
        if (length == 0) return 0;
        if (persistFixedProgress)
            return Mathf.Clamp(PlayerPrefs.GetInt(FixedIdxKey, Mathf.Clamp(startFixedIndex, 0, length - 1)), 0, length - 1);
        if (_volatileFixedIndex < 0)
            _volatileFixedIndex = Mathf.Clamp(startFixedIndex, 0, length - 1);
        return _volatileFixedIndex;
    }

    void SetCurrentFixedIndex(int idx)
    {
        if (persistFixedProgress) PlayerPrefs.SetInt(FixedIdxKey, idx);
        else _volatileFixedIndex = idx;
    }

    int NextIndex(int current, int length)
    {
        if (length <= 0) return 0;
        int next = current + 1;
        if (next >= length) next = loopFixedOrder ? 0 : length - 1;
        return next;
    }
}

