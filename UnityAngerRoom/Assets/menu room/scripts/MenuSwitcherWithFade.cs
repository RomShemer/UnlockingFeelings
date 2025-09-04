using UnityEngine;
using System.Collections;

public class MenuSwitcherWithFade : MonoBehaviour
{
    [Header("Canvases")]
    [SerializeField] CanvasGroup menuCanvas;
    [SerializeField] CanvasGroup instructionsCanvas;

    [Header("Character")]
    [SerializeField] GameObject talkingCharacter;
    [SerializeField] Animator talkingAnimator;
    [SerializeField] string talkTriggerName = "Talk";
    [SerializeField] CanvasGroup talkingCanvasGroup;
    [SerializeField] float charFadeDuration = 0.4f;

    [Header("Audio / Subtitles")]
    [SerializeField] AudioSource voiceOver;
    [SerializeField] SubtitleManager subtitleManager;

    [Header("Fade Settings")]
    [SerializeField] float fadeDuration = 0.5f;
    [SerializeField] float subtitleEndPadding = 0.0f;

    [Header("Safety / UX")]
    [SerializeField] float minInstructionSeconds = 0.5f;  // כמה זמן לפחות להישאר על מסך ההוראות

    Coroutine currentFade;
    Coroutine charFade;
    bool _isReturning = false;

    void Start()
    {
        SafeSetVisible(menuCanvas, true);
        SafeSetVisible(instructionsCanvas, false);
        if (talkingCharacter) talkingCharacter.SetActive(false);

        if (subtitleManager != null)
            subtitleManager.OnSubtitlesCompleted += HandleSubtitlesCompleted;
    }

    void OnDestroy()
    {
        if (subtitleManager != null)
            subtitleManager.OnSubtitlesCompleted -= HandleSubtitlesCompleted;
    }

    public void ShowInstructions()
    {
        _isReturning = false;

        if (currentFade != null) StopCoroutine(currentFade);
        currentFade = StartCoroutine(SwitchCanvases(menuCanvas, instructionsCanvas));

        if (talkingCharacter)
        {
            talkingCharacter.SetActive(true);
            if (talkingCanvasGroup)
            {
                if (charFade != null) StopCoroutine(charFade);
                talkingCanvasGroup.alpha = 0f;
                charFade = StartCoroutine(FadeCanvasGroup(talkingCanvasGroup, 0f, 1f, charFadeDuration));
            }
        }

        if (talkingAnimator && !string.IsNullOrEmpty(talkTriggerName))
            talkingAnimator.SetTrigger(talkTriggerName);

        if (voiceOver)
        {
            voiceOver.mute = false;
            voiceOver.volume = Mathf.Max(voiceOver.volume, 0.7f);
            voiceOver.spatialBlend = 0f;
            voiceOver.ignoreListenerPause = true;
            voiceOver.time = 0f;
            voiceOver.Play();
            StartCoroutine(WaitForVoiceOverEnd());   // יטפל גם במקרה שהוא לא מתחיל
        }

        if (subtitleManager)
            subtitleManager.StartForAudio();
    }

    public void BackToMenu()
    {
        if (_isReturning) return;
        _isReturning = true;

        if (voiceOver) voiceOver.Stop();
        if (subtitleManager) subtitleManager.StopSubtitles();

        StartCoroutine(BackWithCharFade());
    }

    void HandleSubtitlesCompleted()
    {
        if (_isReturning) return;

        if (voiceOver && voiceOver.isPlaying)
        {
            StartCoroutine(WaitForVoiceOverEnd());
        }
        else
        {
            if (subtitleEndPadding > 0f)
                StartCoroutine(BackAfterDelay(subtitleEndPadding));
            else
                BackToMenu();
        }
    }

    IEnumerator BackAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        BackToMenu();
    }

    IEnumerator WaitForVoiceOverEnd()
    {
        if (!voiceOver)
        {
            // אין אודיו – לפחות תן שהות מינימלית לפני חזרה
            yield return new WaitForSeconds(minInstructionSeconds);
            BackToMenu();
            yield break;
        }

        // 1) חכה שהאודיו *יתחיל* (עד חצי שנייה)
        float startedWait = 0f;
        while (!voiceOver.isPlaying && startedWait < 0.5f)
        {
            startedWait += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!voiceOver.isPlaying)
        {
            // לא התחיל בכלל – אל תחזיר מייד; תן זמן מסך מינימלי
            yield return new WaitForSeconds(minInstructionSeconds);
            BackToMenu();
            yield break;
        }

        // 2) כעת חכה עד שיסתיים
        yield return new WaitUntil(() => !voiceOver.isPlaying);

        BackToMenu();
    }

    IEnumerator BackWithCharFade()
    {
        if (talkingCanvasGroup)
        {
            if (charFade != null) StopCoroutine(charFade);
            yield return FadeCanvasGroup(talkingCanvasGroup, talkingCanvasGroup.alpha, 0f, charFadeDuration);
        }

        if (talkingCharacter) talkingCharacter.SetActive(false);

        if (currentFade != null) StopCoroutine(currentFade);
        currentFade = StartCoroutine(SwitchCanvases(instructionsCanvas, menuCanvas));
    }

    IEnumerator SwitchCanvases(CanvasGroup from, CanvasGroup to)
    {
        if (from)
        {
            yield return FadeCanvas(from, 1f, 0f);
            from.gameObject.SetActive(false);
        }

        if (to)
        {
            to.gameObject.SetActive(true);
            yield return FadeCanvas(to, 0f, 1f);
        }
    }

    IEnumerator FadeCanvas(CanvasGroup cg, float start, float end)
    {
        if (!cg) yield break;
        float elapsed = 0f;
        cg.alpha = start;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, elapsed / fadeDuration);
            yield return null;
        }
        cg.alpha = end;
        bool visible = end > 0.9f;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration)
    {
        if (!cg) yield break;
        float elapsed = 0f;
        cg.alpha = start;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }
        cg.alpha = end;
    }

    void SafeSetVisible(CanvasGroup cg, bool visible)
    {
        if (!cg) return;
        cg.alpha = visible ? 1f : 0f;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
        cg.gameObject.SetActive(visible);
    }
}
