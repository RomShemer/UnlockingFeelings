using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class BookVariantSwitcher : MonoBehaviour
{
    [Header("Book Objects")]
    public GameObject closedRoot;
    public GameObject[] openRoots;

    public enum InitialState { Closed, Open }
    public InitialState startState = InitialState.Closed;

    [Header("Grab Lock")]
    public bool disableGrabWhenOpen = true;
    public bool disableCollidersWhenOpen = false;

    Rigidbody rb;
    readonly List<Behaviour> grabBehaviours = new List<Behaviour>();
    readonly List<Collider>  colliders      = new List<Collider>();

    void OnEnable()  { Cache(); ApplyState(startState == InitialState.Open); }
    void OnValidate(){ ApplyState(startState == InitialState.Open); }
    void Start()     { ApplyState(startState == InitialState.Open); }

    void Cache()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        grabBehaviours.Clear();
        colliders.Clear();

        var allBehaviours = GetComponentsInChildren<Behaviour>(true);
        foreach (var b in allBehaviours)
        {
            if (!b) continue;
            var n = b.GetType().Name;
            if (n.Contains("Grabbable") || n.Contains("GrabInteractable") || n.Contains("RayInteractable") || n.Contains("RayGrab"))
                grabBehaviours.Add(b);
        }
        colliders.AddRange(GetComponentsInChildren<Collider>(true));
    }

    public void Open()  => ApplyState(true);
    public void Close() => ApplyState(false);

    public void OpenAndHardLock()
    {
        ApplyState(true);
        if (rb && Application.isPlaying)
        {
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    public void ApplyState(bool open)
    {
        if (closedRoot) closedRoot.SetActive(!open);
        if (openRoots != null) foreach (var g in openRoots) if (g) g.SetActive(open);

        if (disableGrabWhenOpen)
            foreach (var beh in grabBehaviours) if (beh) beh.enabled = !open;

        if (disableCollidersWhenOpen)
            foreach (var c in colliders) if (c) c.enabled = !open;

        if (Application.isPlaying && rb)
            rb.isKinematic = open;
    }
}
