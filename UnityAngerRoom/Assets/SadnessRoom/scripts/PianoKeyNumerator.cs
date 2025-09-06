using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PianoKeyNumerator : MonoBehaviour
{
    [Header("Setup")]
    public Transform keysParent;        // האב שמכיל את הקלידים (למשל "Piano_Colliders")
    public TMP_Text labelPrefab;        // גרור את KeyLabelPrefab
    public Transform sortReference;     // אופציונלי: מה מגדיר את כיוון הימין (Right) של המקלדת

    [Header("Detection")]
    public string keyNamePrefix = "Key_";  // שמות כמו Key_Cs1, Key_F2 וכו'
    public bool onlyNamesWithPrefix = true;

    [Header("Layout")]
    public Vector3 extraWorldOffset = new Vector3(0f, 0.01f, 0f); // הרמה מעל הקליד
    public float sizeFactor = 0.7f;      // גודל התווית יחסית לרוחב הקליד
    public float minWorldScale = 0.01f;  // רצפה לגודל (מטרים)
    public float maxWorldScale = 0.15f;  // תקרה לגודל

    [Header("Control")]
    public bool createOnStart = true;
    public bool startHidden = false;
    public bool logToConsole = true;
    
    [Header("Ordering")]
    public bool leftToRight = true; // <- חדש: true = משמאל לימין, false = מימין לשמאל

    readonly List<TMP_Text> labels = new();
    readonly List<Transform> keyOrder = new();

    void Start()
    {
        if (createOnStart) CreateOrRefresh();
        if (startHidden) SetLabelsVisible(false);
    }

    // יוצר/מרענן מספרים
    public void CreateOrRefresh()
    {
        ClearExisting();

        if (!keysParent || !labelPrefab)
        {
            Debug.LogWarning("PianoKeyNumerator: assign keysParent + labelPrefab.");
            return;
        }

        // לוקחים את כל ה-Transfrom שיש לו קוליידר (כי אצלך הקלידים השחורים הם האמיתיים)
        var candidates = keysParent.GetComponentsInChildren<Collider>(true)
                                   .Select(c => c.transform)
                                   .Distinct();

        if (onlyNamesWithPrefix)
            candidates = candidates.Where(t => t.name.StartsWith(keyNamePrefix));

        if (!candidates.Any())
        {
            Debug.LogWarning("PianoKeyNumerator: No keys found under " + keysParent.name);
            return;
        }

        // ממיינים לפי ציר X המקומי של האב (או sortReference אם הוגדר)
        Transform basis = sortReference ? sortReference : keysParent;
        var ordered = candidates
            .Select(t => new { t, x = basis.InverseTransformPoint(t.position).x })
            .OrderBy(p => leftToRight ? p.x : -p.x)   // <- כאן ההיפוך
            .Select(p => p.t)
            .ToList();


        keyOrder.Clear();
        keyOrder.AddRange(ordered);

        // יצירת תווית לכל קליד
        for (int i = 0; i < keyOrder.Count; i++)
        {
            var key = keyOrder[i];
            var col = key.GetComponent<Collider>();
            if (!col) continue;

            // מרכז למעלה של הקוליידר
            var b = col.bounds;
            Vector3 topCenter = new Vector3(b.center.x, b.max.y, b.center.z);

            // יוצרים את המספר
            var lbl = Instantiate(labelPrefab);
            lbl.text = (i + 1).ToString();

            // מיקום וסקייל בעולם (לא תלוי בסקייל משוגע של האב)
            lbl.transform.position = topCenter + extraWorldOffset;

            // קובע גודל לפי רוחב הקליד
            float baseSize = Mathf.Max(b.size.x, b.size.z); // הרוחב בפועל
            float worldScale = Mathf.Clamp(baseSize * sizeFactor, minWorldScale, maxWorldScale);
            lbl.transform.localScale = Vector3.one * worldScale;

            // מבט אל המצלמה
            var bb = lbl.GetComponent<BillboardToCamera>();
            if (!bb) bb = lbl.gameObject.AddComponent<BillboardToCamera>();
            bb.onlyYaw = true;

            labels.Add(lbl);
        }

        if (logToConsole) Debug.Log($"PianoKeyNumerator: created {labels.Count} labels.");
    }

    public void SetLabelsVisible(bool visible)
    {
        foreach (var l in labels) if (l) l.gameObject.SetActive(visible);
    }

    public void ClearExisting()
    {
        foreach (var l in labels) if (l) DestroyImmediate(l ? l.gameObject : null);
        labels.Clear();
        keyOrder.Clear();
    }

    public Transform GetKeyByNumber(int number)
    {
        int i = number - 1;
        if (i < 0 || i >= keyOrder.Count) return null;
        return keyOrder[i];
    }
}
