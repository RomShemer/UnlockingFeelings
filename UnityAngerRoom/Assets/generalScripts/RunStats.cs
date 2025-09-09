//using System.Collections.Generic;
//using System.Text;
//using UnityEngine;
//using UnityEngine.SceneManagement;

//public class RunStats : MonoBehaviour
//{
//    public static RunStats Instance { get; private set; }

//    [System.Serializable]
//    public class RoomEntry
//    {
//        public string roomName;
//        public float startAt;      // realtimeSinceStartup בתחילת החדר
//        public float duration;     // משך עד סיום (מילוי ב-complete/fail)
//        public bool completed;     // true אם יצא דרך RoomExit
//        public bool failed;        // true אם עבר ל-loseRoom (למשל timeout)
//        public string failReason;  // "timeout" / "exit" / סיבה אחרת לאבחון
//    }

//    // היסטוריה של כל החדרים שנשוחקו בריצה
//    public readonly List<RoomEntry> history = new List<RoomEntry>();
//    RoomEntry current;

//    void Awake()
//    {
//        if (Instance != null) { Destroy(gameObject); return; }
//        Instance = this;
//        DontDestroyOnLoad(gameObject);

//        // רישום מאזין לטעינת סצנות
//        SceneManager.sceneLoaded += OnSceneLoaded;
//    }

//    void OnDestroy()
//    {
//        if (Instance == this)
//            SceneManager.sceneLoaded -= OnSceneLoaded;
//    }

//    // נפתח רשומה חדשה בכל טעינת סצנת "חדר" (לא תפריט/ניצחון/הפסד)
//    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
//    {
//        string s = scene.name;
//        if (s == "menuScene" || s == "winRoom" || s == "loseRoom") return;

//        StartRoom(s);
//    }

//    // --- API ---

//    public void StartRoom(string roomName)
//    {
//        // סגירה בטוחה של חדר קודם אם משום מה נשאר פתוח
//        if (current != null && !current.completed && !current.failed)
//        {
//            current.duration = Time.realtimeSinceStartup - current.startAt;
//        }

//        current = new RoomEntry
//        {
//            roomName = roomName,
//            startAt  = Time.realtimeSinceStartup
//        };
//        history.Add(current);
//        // Debug.Log($"[RunStats] StartRoom: {roomName}");
//    }

//    public void CompleteCurrent(string reason = "exit")
//    {
//        if (current == null) return;
//        if (current.completed || current.failed) return;

//        current.completed = true;
//        current.duration  = Time.realtimeSinceStartup - current.startAt;
//        current.failReason = reason; // למעקב; לא נחשב כישלון
//        // Debug.Log($"[RunStats] Complete: {current.roomName} ({FormatTime(current.duration)})");
//    }

//    public void FailCurrent(string reason = "timeout")
//    {
//        if (current == null) return;
//        if (current.completed || current.failed) return;

//        current.failed   = true;
//        current.duration = Time.realtimeSinceStartup - current.startAt;
//        current.failReason = reason;
//        // Debug.Log($"[RunStats] Fail: {current.roomName} ({reason}) {FormatTime(current.duration)}");
//    }

//    public void ResetAll()
//    {
//        history.Clear();
//        current = null;
//        // Debug.Log("[RunStats] ResetAll");
//    }

//    // --- Builders לטקסטים למסכי UI ---

//    // Achievements: כל החדרים שהושלמו
//    public string BuildAchievementsText()
//    {
//        var sb = new StringBuilder();
//        foreach (var r in history)
//            if (r.completed)
//                sb.AppendLine($"• {r.roomName} ✓");
//        if (sb.Length == 0) sb.Append("No achievements yet.");
//        return sb.ToString();
//    }

//    // Challenges: כל החדרים שנכשלו (תיקון: לא רק הראשון)
//    public string BuildChallengesText()
//    {
//        var failed = history.FindAll(r => r.failed);
//        if (failed.Count == 0) return "No failures yet.";

//        var sb = new StringBuilder();
//        foreach (var r in failed)
//            sb.AppendLine($"• Failed in {r.roomName} ({r.failReason})");
//        return sb.ToString();
//    }

//    // Progress: זמן בכל חדר (גם אם נכשל)
//    public string BuildProgressText()
//    {
//        if (history.Count == 0) return "No rooms played yet.";

