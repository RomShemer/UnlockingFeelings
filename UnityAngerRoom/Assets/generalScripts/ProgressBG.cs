// ProgressBarUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.VFX;
using System.Collections;

public class ProgressBG : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] Image fill;
    [SerializeField] TextMeshProUGUI label;

    [Header("Sparkle VFX")]
    [SerializeField] VisualEffect sparklePrefab;         // Prefab של הניצוצות
    [SerializeField] Vector3 sparkleOffset = new Vector3(0, 0.2f, 0);
    [SerializeField] float sparkleLifetime = 2f;
    [SerializeField] string playEventName = "OnPlay";    // אופציונלי

    [Header("Sounds")]
    [SerializeField] AudioSource sfxSource;              // טיק
    [SerializeField] AudioClip tickClip;
    [Range(0f, 1f)] [SerializeField] float tickVolume = 0.9f;
    [SerializeField] Vector2 tickPitchRange = new Vector2(0.95f, 1.05f);

    [SerializeField] AudioSource musicSource;            // מוזיקת סיום
    [SerializeField] AudioClip completeClip;
    [Range(0f, 1f)] [SerializeField] float completeVolume = 1f;
    [SerializeField] bool stopSfxOnComplete = true;

    [Header("Fade Out On Complete")]
    [SerializeField] CanvasGroup canvasGroup;   // אם ריק, יימצא אוטומטית
    [SerializeField] float fadeOutDuration = 1.2f;
    [SerializeField] float fadeOutDelay = 0.25f;
    [SerializeField] bool disableGameObjectAfterFade = true;

    [Header("Timer Control On Complete")]
    [Tooltip("אם true – כשהבר מתמלא נעצור את הטיימר ונעלים את קנבס השעון")]
    [SerializeField] bool stopTimerAndHideClock = true;

    [Tooltip("רפרנס ל-CountdownTimer (אם ריק – נאתר אוטומטית בסצינה)")]
    [SerializeField] CountdownTimer countdown;

    [Tooltip("האובייקט של קנבס השעון להעלמה (אם ריק – ננסה לקחת מ-countdown.timerUI)")]
    [SerializeField] GameObject timerCanvasToHide;

    int total = 6;
    int current = 0;
    bool completed;

    void Awake()
    {
        if (!canvasGroup) canvasGroup = GetComponentInParent<CanvasGroup>();
        if (canvasGroup) canvasGroup.alpha = 1f;
    }

    public void Init(int totalTargets)
    {
        Debug.Log("Debug : ProgressBG.Init עם totalTargets=" + totalTargets, this);
        total = Mathf.Max(1, totalTargets);
        current = 0;
        completed = false;
        UpdateUI();
    }

    /// מדווח התקדמות של יחידה אחת (אפשר להעביר Emitter כדי שהניצוצות יצאו ממנו)
    public void ReportOne(Transform emitter = null)
    {
        if (completed) return;

        current = Mathf.Clamp(current + 1, 0, total);
        Debug.Log($"[ProgressBG] ReportOne: {current}/{total}"); // <—

        UpdateUI();

        // ניצוצות
        if (sparklePrefab != null)
        {
            Vector3 pos = emitter ? emitter.position + sparkleOffset
                                  : transform.position + sparkleOffset;

            var vfx = Instantiate(sparklePrefab, pos, Quaternion.identity);
            vfx.Play();
            Destroy(vfx.gameObject, sparkleLifetime);
        }

        // טיק
        PlayTick();

        // סיום
        if (current >= total && !completed)
        {
            OnComplete();
        }
    }

    void PlayTick()
    {
        if (!sfxSource || !tickClip) return;

        float origPitch = sfxSource.pitch;
        if (tickPitchRange.x != 1f || tickPitchRange.y != 1f)
            sfxSource.pitch = Random.Range(tickPitchRange.x, tickPitchRange.y);

        sfxSource.PlayOneShot(tickClip, tickVolume);
        sfxSource.pitch = origPitch;
    }

    void OnComplete()
    {
        completed = true;

        if (stopSfxOnComplete && sfxSource) sfxSource.Stop();

        if (musicSource && completeClip)
        {
            musicSource.clip = completeClip;
            musicSource.loop = false;
            musicSource.volume = completeVolume;
            musicSource.Play();
        }

        // --- חדש: עצירת טיימר והעלמת קנבס השעון ---
        if (stopTimerAndHideClock)
        {
            // מצא טיימר אם לא חיברנו ידנית
            if (!countdown) countdown = FindObjectOfType<CountdownTimer>();

            if (countdown)
            {
                countdown.PauseTimer(); // מפסיק את הספירה לאחור

                // אם לא חיברנו קנבס לשעון – ננסה לקחת מהטיימר עצמו (timerUI)
                if (!timerCanvasToHide) timerCanvasToHide = countdown.timerUI;

                if (timerCanvasToHide) timerCanvasToHide.SetActive(false);
            }
        }

        //if (fadeOutDuration > 0f && canvasGroup)
        //    StartCoroutine(FadeOutAndDisable());
        //else if (disableGameObjectAfterFade)
            //gameObject.SetActive(false);
    }

    //IEnumerator FadeOutAndDisable()
    //{
    //    if (fadeOutDelay > 0f) yield return new WaitForSeconds(fadeOutDelay);

    //    float t = 0f;
    //    float start = canvasGroup.alpha;
    //    while (t < fadeOutDuration)
    //    {
    //        t += Time.deltaTime;
    //        float k = Mathf.Clamp01(t / fadeOutDuration);
    //        canvasGroup.alpha = Mathf.Lerp(start, 0f, k);
    //        yield return null;
    //    }
    //    canvasGroup.alpha = 0f;

    //    if (disableGameObjectAfterFade)
    //        gameObject.SetActive(false);
    //}

    void UpdateUI()
    {
        float ratio = (float)current / total;

        if (fill)  fill.fillAmount = ratio;
        if (label) label.text = $"{current}/{total}";
    }
}