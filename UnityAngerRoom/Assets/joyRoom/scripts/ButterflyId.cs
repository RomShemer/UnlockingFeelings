// Attach על כל פרפר
using UnityEngine;


public class ButterflyId : MonoBehaviour
{
    [Tooltip("לדוגמה: yellow / red / blue / green / pink / purple")]
    public string colorKey;

    [HideInInspector] public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public Collider[] cols;

    void Awake()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        cols = GetComponentsInChildren<Collider>();
    }

    public void FreezeAt(Transform snapPoint)
    {
        // מפסיקים תנועה וממקמים בנקודת היעד
        if (grab) grab.enabled = false;
        if (rb) { rb.isKinematic = true; rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }

        transform.SetPositionAndRotation(snapPoint.position, snapPoint.rotation);

        // מבטלים התנגשויות כדי שלא יזוז יותר
        foreach (var c in cols) if (c) c.isTrigger = true;
    }
}
