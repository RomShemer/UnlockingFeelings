using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("Auto-pick rooms from Build Settings")]
    [Tooltip("שמות סצינות שלא ייכנסו להגרלה (למשל תפריטים)")]
    public List<string> excludeNames = new List<string> { "menuScene", "menu", "mainmenu" };

    [Tooltip("אל תגריל את הסצינה הנוכחית")]
    public bool excludeCurrentScene = true;

    [Tooltip("אל תחזור על אותו חדר פעמיים ברצף")]
    public bool avoidRepeatLastPick = true;

    const string LastPickKey = "last_random_scene";

    // קריאה מהכפתור: OnClick -> SceneLoader.LoadRandomScene()
    public void LoadRandomScene()
    {
        var pool = GetCandidateScenes();

        if (pool.Count == 0)
        {
            Debug.LogError("[SceneLoader] No candidate scenes. Check Build Settings / excludeNames.");
            return;
        }

        // הימנעות מחזרה רצופה
        if (avoidRepeatLastPick && PlayerPrefs.HasKey(LastPickKey) && pool.Count > 1)
        {
            string last = PlayerPrefs.GetString(LastPickKey);
            pool.Remove(last);
        }

        string chosen = pool[Random.Range(0, pool.Count)];

        if (avoidRepeatLastPick)
            PlayerPrefs.SetString(LastPickKey, chosen);

        // טעינה עם פייד אם קיים
        if (ScreenFader.Instance != null)
            ScreenFader.Instance.FadeToScene(chosen);
        else
            SceneManager.LoadScene(chosen);
    }

    // אופציונלי: טעינה ישירה בשם סצינה
    public void LoadSceneByName(string sceneName)
    {
        if (ScreenFader.Instance != null)
            ScreenFader.Instance.FadeToScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    // בונה רשימת מועמדים מתוך Build Settings
    List<string> GetCandidateScenes()
    {
        var list = new List<string>();
        int count = SceneManager.sceneCountInBuildSettings;

        string current = SceneManager.GetActiveScene().name;

        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);                   // e.g. Assets/Scenes/joyroom1.unity
            string name = Path.GetFileNameWithoutExtension(path);                     // e.g. joyroom1
            if (string.IsNullOrWhiteSpace(name)) continue;

            // החרגות
            if (excludeNames.Any(x => string.Equals(x, name)))
                continue;

            if (excludeCurrentScene && name == current)
                continue;

            list.Add(name);
        }

        // במקרה חריג – הסר כפילויות
        return list.Distinct().ToList();
    }
}
