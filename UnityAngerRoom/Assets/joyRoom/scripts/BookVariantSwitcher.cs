using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]   // גם בעורך
public class BookVariantSwitcher : MonoBehaviour
{
    [Header("Book Objects")]
    public GameObject closedRoot;      // הספר הסגור
    public GameObject[] openRoots;     // חלקי הספר הפתוח

    public enum InitialState { Closed, Open }
    public InitialState startState = InitialState.Closed;

    [Header("Grab Lock")]
    [Tooltip("כשהספר פתוח ננטרל כל רכיבי Grab/RayGrab")]
    public bool disableGrabWhenOpen = true;
    [Tooltip("גם כיבוי קוליידרים כשהספר פתוח (לא חובה)")]
    public bool disableCollidersWhenOpen = false;

    Rigidbody rb;

    // נאסוף כל קומפוננטת "תפיסה" רלוונטית (Meta/Interaction SDK/XRI/OVR וכו')
    readonly List<Behaviour> grabBehaviours = new List<Behaviour>();
    readonly List<Collider>  colliders      = new List<Collider>();

    void OnEnable()  { Cache(); ApplyState(startState == InitialState.Open); }
    void OnValidate(){ ApplyState(startState == InitialState.Open); } // שינוי באינספקטור
    void Start()     { ApplyState(startState == InitialState.Open); }

    void Cache()
    {
        if (!rb) rb = GetComponent<Rigidbody>();

        grabBehaviours.Clear();
        colliders.Clear();

        // אסוף כל Behaviour ומיין לפי שמות סוגים טיפוסיים
        var allBehaviours = GetComponentsInChildren<Behaviour>(true);
        foreach (var b in allBehaviours)
        {
            if (!b) continue;
            var n = b.GetType().Name;
            // טיפוסים נפוצים: Grabbable, RayInteractable, RayGrabInteractable, XRGrabInteractable, OVRGrabbable וכו'
            if (n.Contains("Grabbable") || n.Contains("GrabInteractable") || n.Contains("RayInteractable") || n.Contains("RayGrab"))
                grabBehaviours.Add(b);
        }

        // אופציונלית – קח גם קוליידרים
        colliders.AddRange(GetComponentsInChildren<Collider>(true));
    }

    public void Open()  => ApplyState(true);
    public void Close() => ApplyState(false);

    public void ApplyState(bool open)
    {
        if (closedRoot) closedRoot.SetActive(!open);
        if (openRoots != null)
            foreach (var g in openRoots) if (g) g.SetActive(open);

        // רלוונטי בזמן ריצה/עורך (כדי לראות תוצאה מיד)
        if (disableGrabWhenOpen)
        {
            foreach (var beh in grabBehaviours)
                if (beh) beh.enabled = !open;
        }

        if (disableCollidersWhenOpen)
        {
            foreach (var c in colliders)
                if (c) c.enabled = !open;
        }

        // פיזיקה: כשפתוח נועל תנועה (לבחירה – נשאיר נוח)
        if (Application.isPlaying && rb)
            rb.isKinematic = open;
    }

    // תפריט מהיר בלחיצה ימנית על הקומפוננטה
    [ContextMenu("Force Closed (Editor)")]
    void ForceClosed() { startState = InitialState.Closed; ApplyState(false); }

    [ContextMenu("Force Open (Editor)")]
    void ForceOpen() { startState = InitialState.Open; ApplyState(true); }
}
