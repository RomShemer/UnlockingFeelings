using UnityEngine;

[ExecuteAlways]   // גם בעורך
public class BookVariantSwitcher : MonoBehaviour
{
    public GameObject closedRoot;      // גרור את Buch
    public GameObject[] openRoots;     // גרור את book content + book cover and side

    public enum InitialState { Closed, Open }
    public InitialState startState = InitialState.Closed;

    [Tooltip("כשהספר פתוח ננעל את הגרירה (אופציונלי, בזמן ריצה)")]
    public bool disableGrabWhenOpen = false;

    Rigidbody rb;
    Behaviour grab; // XRGrabInteractable אם קיים

    void OnEnable()  { Cache(); ApplyState(startState == InitialState.Open); }
    void OnValidate(){ ApplyState(startState == InitialState.Open); } // כל שינוי באינספקטור
    void Start()     { ApplyState(startState == InitialState.Open); }

    void Cache()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!grab)
        {
            var xrType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
            if (xrType != null) grab = (Behaviour)GetComponent(xrType);
        }
    }

    public void Open()  => ApplyState(true);
    public void Close() => ApplyState(false);

    public void ApplyState(bool open)
    {
        if (closedRoot) closedRoot.SetActive(!open);
        if (openRoots != null)
            foreach (var g in openRoots) if (g) g.SetActive(open);

        // הגיוני רק בזמן ריצה
        if (Application.isPlaying && disableGrabWhenOpen)
        {
            if (grab) grab.enabled = !open;
            if (rb)   rb.isKinematic = open;
        }
    }

    // תפריט מהיר בלחיצה ימנית על הקומפוננטה
    [ContextMenu("Force Closed (Editor)")]
    void ForceClosed() { startState = InitialState.Closed; ApplyState(false); }

    [ContextMenu("Force Open (Editor)")]
    void ForceOpen() { startState = InitialState.Open; ApplyState(true); }
}