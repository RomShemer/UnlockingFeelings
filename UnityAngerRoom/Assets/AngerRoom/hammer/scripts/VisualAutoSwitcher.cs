using UnityEngine;

/// ���� ��� ����� �� �����/���� (����/����) ������ �� �����:
/// ��� (Controller Visual) �� �� ������� (Synthetic Hand).
public class XRVisualAutoSwitcher : MonoBehaviour
{
    [Header("Visuals")]
    public GameObject leftControllerVisual;
    public GameObject rightControllerVisual;
    public GameObject leftSyntheticHand;
    public GameObject rightSyntheticHand;

    [Header("Hand Tracking (for IsTracked)")]
    public OVRHand leftOVRHand;   // ���� ���� LeftHand
    public OVRHand rightOVRHand;  // ���� ���� RightHand

    [Header("Preference")]
    [Tooltip("�� ������� �� ����� ��� ����� � �� ������?")]
    public bool preferControllers = true;  // true=������ �����, false=������ �����

    void Update()
    {
        // ���� ����� �������?
        var connected = OVRInput.GetConnectedControllers();

        bool leftCtrlConnected = (connected & OVRInput.Controller.LTouch) != 0;
        bool rightCtrlConnected = (connected & OVRInput.Controller.RTouch) != 0;

        // ��� �� ������ ������/����� �� ���� ����?
        bool leftCtrlTracked = leftCtrlConnected &&
                                OVRInput.GetControllerPositionTracked(OVRInput.Controller.LTouch) &&
                                OVRInput.GetControllerOrientationTracked(OVRInput.Controller.LTouch);

        bool rightCtrlTracked = rightCtrlConnected &&
                                OVRInput.GetControllerPositionTracked(OVRInput.Controller.RTouch) &&
                                OVRInput.GetControllerOrientationTracked(OVRInput.Controller.RTouch);

        // ��� ������ �-Hand Tracking ���� ������ ����?
        bool leftHandTracked = leftOVRHand && leftOVRHand.IsTracked;
        bool rightHandTracked = rightOVRHand && rightOVRHand.IsTracked;

        // ����� ��� �� �����
        UpdateSide(leftCtrlTracked, leftHandTracked, leftControllerVisual, leftSyntheticHand);
        UpdateSide(rightCtrlTracked, rightHandTracked, rightControllerVisual, rightSyntheticHand);

        // (����) ��� �� �"Active Controller" �����
        var active = OVRInput.GetActiveController(); // ����� LTouch/RTouch/Hands/None...
        // Debug.Log("Active controller: " + active);
    }

    void UpdateSide(bool controllerTracked, bool handTracked,
                    GameObject controllerGO, GameObject handGO)
    {
        if (preferControllers)
        {
            // ���� ��� �� ��� ���� �������; ���� ���� �� �� �� ������ ��
            if (controllerGO) controllerGO.SetActive(controllerTracked);
            if (handGO) handGO.SetActive(!controllerTracked && handTracked);
        }
        else
        {
            // ����� ��: ���� �� �� �� ������; ���� ���� ��� �� ��� �������
            if (handGO) handGO.SetActive(handTracked);
            if (controllerGO) controllerGO.SetActive(!handTracked && controllerTracked);
        }
    }
}