//        var sb = new StringBuilder();
//        foreach (var r in history)
//        {
//            string status = r.failed ? " (failed)" : (r.completed ? " (completed)" : "");
//            sb.AppendLine($"• {r.roomName} — {FormatTime(r.duration)}{status}");
//        }
//        return sb.ToString();
//    }

//    public static string FormatTime(float seconds)
//    {
//        if (seconds < 0f) seconds = 0f;
//        int m = Mathf.FloorToInt(seconds / 60f);
//        int s = Mathf.FloorToInt(seconds - m * 60);
//        return $"{m:00}:{s:00}";
//    }
//}

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using UnityEngine;
//using UnityEngine.SceneManagement;

//public class RunStats : MonoBehaviour
//{
//    public static RunStats Instance { get; private set; }

//    [Header("Scenes")]
//    public string menuSceneName = "menuScene";
//    public string winSceneName = "winRoom";
//    public string loseSceneName = "loseRoom";

//    [Header("Game Flow (החדרים הנדרשים לניצחון)")]
//    public List<string> requiredRooms = new() { "AngerScene", "FearScene", "JoyScene 1", "SadnessScene" };

//    // ===== מודלים =====
//    [Serializable]
//    public class RoomEntry
//    {
//        public string roomName;
//        public float startAt;     // realtimeSinceStartup בתחילת החדר
//        public float duration;    // משך החדר (נסגר בהשלמה/כישלון)
//        public bool completed;    // true אם הושלם
//        public bool failed;       // true אם נכשל
//        public string failReason; // "timeout" / "quit" / ...
//    }

//    [Serializable]
//    public class AggregateRoomStats
//    {
//        public string roomName;
//        public int attempts;
//        public int wins;
//        public int fails;
//        public float bestTime = float.PositiveInfinity;
//        public float totalTime;

//        public float AvgTime => attempts > 0 ? totalTime / attempts : 0f;
//        public string BestTimeStr => float.IsInfinity(bestTime) ? "—" : FormatTime(bestTime);
//    }

//    [Serializable]
//    public class RunSummary
//    {
//        public List<RoomEntry> entries = new();
//        public float totalTime;
//        public int wins;
//        public int fails;
//        public bool allRequiredCompleted;
//    }

//    // ===== נתוני ריצה =====
//    public readonly List<RoomEntry> history = new();
//    public readonly Dictionary<string, AggregateRoomStats> aggregate = new();

//    RoomEntry current;
//    bool runActive;
//    float runStart;
//    RunSummary lastRun; // נשמר גם ב-PlayerPrefs

//    // ===== לייף-סייקל =====
//    void Awake()
//    {
//        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//        Instance = this;
//        DontDestroyOnLoad(gameObject);
//        SceneManager.sceneLoaded += OnSceneLoaded;

//        LoadPersistent();
//    }

//    void OnDestroy()
//    {
//        if (Instance == this)
//            SceneManager.sceneLoaded -= OnSceneLoaded;
//    }

//    // מתחיל ריצה חדשה (נקרא מהתפריט כששחקן לוחץ "New Game")
//    public void BeginNewRun()
//    {
//        history.Clear();
//        current = null;
//        runActive = true;
//        runStart = Time.realtimeSinceStartup;
//    }

//    // נקרא אוטומטית על כל טעינת סצנה
//    void OnSceneLoaded(Scene scene, LoadSceneMode _)
//    {
//        var s = scene.name;

//        // מעבר דרך התפריט: אם עוד לא התחלנו ריצה ונכנסים לחדר - נתחיל.
//        if (s == menuSceneName || s == winSceneName || s == loseSceneName)
//        {
//            // אין חדר פעיל כאן
//            return;
//        }

//        if (!runActive) runActive = true; // במקרה שנכנסו ישר לחדר

//        StartRoom(s);
//    }

//    // ===== חדרים =====
//    public void StartRoom(string roomName)
//    {
//        // סגור חדר קודם (אם נשאר פתוח)
//        if (current != null && !current.completed && !current.failed)
//        {
//            current.duration = Time.realtimeSinceStartup - current.startAt;
//            // אין סיבה -> נחשב נטוש
//            current.failed = true;
//            current.failReason = "aborted";
//            UpdateAggregate(current);
//        }

//        current = new RoomEntry
//        {
//            roomName = roomName,
//            startAt = Time.realtimeSinceStartup
//        };
//        history.Add(current);
//        // Debug.Log($"[RunStats] StartRoom {roomName}");
//    }

//    public void CompleteCurrent(string reason = "exit")
//    {
//        if (current == null || current.completed || current.failed) return;

