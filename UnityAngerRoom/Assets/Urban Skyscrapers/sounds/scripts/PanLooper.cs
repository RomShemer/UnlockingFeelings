using UnityEngine;

public class PanLooper : MonoBehaviour
{
    public AudioSource tensionSource;  // ���� �� �-Audio Source �� �-Tension ����
    public float panSpeed = 0.5f;      // ��� ��� ���� ��� ���

    void Update()
    {
        // sin ����� ����� ���� ��� -1 �-1
        tensionSource.panStereo = Mathf.Sin(Time.time * panSpeed);
    }
}
