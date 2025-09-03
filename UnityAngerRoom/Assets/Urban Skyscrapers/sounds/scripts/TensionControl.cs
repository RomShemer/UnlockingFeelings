using UnityEngine;
using UnityEngine.Audio;

public class TensionControl : MonoBehaviour
{
    public AudioMixerSnapshot calmSnapshot;
    public AudioMixerSnapshot tenseSnapshot;

    public void ToCalm()
    {
        calmSnapshot.TransitionTo(2f); // מעבר חלק ב-2 שניות
    }

    public void ToTense()
    {
        tenseSnapshot.TransitionTo(2f);
    }
}