//        current.completed = true;
//        current.duration = Time.realtimeSinceStartup - current.startAt;
//        current.failReason = reason; // לא כישלון; רק אינפו
//        UpdateAggregate(current);
//        // Debug.Log($"[RunStats] Complete {current.roomName} {FormatTime(current.duration)}");
//    }

//    public void FailCurrent(string reason = "timeout")
//    {
//        if (current == null || current.completed || current.failed) return;

//        current.failed = true;
//        current.duration = Time.realtimeSinceStartup - current.startAt;
//        current.failReason = reason;
//        UpdateAggregate(current);
//        // Debug.Log($"[RunStats] Fail {current.roomName} ({reason}) {FormatTime(current.duration)}");
//    }

//    void UpdateAggregate(RoomEntry e)
//    {
//        if (!aggregate.TryGetValue(e.roomName, out var agg))
//        {
//            agg = new AggregateRoomStats { roomName = e.roomName };
//            aggregate[e.roomName] = agg;
//        }

//        agg.attempts++;
//        agg.totalTime += e.duration;

//        if (e.completed)
//        {
//            agg.wins++;
//            if (e.duration < agg.bestTime) agg.bestTime = e.duration;
//        }
//        else
//        {
//            agg.fails++;
//        }
//    }

//    // ===== סיום ריצה + סיכום =====
//    public RunSummary EndRunAndComputeSummary()
//    {
//        if (!runActive) return lastRun; // אין ריצה

//        // סגור חדר פתוח אם צריך
//        if (current != null && !current.completed && !current.failed)
//        {
//            current.failed = true;
//            current.failReason = "aborted";
//            current.duration = Time.realtimeSinceStartup - current.startAt;
//            UpdateAggregate(current);
//        }

//        var sum = new RunSummary();
//        sum.entries = new List<RoomEntry>(history);
//        sum.totalTime = history.Sum(r => r.duration);
//        sum.wins = history.Count(r => r.completed);
//        sum.fails = history.Count(r => r.failed);

//        var completedRooms = new HashSet<string>(history.Where(r => r.completed).Select(r => r.roomName));
//        sum.allRequiredCompleted = requiredRooms.All(r => completedRooms.Contains(r));

//        lastRun = sum;
//        SavePersistent();

//        runActive = false;
//        current = null;
//        return sum;
//    }

//    // ===== UI Builders =====
//    public string BuildAchievementsText(RunSummary sum = null)
//    {
//        sum ??= lastRun;
//        if (sum == null || sum.entries.Count == 0) return "No achievements yet.";
//        var sb = new StringBuilder();
//        foreach (var r in sum.entries.Where(e => e.completed))
//            sb.AppendLine($"• {r.roomName} ✓ ({FormatTime(r.duration)})");
//        return sb.Length == 0 ? "No achievements yet." : sb.ToString();
//    }

//    public string BuildChallengesText(RunSummary sum = null)
//    {
//        sum ??= lastRun;
//        if (sum == null || sum.entries.Count == 0) return "No failures yet.";
//        var fails = sum.entries.Where(e => e.failed).ToList();
//        if (fails.Count == 0) return "No failures yet.";
//        var sb = new StringBuilder();
//        foreach (var r in fails)
//            sb.AppendLine($"• {r.roomName} — {r.failReason} ({FormatTime(r.duration)})");
//        return sb.ToString();
//    }

//    public string BuildProgressText(RunSummary sum = null)
//    {
//        sum ??= lastRun;
//        if (sum == null || sum.entries.Count == 0) return "No rooms played yet.";
//        var sb = new StringBuilder();
//        foreach (var r in sum.entries)
//        {
//            string status = r.failed ? " (failed)" : (r.completed ? " (completed)" : "");
//            sb.AppendLine($"• {r.roomName} — {FormatTime(r.duration)}{status}");
//        }
//        sb.AppendLine($"Total: {FormatTime(sum.totalTime)}");
//        return sb.ToString();
//    }

//    public string BuildHeadline(RunSummary sum = null)
//    {
//        sum ??= lastRun;
//        if (sum == null) return "";
//        return sum.allRequiredCompleted ? "You Won! 🎉" : "Try Again 💪";
//    }

//    // ===== ניווט לסיכום =====
//    public void GoToSummaryScene()
//    {
//        var sum = EndRunAndComputeSummary();
//        SceneManager.LoadScene(sum.allRequiredCompleted ? winSceneName : loseSceneName);
//    }

