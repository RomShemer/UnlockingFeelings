using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class PuzzlePieceHandler : MonoBehaviour
{
    public int PieceID;
    private Rigidbody rb;
    private XRGrabInteractable grab;
    public bool isConnected = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();

        // בהתחלה שיהיה קינטי כדי שלא יזוז סתם
        //rb.isKinematic = true;

        // מחברים אירועים
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    public void OnGrab(SelectEnterEventArgs args)
    {
        // כשאני תופסת ביד → מפסיקים קינטיות
        //rb.isKinematic = false;
    }

    public void OnRelease(SelectExitEventArgs args)
    {
        // כשאני משחררת → חוזר להיות קינטי
        //rb.isKinematic = true;
    }
    public PuzzleGroupHandler CurrentGroup { get; private set; }

    public void SetGroup(PuzzleGroupHandler newGroup)
    {
        CurrentGroup = newGroup;
        transform.SetParent(newGroup.transform);
    }
}
