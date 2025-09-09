using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomRunManager : MonoBehaviour
{
    public static RoomRunManager Instance { get; private set; }

    // ====== DOORS MODE (חדש) ======
    [Header("Doors mode (menu)")]
    [Tooltip("לאפשר טעינה דרך דלתות כבר בתחילת המשחק (אופציונלי).")]
    public bool doorsEnabledAtStart = false;

    /// <summary>מצב השער: האם מותר לדלתות לטעון סצנות כעת.</summary>
    public bool DoorsEnabled { get; private set; }
    public static bool AreDoorsEnabled => Instance != null && Instance.DoorsEnabled;

    /// <summary>
    /// כפתור “משחק חדש” כשבוחרים חדרים דרך דלתות:
    /// - מאפס ביקורים בלודר
    /// - מאפס מצב ריצה פנימי
    /// - מאפשר טעינה דרך דלתות
    /// </summary>
    public void NewGameDoorsMode()
    {
        var loader = SceneLoader.Instance ?? FindFirstObjectByType<SceneLoader>();
        if (loader != null)
            loader.ResetVisitedAndProgress();

        runActive = false;
        remaining.Clear();

        RunStats.Instance.BeginNewRun();

        SetDoorsEnabled(true);
        Debug.Log("[RoomRunManager] New Game (doors mode): visited cleared; doors enabled.");
    }

    public void LoadMenu()
    {
        var loader = SceneLoader.Instance ?? FindFirstObjectByType<SceneLoader>();
        if (loader != null)
        {
            bool finished = RunStats.Instance != null && RunStats.Instance.IsRunFinished();

            if (finished)
            {
                RunStats.Instance.GoToSummaryScene();   // יחליט לבד win/lose
            }
            else
            { 
                loader.LoadMenu(); 
            }
            Debug.Log("[RoomExit] Marked current room as completed in RunStats.");
        }

    }
    
    public void LoadMainMenuAfterWinLoose()
    {
        var loader = SceneLoader.Instance ?? FindFirstObjectByType<SceneLoader>();
        if (loader != null)
        {
            loader.LoadMenu();
        }
    }
    
    //public void updateRunStatsLoadNewRoom(string roomName)
    //{
    //    RunStats.Instance.StartRoom(roomName);
    //}

    /// <summary>לאפשר/לחסום טעינה דרך דלתות בכל שלב.</summary>
    public void SetDoorsEnabled(bool on) => DoorsEnabled = on;

    // ====== Include / Exclude (כמו שהיה) ======
    [Header("Include / Exclude Filters")]
    public List<string> includePathTokens = new List<string>();
    public List<string> includeExactNames = new List<string>();
    public List<string> excludeExactNames = new List<string>
    { "menu", "menuScene", "mainmenu", "end", "credits", "bootstrap", "loading" };
    public List<string> excludeNamePrefixes = new List<string> { "dev_", "test_", "ui_" };
    public List<string> excludeNameSubstrings = new List<string> { "demo", "prototype" };

    [Header("Flow")]
    public string mainMenuScene = "menu";
    public string endScene = "";
    public bool useFixedSeed = false;
    public int fixedSeed = 12345;

    [Header("Fade (ScreenFader)")]
    public float fadeOut = 0.8f;
    public float fadeHold = 0.15f;
    public float fadeIn = 0.8f;
    public Color fadeColor = Color.black;

    // ===== מצב ריצה (כמו שהיה) =====
    private readonly List<string> remaining = new List<string>();
    private bool runActive = false;
    private bool transitionLock = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        DoorsEnabled = doorsEnabledAtStart; // סטארט ברירת מחדל
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (runActive)
        {
            int idx = remaining.IndexOf(scene.name);
            if (idx >= 0) remaining.RemoveAt(idx);
        }
        transitionLock = false;
    }

    // ===== ריצה אקראית (כמו שהיה, למצב “מסלול”) =====
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

        // כשעובדים במצב “מסלול”, נטריל את הדלתות כדי שלא יעקפו את הרצף
        SetDoorsEnabled(false);

        string first = remaining[0];
        remaining.RemoveAt(0);
        LoadWithFade(first);
    }

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

        SetDoorsEnabled(false);
    }
    public void LoadNextRoom()
    {
        if (!runActive) { Debug.LogWarning("[RoomRunManager] Run is not active."); return; }
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

    // ===== טעינה עם פייד =====
    private void LoadWithFade(string sceneName)
    {
        EnsureFader();

        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.fadeColor = fadeColor;
            ScreenFader.Instance.FadeToScene(sceneName, fadeOut, fadeHold, fadeIn, fadeColor);
        }
        else
        {
            Debug.LogWarning("[RoomRunManager] No ScreenFader found → direct load.");
            SceneManager.LoadScene(sceneName);
            transitionLock = false;
        }
    }

    private void EnsureFader()
    {
        if (ScreenFader.Instance != null) return;
        var go = new GameObject("ScreenFader");
        go.AddComponent<ScreenFader>();
    }

    // ===== בניית מאגר חדרים =====
    private List<string> BuildRoomPool()
    {
        var list = new List<string>();
        int count = SceneManager.sceneCountInBuildSettings;

        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrWhiteSpace(name)) continue;

            if (!IsEligible(name, path)) continue;
            list.Add(name);
        }
        return list.Distinct().ToList();
    }

    private bool IsEligible(string sceneName, string scenePath)
    {
        if (includeExactNames != null && includeExactNames.Count > 0)
        {
            if (!includeExactNames.Contains(sceneName)) return false;
        }
        else
        {
            if (includePathTokens != null && includePathTokens.Count > 0)
            {
                string p = scenePath.Replace("\\", "/");
                bool match = includePathTokens.Any(tok =>
                    !string.IsNullOrEmpty(tok) &&
                    p.IndexOf(tok, System.StringComparison.OrdinalIgnoreCase) >= 0);
                if (!match) return false;
            }
        }

        if (excludeExactNames != null && excludeExactNames.Contains(sceneName)) return false;
        if (excludeNamePrefixes != null && excludeNamePrefixes.Any(pre =>
            !string.IsNullOrEmpty(pre) && sceneName.StartsWith(pre))) return false;
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