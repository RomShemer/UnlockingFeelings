using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class CollectibleGate : MonoBehaviour
{
    [Header("References")]
    public PuzzleManager puzzleManager; // ���� ����� ����� �����

    [Header("Modes")]
    public bool useActivateToCollect = true; // ����� �� A/X ���� �����
    public bool useHoldToCollect = false;    // ����� N ����� ���� �����
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
        // �� ������� �� �������� ������� �� ����� A/X
        if (useActivateToCollect && grab.isSelected)
        {
            // ����� A = Button.One ��� ����, X = Button.Three ��� ����
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
        // ����� ���� ����� �����
        if (puzzleManager != null)
            puzzleManager.RegisterPieceCollected();

        // ������ ����������� ���� �������� (�����/�����)
        gameObject.SetActive(false); // ������ �� ���� ����
    }
}
