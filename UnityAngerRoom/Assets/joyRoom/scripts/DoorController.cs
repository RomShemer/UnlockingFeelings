using System.Collections;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Tooltip("ציר הסיבוב של הדלת (האובייקט שהדלת יושבת עליו)")]
    public Transform doorPivot;

    [Tooltip("כמה מעלות לפתוח")]
    public float openAngle = 90f;

    [Tooltip("מהירות פתיחה")]
    public float duration = 1.2f;

    bool opened;

    public void Open()
    {
        if (opened) return;
        opened = true;
        if (doorPivot == null) doorPivot = transform;
        StartCoroutine(OpenRoutine());
    }

    IEnumerator OpenRoutine()
    {
        Quaternion start = doorPivot.rotation;
        Quaternion end = start * Quaternion.Euler(0f, openAngle, 0f);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            doorPivot.rotation = Quaternion.Slerp(start, end, k);
            yield return null;
        }
        doorPivot.rotation = end;
    }
}