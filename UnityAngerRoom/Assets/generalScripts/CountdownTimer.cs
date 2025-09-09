using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;           // אם משתמש ב-TMP
using UnityEngine.UI; // אם משתמש ב-Text רגיל

public class CountdownTimer : MonoBehaviour
{
    [Header("Timer")]
    [Tooltip("משך ההתחלה בשניות (למשל 300 = 5 דקות)")]
    public float startSeconds = 300f;
    public bool autoStart = true;

    [Tooltip("לאיזו סצינה לחזור כשהזמן נגמר (למשל: menu / menuscene)")]
    public string sceneToLoadOnEnd = "menu";

    [Header("UI – תצוגת השעון (אחד מהשדות)")]
    public TextMeshProUGUI tmpText;  // גרור TMP Text אם יש
    public Text uiText;              // או Text רגיל

    [Header("UI – מה להסתיר בסיום")]
    [Tooltip("קנבס/אובייקט של הפרוגרס בר (יוסתר בסיום הזמן)")]
    public GameObject progressBarCanvas;

    [Tooltip("האובייקט שמציג את השעון (יוסתר בסיום הזמן)")]
    public GameObject timerUI;

    [Header("Game Over")]
    [Tooltip("קנבס/טקסט עם GAME OVER שיוצג בסיום הזמן")]
    public GameObject gameOverUI;

    [Tooltip("כמה שניות להראות את GAME OVER לפני מעבר סצינה")]
    public float gameOverDelay = 2f;

    [Header("Game Over Audio")]
    [Tooltip("AudioSource לניגון הסאונד (אופציונלי). אם ריק – ינוגן 2D אוטומטי.")]
    public AudioSource gameOverSource;
    [Tooltip("קליפ 'עצוב' שיושמע כש-GAME OVER מופיע")]
    public AudioClip gameOverClip;
    [Range(0f, 1f)] public float gameOverVolume = 1f;

    [Header("Last-Seconds Warnings")]
    [Tooltip("מאיזה שניות להתחיל אזהרות (הבהוב/פולס/ביפ)")]
    public int warnAtSeconds = 10;

    [Tooltip("האם להבהב צבע/שקיפות בשניות האחרונות")]
    public bool flashInLastSeconds = true;

    [Tooltip("מרווח הבהוב (שניות)")]
    [Range(0.05f, 1.0f)] public float flashInterval = 0.25f;

    [Tooltip("צבע טקסט רגיל")]
    public Color normalColor = Color.white;

    [Tooltip("צבע טקסט בזמן הבהוב")]
    public Color flashColor = new Color(1f, 0.25f, 0.25f, 1f);

    [Tooltip("האם לבצע 'פולס' סקייל (נשימה) בשניות האחרונות")]
    public bool pulseScaleInLastSeconds = true;

    [Tooltip("אמפליטודה של הפולס (למשל 0.06 = ±6%)")]
    [Range(0f, 0.3f)] public float pulseAmplitude = 0.08f;

    [Tooltip("תדירות הפולס (הרץ)")]
    [Range(0.1f, 5f)] public float pulseFrequency = 2f;

    [Header("Audio Beep")]
    [Tooltip("להשמיע ביפ בכל ירידת שנייה בשניות האחרונות")]
    public bool beepInLastSeconds = true;

    [Tooltip("AudioSource לניגון הביפים (רצוי 2D). אם ריק נשתמש ב-PlayClipAtPoint")]
    public AudioSource beepSource;

    [Tooltip("קליפ קצר לצפצוף")]
    public AudioClip beepClip;

    [Range(0f, 1f)] public float beepVolume = 0.9f;

    // --- מצב פנימי ---
    float remaining;
    bool running;
    int lastWholeSeconds = -1;
    float flashTimer = 0f;
    Vector3 baseScale;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    void Start()
    {
        remaining = Mathf.Max(0f, startSeconds);
        running   = autoStart;

        if (gameOverUI) gameOverUI.SetActive(false); // בהתחלה כבוי
        ApplyTextColor(normalColor);
        UpdateClockText();
        lastWholeSeconds = Mathf.CeilToInt(remaining);
    }

    void Update()
    {
        if (!running) return;

        remaining -= Time.deltaTime;
        if (remaining <= 0f)
        {
            remaining = 0f;
            running   = false;

            // נקה אפקטים והחזר צבע/סקייל
            flashTimer = 0f;
            ApplyTextColor(normalColor);
            transform.localScale = baseScale;

            UpdateClockText();
            OnTimesUp();
            return;
        }

        // אפקטים לשניות האחרונות
        HandleLastSecondsFX();

        // ביפ פעם בכל ירידת שנייה + עדכון טקסט
        int whole = Mathf.CeilToInt(remaining);
        if (whole != lastWholeSeconds)
        {
            lastWholeSeconds = whole;

            if (beepInLastSeconds && whole <= warnAtSeconds && whole > 0)
                BeepOnce();

            UpdateClockText();
        }
    }

