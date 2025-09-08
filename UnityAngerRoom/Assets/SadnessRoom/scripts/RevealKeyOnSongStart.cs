using UnityEngine;
using System.Collections;

public class RevealKeyOnSongStart : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public AudioSource source;      // ה-AudioSource שמנגן את השיר המלא
    public AudioClip successClip;   // הקליפ של Exile (השיר שמנוגן אחרי הרצף)
    public GameObject key;          // האובייקט Door_key (כבוי בהתחלה)
    public float delay = 0f;        // השהייה לפני הופעה (אופציונלי)

    bool revealed = false;

    void Awake()
    {
        if (key != null) key.SetActive(false); // ביטחון כפול שהוא כבוי בהתחלה
    }

    void Update()
    {
        if (revealed || source == null) return;

        // נוודא שהתחיל להתנגן קליפ ההצלחה (או כל קליפ אם לא מילאת successClip)
        if (source.isPlaying && (successClip == null || source.clip == successClip))
        {
            revealed = true;
            StartCoroutine(ShowKeyAfterDelay());
        }
    }

    IEnumerator ShowKeyAfterDelay()
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        key.SetActive(true);

        // ביטחון שהפיזיקה “מתעוררת”
        var rb = key.GetComponent<Rigidbody>();
        if (rb) rb.WakeUp();
    }
}