//    // ===== התמדה (Last Run + Aggregate) =====
//    const string KEY_LASTRUN = "RunStats:lastRun";
//    const string KEY_AGG = "RunStats:aggregate";

//    [Serializable]
//    class PersistWrapper
//    {
//        public RunSummary lastRun;
//        public List<AggregateRoomStats> aggregate;
//    }

//    void SavePersistent()
//    {
//        var pw = new PersistWrapper
//        {
//            lastRun = lastRun,
//            aggregate = aggregate.Values.ToList()
//        };
//        var json = JsonUtility.ToJson(pw);
//        PlayerPrefs.SetString(KEY_LASTRUN, json);
//        PlayerPrefs.Save();
//    }

//    void LoadPersistent()
//    {
//        if (!PlayerPrefs.HasKey(KEY_LASTRUN)) return;
//        var json = PlayerPrefs.GetString(KEY_LASTRUN);
//        var pw = JsonUtility.FromJson<PersistWrapper>(json);
//        if (pw != null)
//        {
//            lastRun = pw.lastRun ?? new RunSummary();
//            aggregate.Clear();
//            if (pw.aggregate != null)
//                foreach (var a in pw.aggregate) aggregate[a.roomName] = a;
//        }
//    }

//    // ===== עזר =====
//    public static string FormatTime(float seconds)
//    {
//        if (seconds < 0f) seconds = 0f;
//        int m = Mathf.FloorToInt(seconds / 60f);
//        int s = Mathf.FloorToInt(seconds - m * 60);
//        return $"{m:00}:{s:00}";
//    }

//    // Getter נוח ל-UI בתפריט
//    public RunSummary GetLastRunSummary() => lastRun;
//}

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using UnityEngine;
//using UnityEngine.SceneManagement;

//public class RunStats : MonoBehaviour
//{
//    public static RunStats Instance { get; private set; }

//    [Header("Scene Names")]
//    public string menuSceneName = "menuScene";
//    public string winSceneName = "winRoom";
//    public string loseSceneName = "loseRoom";

//    [Header("Required Rooms (Doors Mode)")]
//    [Tooltip("רשימת חדרים שחייבים להשלים כדי להחשב ניצחון")]
//    public List<string> requiredRooms = new() { "AngerScene", "FearScene", "JoyScene 1", "SadnessScene" };

//    // ----- נתוני ריצה -----
//    [Serializable]
//    public class RoomEntry
//    {
//        public string roomName;
//        public float startAt;     // Time.realtimeSinceStartup בתחילת החדר
//        public float duration;    // נסגר בהצלחה/כישלון
//        public bool completed;    // true אם הושלם (יציאה מוצלחת)
//        public bool failed;       // true אם נכשל (למשל timeout)
//        public string failReason; // "timeout"/"fail"/"aborted"/...
//    }

//    public readonly List<RoomEntry> history = new();
//    RoomEntry current;
//    bool runActive;
//    float runStart;

//    // ===== לייף-סייקל =====
//    void Awake()
//    {
//        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//        Instance = this;
//        DontDestroyOnLoad(gameObject);
//    }

//    // ===== API עיקרי (Doors Mode) =====

//    /// קריאה מהתפריט בתחילת משחק חדש (ב־Doors Mode)
//    public void BeginNewRun()
//    {
//        history.Clear();
//        current = null;
//        runActive = true;
//        runStart = Time.realtimeSinceStartup;
//    }

//    /// קריאה אוטומטית מ־RoomRunManager בכל פעם שטוענים סצנה של חדר
//    public void StartRoom(string roomName)
//    {
//        if (!runActive) runActive = true;

//        // סגירת חדר קודם אם נשאר פתוח (נספר כנטוש/כישלון)
//        if (current != null && !current.completed && !current.failed)
//        {
//            current.failed = true;
//            current.failReason = "aborted";
//            current.duration = Time.realtimeSinceStartup - current.startAt;
//        }

//        current = new RoomEntry
//        {
//            roomName = roomName,
//            startAt = Time.realtimeSinceStartup
//        };
//        history.Add(current);
//        // Debug.Log($"[RunStats] StartRoom: {roomName}");
//    }

//    /// קריאה מהחדר כשהשחקן יצא בהצלחה (לפני חזרה לתפריט)
//    public void CompleteCurrent(string reason = "exit")
//    {
//        if (current == null || current.completed || current.failed) return;