    // --- אפקטים של שניות אחרונות ---
    void HandleLastSecondsFX()
    {
        if (remaining > warnAtSeconds)
        {
            // מחוץ לטווח אזהרה – החזר למצבי ברירת מחדל
            flashTimer = 0f;
            ApplyTextColor(normalColor);
            transform.localScale = baseScale;
            return;
        }

        float dt = Time.deltaTime;

        // Flash (הבהוב צבע/אלפא)
        if (flashInLastSeconds)
        {
            flashTimer += dt;
            if (flashTimer >= flashInterval)
            {
                flashTimer = 0f;
                // טוגל בין normalColor ל-flashColor
                var current = GetCurrentTextColor();
                bool currentlyFlash = ApproximatelySameColor(current, flashColor);
                ApplyTextColor(currentlyFlash ? normalColor : flashColor);
            }
        }

        // Pulse Scale (נשימה)
        if (pulseScaleInLastSeconds)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 2f * Mathf.PI * pulseFrequency) * pulseAmplitude;
            transform.localScale = baseScale * pulse;
        }
    }

    void BeepOnce()
    {
        if (!beepInLastSeconds || beepClip == null) return;

        if (beepSource != null)
            beepSource.PlayOneShot(beepClip, beepVolume);
        else
            AudioSource.PlayClipAtPoint(beepClip, transform.position, beepVolume);
    }

    // --- UI ועדכון טקסט ---
    void UpdateClockText()
    {
        int t = Mathf.CeilToInt(remaining);
        int m = t / 60;
        int s = t % 60;
        string txt = $"{m:00}:{s:00}";

        if (tmpText) tmpText.text = txt;
        if (uiText)  uiText.text  = txt;
    }

    void ApplyTextColor(Color c)
    {
        if (tmpText) tmpText.color = c;
        if (uiText)  uiText.color  = c;
    }

    Color GetCurrentTextColor()
    {
        if (tmpText) return tmpText.color;
        if (uiText)  return uiText.color;
        return normalColor;
    }

    static bool ApproximatelySameColor(Color a, Color b)
    {
        const float eps = 0.01f;
        return Mathf.Abs(a.r - b.r) < eps &&
               Mathf.Abs(a.g - b.g) < eps &&
               Mathf.Abs(a.b - b.b) < eps &&
               Mathf.Abs(a.a - b.a) < eps;
    }

    // --- סוף זמן ---
    void OnTimesUp()
    {
        // 1) הסתר ProgressBar ושעון
        if (progressBarCanvas) progressBarCanvas.SetActive(false);
        if (timerUI)           timerUI.SetActive(false);

        // 2) נגן סאונד Game Over (ודא שאודיו לא מושתק)
        AudioListener.pause = false;
        if (gameOverClip != null)
        {
            if (gameOverSource != null)
            {
                gameOverSource.PlayOneShot(gameOverClip, gameOverVolume);
            }
            else
            {
                StartCoroutine(PlayOneShot2D(gameOverClip, gameOverVolume));
            }
        }

        // 3) הצג Game Over
        if (gameOverUI)
			{ 			
				gameOverUI.SetActive(true);
				RunStats.Instance.FailCurrent("exit");
			}

        // 4) התחל מעבר אחרי דיליי (לא תלוי timeScale)
        StartCoroutine(LoadAfterDelay());
    }

    IEnumerator PlayOneShot2D(AudioClip clip, float vol)
    {
        var go  = new GameObject("OneShot2D(GameOver)");
        var src = go.AddComponent<AudioSource>();
        src.spatialBlend = 0f;     // 2D
        src.playOnAwake  = false;
        src.clip   = clip;
        src.volume = vol;
        src.Play();

        yield return new WaitForSecondsRealtime(clip.length);
        Destroy(go);
    }

    IEnumerator LoadAfterDelay()
    {
        yield return new WaitForSecondsRealtime(gameOverDelay);

        if (RunStats.Instance != null && RunStats.Instance.IsRunFinished())
            RunStats.Instance.GoToSummaryScene();
        else
            RoomRunManager.Instance?.LoadMenu();

        //if (ScreenFader.Instance != null)
        //{


        //    ////ScreenFader.Instance.FadeToScene("loseRoom");
        //}
        //else
        //{
        //    Time.timeScale = 1f;
        //    AudioListener.pause = false;
        //    SceneManager.LoadScene("loseRoom");
        //}
    }

    // ===== API שימושי אם תרצה לשלוט ידנית =====
    public void StartTimer()
    {
        if (remaining <= 0f) remaining = startSeconds;
        running = true;
        lastWholeSeconds = Mathf.CeilToInt(remaining);
        ApplyTextColor(normalColor);
        transform.localScale = baseScale;
        UpdateClockText();
    }

    public void PauseTimer()  => running = false;
    public void ResumeTimer() => running = remaining > 0f;

    public void ResetTimer()
    {
        remaining = startSeconds;
        running = false;
        lastWholeSeconds = Mathf.CeilToInt(remaining);
        flashTimer = 0f;

        ApplyTextColor(normalColor);
        transform.localScale = baseScale;
        UpdateClockText();

        // להחזיר UI לברירת מחדל אם נשארים באותה סצינה
        if (progressBarCanvas) progressBarCanvas.SetActive(true);
        if (timerUI)           timerUI.SetActive(true);
        if (gameOverUI)        gameOverUI.SetActive(false);
    }
}