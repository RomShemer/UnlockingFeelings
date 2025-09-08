using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    public AudioSource audioSource;   // AudioSource כללי
    public AudioClip clickSound;      // הסאונד של הלחיצה

    void Awake()
    {
        // מחבר את הפונקציה לניגון לאירוע OnClick של הכפתור
        GetComponent<Button>().onClick.AddListener(PlayClickSound);
    }

    void PlayClickSound()
    {
        if (audioSource && clickSound)
            audioSource.PlayOneShot(clickSound);
    }
}