//        current.completed = true;
//        current.duration = Time.realtimeSinceStartup - current.startAt;
//        current.failReason = reason; // אינפו בלבד
//        // Debug.Log($"[RunStats] Complete: {current.roomName} ({FormatTime(current.duration)})");
//    }

//    /// קריאה מהחדר כשנגמר הזמן/כישלון אחר
//    public void FailCurrent(string reason = "timeout")
//    {
//        if (current == null || current.completed || current.failed) return;

//        current.failed = true;
//        current.duration = Time.realtimeSinceStartup - current.startAt;
//        current.failReason = reason;
//        // Debug.Log($"[RunStats] Fail: {current.roomName} ({reason}) {FormatTime(current.duration)}");
//    }

//    /// האם כל החדרים הנדרשים הושלמו (לפחות פעם אחת) ואין אף כישלון?
//    public bool IsVictory()
//    {
//        if (history.Any(e => e.failed)) return false; // נגמר זמן/כישלון כלשהו

//        var completedRooms = new HashSet<string>(
//            history.Where(e => e.completed).Select(e => e.roomName)
//        );
//        return requiredRooms.All(r => completedRooms.Contains(r));
//    }

//    /// מעבר למסך סיכום – לפי הכלל: ניצחון רק אם כל החדרים הושלמו וללא כישלון
//    public void GoToSummaryScene()
//    {
//        // סגור חדר פתוח אם איכשהו נשאר
//        if (current != null && !current.completed && !current.failed)
//        {
//            current.failed = true;
//            current.failReason = "aborted";
//            current.duration = Time.realtimeSinceStartup - current.startAt;
//        }

//        bool win = IsVictory();
//        SceneManager.LoadScene(win ? winSceneName : loseSceneName);

//        runActive = false;
//        current = null;
//    }

//    // ===== Builders לטקסטים למסכי UI =====
//    public string BuildHeadline() => IsVictory() ? "You Won! 🎉" : "Try Again 💪";

//    public string BuildAchievementsText()
//    {
//        var sb = new StringBuilder();
//        foreach (var r in history.Where(e => e.completed))
//            sb.AppendLine($"• {r.roomName} ✓ ({FormatTime(r.duration)})");
//        if (sb.Length == 0) sb.Append("No achievements yet.");
//        return sb.ToString();
//    }

//    public string BuildChallengesText()
//    {
//        var fails = history.Where(e => e.failed).ToList();
//        if (fails.Count == 0) return "No failures yet.";

//        var sb = new StringBuilder();
//        foreach (var r in fails)
//            sb.AppendLine($"• {r.roomName} — {r.failReason} ({FormatTime(r.duration)})");
//        return sb.ToString();
//    }

//    public string BuildProgressText()
//    {
//        if (history.Count == 0) return "No rooms played yet.";

//        var sb = new StringBuilder();
//        foreach (var r in history)
//        {
//            string status = r.failed ? " (failed)" : (r.completed ? " (completed)" : "");
//            sb.AppendLine($"• {r.roomName} — {FormatTime(r.duration)}{status}");
//        }
//        float total = history.Sum(e => e.duration);
//        sb.AppendLine($"Total: {FormatTime(total)}");
//        return sb.ToString();
//    }

//    // ===== עזר =====
//    public static string FormatTime(float seconds)
//    {
//        if (seconds < 0f) seconds = 0f;
//        int m = Mathf.FloorToInt(seconds / 60f);
//        int s = Mathf.FloorToInt(seconds - m * 60);
//        return $"{m:00}:{s:00}";
//    }
//}

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using UnityEngine;
//using UnityEngine.SceneManagement;

//public class RunStats : MonoBehaviour
//{
//    public static RunStats Instance { get; private set; }

//    [Header("Scene Names")]
//    public string menuSceneName = "menuScene";
//    public string winSceneName = "winRoom";
//    public string loseSceneName = "loseRoom";

//    [Header("Required Rooms (Doors Mode)")]
//    [Tooltip("כל החדרים שחייבים להיות 'completed' כדי להחשב ניצחון")]
//    public List<string> requiredRooms = new() { "AngerScene", "FearScene", "JoyScene 1", "SadnessScene" };

//    // ===== מודל חדר =====
//    [Serializable]
//    public class RoomEntry
//    {
//        public string roomName;
//        public float startAt;     // Time.realtimeSinceStartup בתחילת החדר
//        public float duration;    // נסגר ב-complete/fail
//        public bool completed;    // אמת אם יציאה מוצלחת מהחדר
//        public bool failed;       // אמת אם כישלון (למשל timeout)
//        public string failReason; // "timeout" / "fail" / "aborted" / ...
//    }

