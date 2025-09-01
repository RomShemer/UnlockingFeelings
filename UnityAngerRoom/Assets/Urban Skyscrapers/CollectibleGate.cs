using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class CollectibleGate : MonoBehaviour
{
    [Header("References")]
    public PuzzleManager puzzleManager; // מנהל שמונה חלקים בסצנה

    [Header("Modes")]
    public bool useActivateToCollect = true; // לחיצה על A/X בזמן אחיזה
    public bool useHoldToCollect = false;    // החזקה N שניות בזמן אחיזה
    public float holdSeconds = 1.0f;

    XRGrabInteractable grab;
    Coroutine holdRoutine;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void OnDestroy()
    {
        if (grab == null) return;
        grab.selectEntered.RemoveListener(OnGrab);
        grab.selectExited.RemoveListener(OnRelease);
    }

    void Update()
    {
        // אם מחזיקים את האובייקט ולוחצים על כפתור A/X
        if (useActivateToCollect && grab.isSelected)
        {
            // כפתור A = Button.One ביד ימין, X = Button.Three ביד שמאל
            if (OVRInput.GetDown(OVRInput.Button.One) || OVRInput.GetDown(OVRInput.Button.Three))
            {
                CollectNow();
            }
        }
    }

    void OnGrab(SelectEnterEventArgs _)
    {
        if (useHoldToCollect)
            holdRoutine = StartCoroutine(HoldToCollect());
    }

    void OnRelease(SelectExitEventArgs _)
    {
        if (holdRoutine != null)
            StopCoroutine(holdRoutine);
        holdRoutine = null;
    }

    IEnumerator HoldToCollect()
    {
        float t = 0f;
        while (t < holdSeconds && grab.isSelected)
        {
            t += Time.deltaTime;
            yield return null;
        }
        if (grab.isSelected) CollectNow();
    }

    public void CollectNow()
    {
        // עדכון מנהל הפאזל בסצנה
        if (puzzleManager != null)
            puzzleManager.RegisterPieceCollected();

        // אפקטים אופציונליים לפני ההיעלמות (סאונד/ניצוץ)
        gameObject.SetActive(false); // להעלים את החלק כרגע
    }
}
