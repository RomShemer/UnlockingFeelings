using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

public class ProgressMatchesVFX : MonoBehaviour
{
    [Header("UI")]
    public Image fill;                 // ה-Image של ה-ProgressBar (fillAmount 0..1)
    public int totalButterflies = 6;   // כמה צריך להתאים כדי להשלים

    [Header("VFX")]
    public VisualEffect sparklePrefab; // ה-Visual Effect Graph prefab
    public Vector3 worldOffset = new Vector3(0f, 0.15f, 0f);
    public string playEventName = "OnPlay"; // Initial Event Name ב-VFX (ברירת מחדל OnPlay)
    public float autoDestroyAfter = 2f;

    // מעקב כדי לא לספור את אותו פרפר פעמיים
    private readonly HashSet<Transform> matched = new HashSet<Transform>();
    private int matchedCount = 0;

    public void ReportMatch(Transform butterfly)
    {
        if (!butterfly || matched.Contains(butterfly)) return;

        matched.Add(butterfly);
        matchedCount = Mathf.Min(matchedCount + 1, totalButterflies);

        // עדכון פס התקדמות
        if (fill) fill.fillAmount = (float)matchedCount / Mathf.Max(1, totalButterflies);

        // ניצוצות במיקום הפרפר
        if (sparklePrefab)
        {
            var pos = butterfly.position + worldOffset;
            var vfx = Instantiate(sparklePrefab, pos, Quaternion.identity);
            if (!string.IsNullOrEmpty(playEventName)) vfx.SendEvent(playEventName); else vfx.Play();
            Destroy(vfx.gameObject, autoDestroyAfter);
        }
    }

    public void ResetProgress()
    {
        matched.Clear();
        matchedCount = 0;
        if (fill) fill.fillAmount = 0f;
    }
}