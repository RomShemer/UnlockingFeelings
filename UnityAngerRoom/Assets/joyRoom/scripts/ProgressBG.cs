// ProgressBarUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.VFX;

public class ProgressBarUI : MonoBehaviour
{
    [SerializeField] Image fill;
    [SerializeField] TextMeshProUGUI label;

    [Header("Sparkle VFX")]
    [SerializeField] VisualEffect sparklePrefab;         // Prefab של הניצוצות
    [SerializeField] Vector3 sparkleOffset = new Vector3(0, 0.2f, 0);
    [SerializeField] float sparkleLifetime = 2f;
    [SerializeField] string playEventName = "OnPlay";    // השם של האירוע ב-VFX Graph (ברירת מחדל OnPlay)

    int total = 1;
    int current = 0;

    public void Init(int totalTargets)
    {
        total = Mathf.Max(1, totalTargets);
        current = 0;
        Debug.Log($"[ProgressBarUI:{name}] Init → total={total}, current={current}");
        UpdateUI();
    }

    // אפשר להעביר את Transform של הפרפר כדי שהניצוצות יצאו משם
    public void ReportOne(Transform emitter = null)
    {
        current = Mathf.Clamp(current + 1, 0, total);
        Debug.Log($"[ProgressBarUI:{name}] ReportOne → current={current}/{total}");
        UpdateUI();

        // ===== אפקט ניצוצות =====
        if (sparklePrefab != null)
        {
            Vector3 pos = emitter ? emitter.position + sparkleOffset
                                  : transform.position + sparkleOffset;

            var vfx = Instantiate(sparklePrefab, pos, Quaternion.identity);
            vfx.Play();

            Destroy(vfx.gameObject, sparkleLifetime);
        }
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
