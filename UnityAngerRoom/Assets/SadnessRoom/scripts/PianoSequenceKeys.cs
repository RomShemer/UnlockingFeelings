using UnityEngine;
using UnityEngine.Serialization;

public class PianoSequenceKeys : MonoBehaviour
{
    [Header("Sequence (exact order)")]
    public string[] sequence = new string[10];

    [Header("Music")]
    public AudioSource musicSource;   // 2D AudioSource (Spatial Blend = 0)
    public AudioClip musicClip;
    public bool loop = true;

    [Header("Behaviour")]
    public bool startOnlyOnce = true;

    [FormerlySerializedAs("startOnFirstCorrect")]
    public bool startOnFirstCorrect = false;   // false = להתחיל רק בסוף הרצף

    public bool strictResetOnError = false;
    public float maxGapSeconds = 0f;

    [Header("Performance")]
    public bool prewarmMusic = true;
    public bool usePlayScheduled = true;
    public double scheduleLeadTime = 0.10;
    
    public BookPageTurnSimple bookTurn;  // גרור את OpenBook_Correct עם הסקריפט
    public Texture2D nextSpread;         // גרור את התמונה החדשה (NW)

    void OnAllNotesCompleted()
    {
        bookTurn.FlipTo(nextSpread);
    }


    // --- חדש: חיבור לבר ההתקדמות ---
    [Header("UI Progress")]
    public ProgressBarUI progress;   // גרור כאן את רכיב ProgressBarUI מה-Canvas

    int index = 0;
    bool started = false;
    float lastHitTime = -1f;

    void OnEnable()  => PianoKeySound.AnyPressed += OnKeyPressed;
    void OnDisable() => PianoKeySound.AnyPressed -= OnKeyPressed;

    System.Collections.IEnumerator Start()
    {
        // סנכרון הבר למספר הצעדים ברצף
        if (progress != null)
        {
            progress.ResetProgress();
            progress.totalSteps = (sequence != null && sequence.Length > 0) ? sequence.Length : 10;
        }

        if (!prewarmMusic || !musicClip) yield break;

        if (musicClip.loadState != AudioDataLoadState.Loaded)
        {
            musicClip.LoadAudioData();
            while (musicClip.loadState == AudioDataLoadState.Loading)
                yield return null;
        }

        if (musicSource)
        {
            musicSource.clip = musicClip;
            musicSource.loop = loop;
        }
    }

    void OnKeyPressed(PianoKeySound key, string token)
    {
        if (sequence == null || sequence.Length == 0) return;

        // פער זמן גדול? מאפסים גם את הבר
        if (maxGapSeconds > 0f && lastHitTime > 0f && Time.time - lastHitTime > maxGapSeconds)
        {
            index = 0;
            if (progress) progress.ResetProgress();
        }

        // צעד נכון?
        if (token == sequence[index])
        {
            // אם ביקשת להתחיל כבר על הראשון
            if (index == 0 && startOnFirstCorrect)
                TryStartMusic();

            index++;

            // עדכן בר: צעד נכון = קפיצה קדימה
            if (progress) progress.OnCorrectNote();

            // הושלם הרצף כולו
            if (index >= sequence.Length)
            {
                TryStartMusic();      // מתחיל את המוזיקה
                // אל תאפס את הבר – הוא מלא עכשיו
                index = 0;            // מכין לסיבוב הבא

                OnAllNotesCompleted();

            }
        }
        else
        {
            // טעות:
            // אם strictResetOnError או שהתווים לא התחילו מחדש – איפוס מלא
            if (strictResetOnError || token != sequence[0])
            {
                index = 0;
                if (progress) progress.OnWrongNote(); // מאפס את הבר
            }
            else
            {
                // הטוקן הוא התו הראשון ברצף: מתחילים סשן חדש מ-1
                index = 1;
                if (progress)
                {
                    progress.OnWrongNote();   // איפוס
                    progress.OnCorrectNote(); // ופעם אחת קדימה עבור התו הראשון
                }
                if (startOnFirstCorrect) TryStartMusic();
            }
        }

        lastHitTime = Time.time;
    }

    void TryStartMusic()
    {
        if (!musicSource || !musicClip) return;
        if (startOnlyOnce && started) return;

        musicSource.clip = musicClip;
        musicSource.loop = loop;

        if (usePlayScheduled)
        {
            double when = AudioSettings.dspTime + scheduleLeadTime;
            musicSource.PlayScheduled(when);
        }
        else
        {
            musicSource.Play();
        }

        started = true;
    }
}
