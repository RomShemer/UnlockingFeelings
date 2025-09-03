// ProgressBarUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProgressBarUI : MonoBehaviour
{
    [SerializeField] Image fill;
    [SerializeField] TextMeshProUGUI label;

    int total = 1;
    int current = 0;

    public void Init(int totalTargets)
    {
        total = Mathf.Max(1, totalTargets);
        current = 0;
        Debug.Log($"[ProgressBarUI:{name}] Init → total={total}, current={current}");
        UpdateUI();
    }

    public void ReportOne()
    {
        current = Mathf.Clamp(current + 1, 0, total);
        Debug.Log($"[ProgressBarUI:{name}] ReportOne → current={current}/{total}");
        UpdateUI();
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
