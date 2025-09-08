using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider), typeof(Rigidbody))]
public class PianoKeySound : MonoBehaviour
{
    [Header("Motion")]
    public float downDistance = 0.015f;
    public float pressTime = 0.06f;
    public float releaseTime = 0.1f;
    public bool lockWhilePressed = true;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip note;

    [Header("Activator filter")]
    public LayerMask activatorLayers;   // תבחר את KeyActivator
    public string[] activatorTags = { }; // לא חובה

    // === חדש: אירוע גלובלי (כל מקש משדר כשהוא נלחץ) + אירוע ל-Inspector
    public static event Action<PianoKeySound, string> AnyPressed;
    public UnityEvent onPressed;

    Vector3 startLocalPos;
    bool busy;

    // מעקב על כל הקלידים + “בעלות” על הבועה (קליד אחד בלבד)
    static readonly List<PianoKeySound> all = new();
    static readonly Dictionary<Collider, PianoKeySound> claim = new();

    void OnEnable()  => all.Add(this);
    void OnDisable() => all.Remove(this);

    void Awake()
    {
        startLocalPos = transform.localPosition;

        var col = GetComponent<Collider>(); col.isTrigger = true;
        var rb  = GetComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;

        if (!audioSource) audioSource = GetComponentInParent<AudioSource>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsAllowed(other)) return;

        if (claim.TryGetValue(other, out var owner) && owner != this) return;  // הבועה תפוסה
        if (!IsNearestKeyTo(other)) return;                                     // יש קליד קרוב יותר

        claim[other] = this;  // תופס את הבועה עד היציאה
        Press();
    }

    void OnTriggerExit(Collider other)
    {
        if (claim.TryGetValue(other, out var owner) && owner == this)
            claim.Remove(other);
    }

    bool IsAllowed(Collider other)
    {
        if (activatorLayers.value != 0) {
            int bit = 1 << other.gameObject.layer;
            if ((activatorLayers.value & bit) == 0) return false;
        }
        if (activatorTags != null && activatorTags.Length > 0) {
            bool ok = false;
            foreach (var t in activatorTags)
                if (!string.IsNullOrEmpty(t) && other.CompareTag(t)) { ok = true; break; }
            if (!ok) return false;
        }
        return true;
    }

    bool IsNearestKeyTo(Collider other)
    {
        Vector3 p = other.bounds.center; p.y = 0f;
        float my = HorizontalDistSqr(p, GetComponent<Collider>().bounds.center);
        foreach (var k in all)
        {
            if (k == this) continue;
            var kc = k.GetComponent<Collider>();
            if (kc == null || !kc.enabled) continue;
            float dk = HorizontalDistSqr(p, kc.bounds.center);
            if (dk < my) return false;
        }
        return true;
    }
    static float HorizontalDistSqr(Vector3 a, Vector3 b) { a.y = b.y = 0f; return (a - b).sqrMagnitude; }

    [ContextMenu("Test Press")]
    public void Press()
    {
        if (lockWhilePressed && busy) return;
        StartCoroutine(PressRoutine());
    }

    System.Collections.IEnumerator PressRoutine()
    {
        busy = true;

        // >>> כאן אנו משדרים שהמקש נלחץ, יחד עם שם התו (למשל "Ds4")
        AnyPressed?.Invoke(this, GetNoteToken());
        onPressed?.Invoke();

        Vector3 downPos = startLocalPos + Vector3.down * downDistance;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(pressTime, 0.0001f);
            transform.localPosition = Vector3.Lerp(startLocalPos, downPos, t);
            yield return null;
        }

        if (audioSource && note) audioSource.PlayOneShot(note);

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(releaseTime, 0.0001f);
            transform.localPosition = Vector3.Lerp(downPos, startLocalPos, t);
            yield return null;
        }

        busy = false;
    }

    // לוקח את התו משם האובייקט: "Key_Ds4" -> "Ds4"
    string GetNoteToken()
    {
        string n = name;
        int i = n.LastIndexOf('_');
        return (i >= 0 && i < n.Length - 1) ? n.Substring(i + 1) : n;
    }
}
