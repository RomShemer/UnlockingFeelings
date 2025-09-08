using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class SnapshotLooper : MonoBehaviour
{
    [Header("Snapshots")]
    public AudioMixerSnapshot calmSnapshot;
    public AudioMixerSnapshot tenseSnapshot;

    [Header("Timings (seconds)")]
    public float fadeSeconds = 2f;        // זמן קרוס־פייד בין המצבים
    public float holdCalmSeconds = 6f;    // כמה זמן להישאר על Calm
    public float holdTenseSeconds = 6f;   // כמה זמן להישאר על Tense

    void Start()
    {
        StartCoroutine(Loop());
    }

    IEnumerator Loop()
    {
        while (true)
        {
            // Calm → החזקה
            if (calmSnapshot != null) calmSnapshot.TransitionTo(fadeSeconds);
            yield return new WaitForSeconds(holdCalmSeconds);

            // Tense → החזקה
            if (tenseSnapshot != null) tenseSnapshot.TransitionTo(fadeSeconds);
            yield return new WaitForSeconds(holdTenseSeconds);
        }
    }
}
