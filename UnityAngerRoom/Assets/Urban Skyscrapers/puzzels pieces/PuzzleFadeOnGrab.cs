//using UnityEngine;
//using UnityEngine.Audio;
//using System.Collections;

//public class PuzzleFadeOnGrab : MonoBehaviour
//{
//    public float fadeDuration = 1.5f; // שניות
//    private Material mat;
//    private Color startColor;

//    [Header("Audio (optional)")]
//    public AudioMixerGroup sfxGroup;   // גררי לכאן את Effects מהמיקסר
//    public AudioClip vanishClip;       // סאונד ההיעלמות/התמוססות
//    public AudioClip saveCueClip;      // אופציונלי: "נשמר" קטן בסוף
//    public bool play2D = false;        // true = 2D; false = 3D בנקודת החלק
//    public float sfxMinDistance = 1.5f;
//    public float sfxMaxDistance = 12f;

//    void Start()
//    {
//        mat = GetComponent<Renderer>().material;
//        startColor = mat.color;
//    }

//    public void StartFadeOut()
//    {
//        Debug.Log("start fade out");
//        StartCoroutine(FadeOutAndDisable());
//    }

//    private System.Collections.IEnumerator FadeOutAndDisable()
//    {
//        Debug.Log("enter FadeOutAndDisable");
//        float timer = 0f;

//        while (timer < fadeDuration)
//        {
//            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
//            mat.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
//            timer += Time.deltaTime;
//            yield return null;
//        }

//        mat.color = new Color(startColor.r, startColor.g, startColor.b, 0f);

//        Debug.Log("before set active false");

//        // אופציונלי: להעביר למקום אחר / להפעיל משהו
//        gameObject.SetActive(false);
//        Debug.Log("after set active false");

//    }
//}

//using UnityEngine;
//using UnityEngine.Audio;
//using System.Collections; // ← חדש (בשביל IEnumerator בלי שמות מלאים)

//public class PuzzleFadeOnGrab : MonoBehaviour
//{
//    public float fadeDuration = 1.5f; // שניות
//    private Material mat;
//    private Color startColor;

//    [Header("Audio (optional)")]
//    public AudioMixerGroup sfxGroup;   // גררי לכאן את Effects מהמיקסר
//    public AudioClip vanishClip;       // סאונד ההיעלמות/התמוססות
//    public AudioClip saveCueClip;      // אופציונלי: "נשמר" קטן בסוף
//    public bool play2D = false;        // true = 2D; false = 3D בנקודת החלק
//    public float sfxMinDistance = 1.5f;
//    public float sfxMaxDistance = 12f;

//    [Header("Levels")]                 // ← חדש
//    [Range(0f, 1f)] public float vanishVolume = 1f;   // ← חדש (ווליום ל-whoosh)
//    [Range(0f, 1f)] public float saveCueVolume = 0.3f; // ← חדש (ווליום ל-ding)

//    void Start()
//    {
//        mat = GetComponent<Renderer>().material;
//        startColor = mat.color;
//    }

//    public void StartFadeOut()
//    {
//        Debug.Log("start fade out");

//        // ===== אודיו: התחלת סאונד ההיעלמות (ללא קשר ללוגיקת ה-FADE) =====
//        if (vanishClip != null)
//            StartCoroutine(PlayVanishAudioAt(transform.position));
//        // ==================================================================

//        StartCoroutine(FadeOutAndDisable());
//    }

//    private IEnumerator FadeOutAndDisable()
//    {
//        Debug.Log("enter FadeOutAndDisable");
//        float timer = 0f;

//        while (timer < fadeDuration)
//        {
//            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
//            mat.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
//            timer += Time.deltaTime;
//            yield return null;
//        }

//        mat.color = new Color(startColor.r, startColor.g, startColor.b, 0f);

//        Debug.Log("before set active false");

//        // אופציונלי: “נשמר” קטן בסוף (לא קשור ל-FADE)
//        if (saveCueClip != null)
//        {
//            // אם play2D=true נשמיע ליד המצלמה; אחרת בנקודת החלק
//            Vector3 pos = (play2D && Camera.main) ? Camera.main.transform.position : transform.position;
//            AudioSource.PlayClipAtPoint(saveCueClip, pos, 0.6f);
//        }

//        // אופציונלי: להעביר למקום אחר / להפעיל משהו
//        gameObject.SetActive(false);
//        Debug.Log("after set active false");
//    }

