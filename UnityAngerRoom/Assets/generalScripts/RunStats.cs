using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RunStats : MonoBehaviour
{
    public static RunStats Instance { get; private set; }

    [System.Serializable]
    public class RoomEntry
    {
        public string roomName;
        public float startAt;      // realtimeSinceStartup בתחילת החדר
        public float duration;     // משך עד סיום (מילוי ב-complete/fail)
        public bool completed;     // true אם יצא דרך RoomExit
        public bool failed;        // true אם עבר ל-loseRoom (למשל timeout)
        public string failReason;  // "timeout" / "exit" / סיבה אחרת לאבחון
    }

    // היסטוריה של כל החדרים שנשוחקו בריצה
    public readonly List<RoomEntry> history = new List<RoomEntry>();
    RoomEntry current;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // רישום מאזין לטעינת סצנות
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // נפתח רשומה חדשה בכל טעינת סצנת "חדר" (לא תפריט/ניצחון/הפסד)
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string s = scene.name;
        if (s == "menuScene" || s == "winRoom" || s == "loseRoom") return;

        StartRoom(s);
    }

    // --- API ---

    public void StartRoom(string roomName)
    {
        // סגירה בטוחה של חדר קודם אם משום מה נשאר פתוח
        if (current != null && !current.completed && !current.failed)
        {
            current.duration = Time.realtimeSinceStartup - current.startAt;
        }

        current = new RoomEntry
        {
            roomName = roomName,
            startAt  = Time.realtimeSinceStartup
        };
        history.Add(current);
        // Debug.Log($"[RunStats] StartRoom: {roomName}");
    }

    public void CompleteCurrent(string reason = "exit")
    {
        if (current == null) return;
        if (current.completed || current.failed) return;

        current.completed = true;
        current.duration  = Time.realtimeSinceStartup - current.startAt;
        current.failReason = reason; // למעקב; לא נחשב כישלון
        // Debug.Log($"[RunStats] Complete: {current.roomName} ({FormatTime(current.duration)})");
    }

    public void FailCurrent(string reason = "timeout")
    {
        if (current == null) return;
        if (current.completed || current.failed) return;

        current.failed   = true;
        current.duration = Time.realtimeSinceStartup - current.startAt;
        current.failReason = reason;
        // Debug.Log($"[RunStats] Fail: {current.roomName} ({reason}) {FormatTime(current.duration)}");
    }

    public void ResetAll()
    {
        history.Clear();
        current = null;
        // Debug.Log("[RunStats] ResetAll");
    }

    // --- Builders לטקסטים למסכי UI ---

    // Achievements: כל החדרים שהושלמו
    public string BuildAchievementsText()
    {
        var sb = new StringBuilder();
        foreach (var r in history)
            if (r.completed)
                sb.AppendLine($"• {r.roomName} ✓");
        if (sb.Length == 0) sb.Append("No achievements yet.");
        return sb.ToString();
    }

    // Challenges: כל החדרים שנכשלו (תיקון: לא רק הראשון)
    public string BuildChallengesText()
    {
        var failed = history.FindAll(r => r.failed);
        if (failed.Count == 0) return "No failures yet.";

        var sb = new StringBuilder();
        foreach (var r in failed)
            sb.AppendLine($"• Failed in {r.roomName} ({r.failReason})");
        return sb.ToString();
    }

    // Progress: זמן בכל חדר (גם אם נכשל)
    public string BuildProgressText()
    {
        if (history.Count == 0) return "No rooms played yet.";

        var sb = new StringBuilder();
        foreach (var r in history)
        {
            string status = r.failed ? " (failed)" : (r.completed ? " (completed)" : "");
            sb.AppendLine($"• {r.roomName} — {FormatTime(r.duration)}{status}");
        }
        return sb.ToString();
    }

    public static string FormatTime(float seconds)
    {
        if (seconds < 0f) seconds = 0f;
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds - m * 60);
        return $"{m:00}:{s:00}";
    }
}