//    // ===== מצב ריצה =====
//    public readonly List<RoomEntry> history = new();
//    RoomEntry current;
//    bool runActive;
//    float runStart;

//    // ===== לייף-סייקל =====
//    void Awake()
//    {
//        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//        Instance = this;
//        DontDestroyOnLoad(gameObject);
//    }

//    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
//    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

//    // מאזין כללי: כשה־SceneLoader טוען חדר – נתחיל רישום
//    void OnSceneLoaded(Scene scene, LoadSceneMode _)
//    {
//        string s = scene.name;
//        if (s == menuSceneName || s == winSceneName || s == loseSceneName) return;
//        StartRoom(s);
//    }

//    // ===== API לדלתות =====

//    /// קריאה מהתפריט (New Game בדלתות)
//    public void BeginNewRun()
//    {
//        history.Clear();
//        current = null;
//        runActive = true;
//        runStart = Time.realtimeSinceStartup;
//    }

//    /// מתחיל רישום לחדר חדש (נקרא אוטומטית ע"י OnSceneLoaded)
//    public void StartRoom(string roomName)
//    {
//        if (!runActive) runActive = true;

//        // סגור קודמים אם איכשהו נשאר פתוח → נספר כ-aborted (כישלון)
//        if (current != null && !current.completed && !current.failed)
//        {
//            current.failed = true;
//            current.failReason = "aborted";
//            current.duration = Time.realtimeSinceStartup - current.startAt;
//        }

//        current = new RoomEntry
//        {
//            roomName = roomName,
//            startAt = Time.realtimeSinceStartup
//        };
//        history.Add(current);
//        // Debug.Log($"[RunStats] StartRoom {roomName}");
//    }

//    /// קריאה מתוך החדר ביציאה מוצלחת (לפני חזרה לתפריט)
//    public void CompleteCurrent(string reason = "exit")
//    {
//        if (current == null || current.completed || current.failed) return;

//        current.completed = true;
//        current.duration = Time.realtimeSinceStartup - current.startAt;
//        current.failReason = reason; // אינפו בלבד
//        // Debug.Log($"[RunStats] Complete {current.roomName} ({FormatTime(current.duration)})");
//    }

//    /// קריאה מתוך החדר בכישלון (למשל כשהטיימר נגמר)
//    public void FailCurrent(string reason = "timeout")
//    {
//        if (current == null || current.completed || current.failed) return;

//        current.failed = true;
//        current.duration = Time.realtimeSinceStartup - current.startAt;
//        current.failReason = reason;
//        // Debug.Log($"[RunStats] Fail {current.roomName} ({reason}) {FormatTime(current.duration)}");
//    }

//    /// כלל ניצחון: כל החדרים הנדרשים הושלמו וללא אף failed בריצה
//    public bool IsVictory()
//    {
//        if (history.Any(e => e.failed)) return false; // היה כישלון כלשהו (למשל timeout)
//        var completedRooms = new HashSet<string>(history.Where(e => e.completed).Select(e => e.roomName));
//        return requiredRooms.All(r => completedRooms.Contains(r));
//    }

//    /// מעבר למסך סיכום (win/lose) לפי הכלל למעלה
//    public void GoToSummaryScene()
//    {
//        // סגור חדר פתוח אם נשאר
//        if (current != null && !current.completed && !current.failed)
//        {
//            current.failed = true;
//            current.failReason = "aborted";
//            current.duration = Time.realtimeSinceStartup - current.startAt;
//        }

//        bool win = IsVictory();
//        SceneManager.LoadScene(win ? winSceneName : loseSceneName);

//        runActive = false;
//        current = null;
//    }

//    // ===== Builders ל־UI =====
//    public string BuildHeadline() => IsVictory() ? "You Won! 🎉" : "Try Again 💪";

//    public string BuildAchievementsText()
//    {
//        var sb = new StringBuilder();
//        foreach (var r in history.Where(e => e.completed))
//            sb.AppendLine($"• {r.roomName} ✓ ({FormatTime(r.duration)})");
//        if (sb.Length == 0) sb.Append("No achievements yet.");
//        return sb.ToString();
//    }

