// ProgressBarUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.VFX;
using System.Collections;

public class ProgressBarUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] Image fill;
    [SerializeField] TextMeshProUGUI label;

    [Header("Sparkle VFX")]
    [SerializeField] VisualEffect sparklePrefab;         // Prefab של הניצוצות
    [SerializeField] Vector3 sparkleOffset = new Vector3(0, 0.2f, 0);
    [SerializeField] float sparkleLifetime = 2f;
    [SerializeField] string playEventName = "OnPlay";    // השם של האירוע ב-VFX Graph (לא חובה)

    [Header("Sounds")]
    [Tooltip("AudioSource להשמעת טיק קצר בכל התקדמות")]
    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioClip tickClip;
    [Range(0f, 1f)] [SerializeField] float tickVolume = 0.9f;
    [SerializeField] Vector2 tickPitchRange = new Vector2(0.95f, 1.05f);  // וריאציה עדינה

    [Tooltip("AudioSource למוזיקת סיום כשהבר מתמלא")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioClip completeClip;
    [Range(0f, 1f)] [SerializeField] float completeVolume = 1f;
    [SerializeField] bool stopSfxOnComplete = true;

    [Header("Fade Out On Complete")]
    [SerializeField] CanvasGroup canvasGroup;   // אם ריק, יימצא אוטומטית בהפעלה
    [SerializeField] float fadeOutDuration = 1.2f;
    [SerializeField] float fadeOutDelay = 0.25f; // שנייה קטנה לפני שמתחילים פאייד
    [SerializeField] bool disableGameObjectAfterFade = true;

    int total = 1;
    int current = 0;
    bool completed;

    void Awake()
    {
        if (!canvasGroup) canvasGroup = GetComponentInParent<CanvasGroup>();
        if (canvasGroup) canvasGroup.alpha = 1f;
    }

    public void Init(int totalTargets)
    {
        total = Mathf.Max(1, totalTargets);
        current = 0;
        completed = false;
        Debug.Log($"[ProgressBarUI:{name}] Init → total={total}, current={current}");
        UpdateUI();
    }

    /// <summary>
    /// מדווח התקדמות של יחידה אחת (אפשר להעביר Emitter כדי שהניצוצות יצאו ממנו)
    /// </summary>
    public void ReportOne(Transform emitter = null)
    {
        if (completed) return;

        current = Mathf.Clamp(current + 1, 0, total);
        Debug.Log($"[ProgressBarUI:{name}] ReportOne → current={current}/{total}");
        UpdateUI();

        // ===== אפקט ניצוצות =====
        if (sparklePrefab != null)
        {
            Vector3 pos = emitter ? emitter.position + sparkleOffset
                                  : transform.position + sparkleOffset;

            var vfx = Instantiate(sparklePrefab, pos, Quaternion.identity);
            vfx.Play();
            Destroy(vfx.gameObject, sparkleLifetime);
        }

        // ===== צליל טיק על כל התקדמות =====
        PlayTick();

        // ===== סיום =====
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
        Debug.Log($"[ProgressBarUI:{name}] COMPLETE!");

        if (stopSfxOnComplete && sfxSource) sfxSource.Stop();

        if (musicSource && completeClip)
        {
            musicSource.clip = completeClip;
            musicSource.loop = false;
            musicSource.volume = completeVolume;
            musicSource.Play();
        }

        if (fadeOutDuration > 0f && canvasGroup)
            StartCoroutine(FadeOutAndDisable());
        else if (disableGameObjectAfterFade)
            gameObject.SetActive(false);
    }

    IEnumerator FadeOutAndDisable()
    {
        if (fadeOutDelay > 0f) yield return new WaitForSeconds(fadeOutDelay);

        float t = 0f;
        float start = canvasGroup.alpha;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeOutDuration);
            canvasGroup.alpha = Mathf.Lerp(start, 0f, k);
            yield return null;
        }
        canvasGroup.alpha = 0f;

        if (disableGameObjectAfterFade)
        {
            // אם ה-CanvasGroup יושב על אבא, עדיף לכבות את האבא שלו
            gameObject.SetActive(false);
        }
    }

    void UpdateUI()
    {
        float ratio = (float)current / total;

        if (fill)
        {
            fill.fillAmount = ratio;
            Debug.Log($"[ProgressBarUI:{name}] UpdateUI → fillAmount={fill.fillAmount}");
        }
        if (label)
        {
            label.text = $"{current}/{total}";
            Debug.Log($"[ProgressBarUI:{name}] UpdateUI → label={label.text}");
        }
    }
}
