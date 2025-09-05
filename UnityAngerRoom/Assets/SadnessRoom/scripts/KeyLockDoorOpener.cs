using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class KeyLockDoorOpener : MonoBehaviour
{
    [Header("References")]
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket;   // XR Socket על LockSocket
    public AN_DoorScript door;          // AN_DoorScript שעל Door
    public HingeJoint doorHinge;        // HingeJoint של הדלת
    public Transform snapPoint;         // נקודת ישיבה למפתח
    public string requiredTag = "Key";  // תגית הזיהוי של המפתח

    [Header("Hold Open")]
    public float holdAngle = 85f;       // זווית פתיחה
    public float spring = 900f;
    public float damper = 60f;

    bool opened = false;

    void Reset() { socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>(); }

    void OnEnable()
    {
        if (socket == null) socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        socket.selectEntered.AddListener(OnSelectEntered);
    }

    void OnDisable()
    {
        if (socket != null) socket.selectEntered.RemoveListener(OnSelectEntered);
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (opened) return;

        var go = args.interactableObject.transform.gameObject;
        if (!go.CompareTag(requiredTag)) return; // לא המפתח

        // לקבע את המפתח במנעול
        var rb = go.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        if (snapPoint != null)
            go.transform.SetPositionAndRotation(snapPoint.position, snapPoint.rotation);
        go.transform.SetParent(transform, true); // ילד של LockSocket

        // פתיחת דלת + ביטול אפשרות סגירה
        if (door != null)
        {
            door.Remote   = true;    // לא דרך E
            door.CanClose = false;   // לא תיסגר
            door.RedLocked = false;  // ליתר ביטחון
            door.BlueLocked = false;
            door.Action();           // פתח אם סגור
        }

        // להחזיק פתוח ע"י קפיץ בציר
        if (doorHinge != null)
        {
            var lim = doorHinge.limits;
            lim.max = Mathf.Max(lim.max, holdAngle);
            doorHinge.limits = lim;
            doorHinge.useLimits = true;

            var sp = doorHinge.spring;
            sp.spring = spring;
            sp.damper = damper;
            sp.targetPosition = holdAngle;
            doorHinge.spring = sp;
            doorHinge.useSpring = true;
        }

        opened = true;
        socket.enabled = false; // לא לקלוט עוד אובייקטים
    }
}