//    public string BuildChallengesText()
//    {
//        var fails = history.Where(e => e.failed).ToList();
//        if (fails.Count == 0) return "No failures yet.";

//        var sb = new StringBuilder();
//        foreach (var r in fails)
//            sb.AppendLine($"• {r.roomName} — {r.failReason} ({FormatTime(r.duration)})");
//        return sb.ToString();
//    }

//    public string BuildProgressText()
//    {
//        if (history.Count == 0) return "No rooms played yet.";

//        var sb = new StringBuilder();
//        foreach (var r in history)
//        {
//            string status = r.failed ? " (failed)" : (r.completed ? " (completed)" : "");
//            sb.AppendLine($"• {r.roomName} — {FormatTime(r.duration)}{status}");
//        }
//        float total = history.Sum(e => e.duration);
//        sb.AppendLine($"Total: {FormatTime(total)}");
//        return sb.ToString();
//    }

//    // ===== עזר =====
//    public static string FormatTime(float seconds)
//    {
//        if (seconds < 0f) seconds = 0f;
//        int m = Mathf.FloorToInt(seconds / 60f);
//        int s = Mathf.FloorToInt(seconds - m * 60);
//        return $"{m:00}:{s:00}";
//    }
//}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RunStats : MonoBehaviour
{
    public static RunStats Instance { get; private set; }

    [Header("Scene Names")]
    public string menuSceneName = "menuScene";
    public string winSceneName = "winRoom";
    public string loseSceneName = "loseRoom";

    [Header("Tracked Rooms (Doors Mode)")]
    [Tooltip("רק הסצנות ברשימה הזאת ייספרו כ'חדרים' לריצה הנוכחית")]
    public List<string> trackedRooms = new() { "AngerScene", "FearScene", "JoyScene 1", "SadnessScene" };

    // ===== מודל חדר =====
    [Serializable]
    public class RoomEntry
    {
        public string roomName;
        public float startAt;     // Time.realtimeSinceStartup בתחילת החדר
        public float duration;    // נסגר ב-complete/fail
        public bool completed;    // יציאה מוצלחת מהחדר
        public bool failed;       // כישלון (למשל timeout)
        public string failReason; // "timeout"/"fail"/"aborted"/...
    }

    // ===== מצב ריצה =====
    public readonly List<RoomEntry> history = new();
    RoomEntry current;
    bool runActive;
    float runStart;

    // ===== לייף-סייקל =====
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    // מאזין כללי: כשנטענת סצנה — אם היא ברשימת החדרים, נפתח רשומה
    void OnSceneLoaded(Scene scene, LoadSceneMode _)
    {
        string s = scene.name;
        if (!IsTrackedRoom(s)) return;             // מתעלם מתפריט/ניצחון/הפסד וכל שאינם 4 החדרים
        StartRoom(s);
    }

    bool IsTrackedRoom(string sceneName)
    {
        // בדיקה case-insensitive מול הרשימה
        return trackedRooms.Any(r => string.Equals(r, sceneName, StringComparison.OrdinalIgnoreCase));
    }

    // ===== API לדלתות =====

    /// קריאה מהתפריט (New Game)
    public void BeginNewRun()
    {
        history.Clear();
        current = null;
        runActive = true;
        runStart = Time.realtimeSinceStartup;
    }

    // איפשהו בתוך המחלקה RunStats
    public void ResetAll()
    {
        history.Clear();
        current = null;
        runActive = false;   // לא חובה, אבל טוב לאתחל
        runStart = 0f;
    }


    /// מתחיל רישום לחדר חדש (נקרא אוטומטית ע"י OnSceneLoaded)
    public void StartRoom(string roomName)
    {
        if (!runActive) runActive = true;

        // אם איכשהו עברו חדר בלי לסיים — נספר כ-aborted (כישלון)
        if (current != null && !current.completed && !current.failed)
        {
            current.failed = true;
            current.failReason = "aborted";
            current.duration = Time.realtimeSinceStartup - current.startAt;
            // לא קוראים לסיכום כאן — נגיע אליו רק כשכל 4 הוסדרו
        }

        current = new RoomEntry
        {
            roomName = roomName,
            startAt = Time.realtimeSinceStartup
        };
        history.Add(current);
        // Debug.Log($"[RunStats] StartRoom {roomName}");
    }

    /// קריאה מתוך החדר ביציאה מוצלחת
    public void CompleteCurrent(string reason = "exit")
    {
        if (current == null || current.completed || current.failed) return;
        current.completed = true;
        current.duration = Time.realtimeSinceStartup - current.startAt;
        current.failReason = reason; // אינפו בלבד

        //CheckRunFinished();
    }

    /// קריאה מתוך החדר בכישלון (למשל כשהטיימר נגמר)
    public void FailCurrent(string reason = "timeout")
    {
        if (current == null || current.completed || current.failed) return;
        current.failed = true;
        current.duration = Time.realtimeSinceStartup - current.startAt;
        current.failReason = reason;

        //CheckRunFinished();
    }

    /// בודק אם כל 4 החדרים ("trackedRooms") קיבלו סטטוס (completed/failed) — ואם כן, טוען מסך סיכום
    void CheckRunFinished()
    {
        // לוקחים רק רשומות של חדרים ברשימת המעקב, ורק אם נסגרו (completed/failed)
        var resolved = new HashSet<string>(
            history.Where(e => IsTrackedRoom(e.roomName) && (e.completed || e.failed))
                   .Select(e => e.roomName),
            StringComparer.OrdinalIgnoreCase);

        if (resolved.Count >= trackedRooms.Count)
        {
            GoToSummaryScene();
        }
    }

    public bool IsRunFinished()
    {
        var resolved = new HashSet<string>(
            history.Where(e => IsTrackedRoom(e.roomName) && (e.completed || e.failed))
                   .Select(e => e.roomName),
            StringComparer.OrdinalIgnoreCase);
        return resolved.Count >= trackedRooms.Count;
    }

    /// כלל הניצחון: אין אף failed, ו-כל ה-trackedRooms הושלמו (completed)
    public bool IsVictory()
    {
        if (history.Any(e => IsTrackedRoom(e.roomName) && e.failed)) return false;
        var completed = new HashSet<string>(
            history.Where(e => e.completed && IsTrackedRoom(e.roomName))
                   .Select(e => e.roomName), StringComparer.OrdinalIgnoreCase);
        return trackedRooms.All(r => completed.Contains(r));
    }

    /// מעבר למסך סיכום (עם פייד אם זמין)
    public void GoToSummaryScene()
    {
        // סגור חדר פתוח אם נשאר
        if (current != null && !current.completed && !current.failed)
        {
            current.failed = true;
            current.failReason = "aborted";
            current.duration = Time.realtimeSinceStartup - current.startAt;
        }

        bool win = IsVictory();

        // טעינה עם פייד אם יש ScreenFader בסצנה הנוכחית
        var fader = FindObjectOfType<ScreenFader>();
        if (fader != null) fader.FadeToScene(win ? winSceneName : loseSceneName);
        else SceneManager.LoadScene(win ? winSceneName : loseSceneName);

        runActive = false;
        current = null;
    }

    // ===== Builders ל־UI =====
    public string BuildHeadline() => IsVictory() ? "You Won! 🎉" : "Try Again 💪";

    public string BuildAchievementsText()
    {
        var sb = new StringBuilder();
        foreach (var r in history.Where(x => x.completed && IsTrackedRoom(x.roomName)))
            sb.AppendLine($"• {r.roomName} ✓ ({FormatTime(r.duration)})"); // <-- r.duration
        if (sb.Length == 0) sb.Append("No achievements yet.");
        return sb.ToString();
    }


    public string BuildChallengesText()
    {
        var fails = history.Where(e => e.failed && IsTrackedRoom(e.roomName)).ToList();
        if (fails.Count == 0) return "No failures yet.";

        var sb = new StringBuilder();
        foreach (var r in fails)
            sb.AppendLine($"• {r.roomName} — {r.failReason} ({FormatTime(r.duration)})");
        return sb.ToString();
    }

    public string BuildProgressText()
    {
        var played = history.Where(e => IsTrackedRoom(e.roomName)).ToList();
        if (played.Count == 0) return "No rooms played yet.";

        var sb = new StringBuilder();
        foreach (var r in played)
        {
            string status = r.failed ? " (failed)" : (r.completed ? " (completed)" : "");
            sb.AppendLine($"• {r.roomName} — {FormatTime(r.duration)}{status}");
        }
        float total = played.Sum(e => e.duration);
        sb.AppendLine($"Total: {FormatTime(total)}");
        return sb.ToString();
    }

    // ===== עזר =====
    public static string FormatTime(float seconds)
    {
        if (seconds < 0f) seconds = 0f;
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds - m * 60);
        return $"{m:00}:{s:00}";
    }
}