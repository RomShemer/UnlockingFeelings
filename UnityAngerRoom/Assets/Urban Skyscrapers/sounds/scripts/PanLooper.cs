using UnityEngine;

public class PanLooper : MonoBehaviour
{
    public AudioSource tensionSource;  // גררי את ה-Audio Source של ה-Tension לכאן
    public float panSpeed = 0.5f;      // כמה מהר יזוז מצד לצד

    void Update()
    {
        // sin מייצר תנועה חלקה בין -1 ל-1
        tensionSource.panStereo = Mathf.Sin(Time.time * panSpeed);
    }
}