//    // ===== העזר: מנגן את סאונד ההיעלמות כמקור זמני ונקי =====
//    private IEnumerator PlayVanishAudioAt(Vector3 worldPos)
//    {
//        GameObject go = new GameObject("VanishSFX");
//        go.transform.position = worldPos + Vector3.up * 0.05f;

//        var src = go.AddComponent<AudioSource>();
//        src.outputAudioMixerGroup = sfxGroup; // חבר למיקסר Effects אם קיים
//        src.clip = vanishClip;
//        src.playOnAwake = false;
//        src.loop = false;
//        src.dopplerLevel = 0f;

//        if (play2D)
//        {
//            src.spatialBlend = 0f; // 2D
//        }
//        else
//        {
//            src.spatialBlend = 1f; // 3D
//            src.rolloffMode = AudioRolloffMode.Logarithmic;
//            src.minDistance = sfxMinDistance;
//            src.maxDistance = sfxMaxDistance;
//        }

//        src.volume = 1f; // בלי קשר לפייד הוויזואלי
//        src.Play();

//        yield return new WaitForSeconds(vanishClip.length + 0.1f);
//        Destroy(go);
//    }
//    // ======================================================================
//}

using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

public class PuzzleFadeOnGrab : MonoBehaviour
{
    public float fadeDuration = 1.5f; // שניות
    private Material mat;
    private Color startColor;

    [Header("Audio (optional)")]
    public AudioMixerGroup sfxGroup;   // גררי לכאן את Effects מהמיקסר
    public AudioClip vanishClip;       // סאונד ההיעלמות/התמוססות
    public AudioClip saveCueClip;      // אופציונלי: "נשמר" קטן בסוף
    public bool play2D = false;        // true = 2D; false = 3D בנקודת החלק
    public float sfxMinDistance = 1.5f;
    public float sfxMaxDistance = 12f;

    [Header("Levels")]                 // ADDED
    [Range(0f, 1f)] public float vanishVolume = 1f;   // ADDED – ווליום ל-whoosh
    [Range(0f, 1f)] public float saveCueVolume = 0.3f; // ADDED – ווליום ל-ding

    [Header("Collect Manager")]
    public CollectPuzzleManager collectManager;
    private bool reported;          // שלא נספור פעמיים
    private bool isFading;          // הגנה מפייד כפול

    void Start()
    {
        mat = GetComponent<Renderer>().material;
        startColor = mat.color;

        if (!collectManager) collectManager = FindObjectOfType<CollectPuzzleManager>();
    }

    public void StartFadeOut()
    {
        Debug.Log("start fade out");

        if (vanishClip != null)
            StartCoroutine(PlayVanishAudioAt(transform.position));

        StartCoroutine(FadeOutAndDisable());
    }

    private IEnumerator FadeOutAndDisable()
    {
        Debug.Log("enter FadeOutAndDisable");
        float timer = 0f;

        while (timer < fadeDuration)
        {
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            mat.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            timer += Time.deltaTime;
            yield return null;
        }

        mat.color = new Color(startColor.r, startColor.g, startColor.b, 0f);

        Debug.Log("before set active false");

        // "נשמר" קטן בסוף (לא קשור ל-FADE)
        if (saveCueClip != null)
        {
            Vector3 pos = (play2D && Camera.main) ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(saveCueClip, pos, saveCueVolume); // CHANGED (היה 0.6f)
        }

        gameObject.SetActive(false);

        if (!reported)
        {
            collectManager?.ReportConnection();
            reported = true;
        }
        Debug.Log("after set active false");
    }

    // מנגן את סאונד ההיעלמות כמקור זמני
    private IEnumerator PlayVanishAudioAt(Vector3 worldPos)
    {
        GameObject go = new GameObject("VanishSFX");
        go.transform.position = worldPos + Vector3.up * 0.05f;

        var src = go.AddComponent<AudioSource>();
        src.outputAudioMixerGroup = sfxGroup;
        src.clip = vanishClip;
        src.playOnAwake = false;
        src.loop = false;
        src.dopplerLevel = 0f;

        if (play2D)
        {
            src.spatialBlend = 0f; // 2D – חזק ויציב
        }
        else
        {
            src.spatialBlend = 1f; // 3D – תלוי מרחק
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = sfxMinDistance;   // נסי 0.8–1.0 אם חלש
            src.maxDistance = sfxMaxDistance;   // נסי 18–25 אם צריך
        }

        src.volume = vanishVolume;              // ADDED – משתמשים בווליום שהוספת
        src.Play();

        yield return new WaitForSeconds(vanishClip.length + 0.1f);

        Destroy(go);
    }
}


