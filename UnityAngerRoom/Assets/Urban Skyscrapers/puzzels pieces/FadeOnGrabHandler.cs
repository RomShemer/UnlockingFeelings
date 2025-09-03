using Oculus.Interaction;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class FadeOnGrabHandler : MonoBehaviour
{
    private Grabbable grabbable;
    private XRGrabInteractable grab;
    private PuzzleFadeOnGrab fader;
    private bool hasFaded = false;


    void Awake()
    {
        grabbable = GetComponent<Grabbable>();
        grab = GetComponent<XRGrabInteractable>();
        fader = GetComponent<PuzzleFadeOnGrab>();
        Debug.Log("Awake: grab = " + grab + ", fader = " + fader);
    }

    void OnEnable()
    {
        if (grab != null)
        {
            grab.selectEntered.AddListener(OnGrabbed);
            Debug.Log("OnEnable: Listener added");
        }
    }

    void OnDisable()
    {

        if (grab != null)
        {
            grab.selectEntered.RemoveListener(OnGrabbed);
            Debug.Log("OnDisable: Listener removed");
        }
    }

    void Update()
    {
        if (!hasFaded && grabbable != null && grabbable.SelectingPointsCount > 0)
        {
            Debug.Log("Oculus Grabbable is grabbed → Triggering fade!");
            hasFaded = true;
            Debug.Log("before call fader.startFadeOut");
            fader?.StartFadeOut();
            Debug.Log("after call fader.startFadeOut");

        }
    }

    public void OnGrabbed(SelectEnterEventArgs args)
    {
        Debug.Log("OnGrabbed called!");
        fader?.StartFadeOut();
    }
}
