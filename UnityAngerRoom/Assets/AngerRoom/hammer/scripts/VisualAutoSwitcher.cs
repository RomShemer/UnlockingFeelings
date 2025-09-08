using UnityEngine;

/// בודק בכל פריים מה מחובר/נעקב (שמאל/ימין) ומחליט מה להציג:
/// שלט (Controller Visual) או יד סינתטית (Synthetic Hand).
public class XRVisualAutoSwitcher : MonoBehaviour
{
    [Header("Visuals")]
    public GameObject leftControllerVisual;
    public GameObject rightControllerVisual;
    public GameObject leftSyntheticHand;
    public GameObject rightSyntheticHand;

    [Header("Hand Tracking (for IsTracked)")]
    public OVRHand leftOVRHand;   // נמצא מתחת LeftHand
    public OVRHand rightOVRHand;  // נמצא מתחת RightHand

    [Header("Preference")]
    [Tooltip("אם מחוברים גם שלטים וגם ידיים – מה להעדיף?")]
    public bool preferControllers = true;  // true=להעדיף שלטים, false=להעדיף ידיים

    void Update()
    {
        // אילו בקרים מחוברים?
        var connected = OVRInput.GetConnectedControllers();

        bool leftCtrlConnected = (connected & OVRInput.Controller.LTouch) != 0;
        bool rightCtrlConnected = (connected & OVRInput.Controller.RTouch) != 0;

        // האם יש טרקינג למיקום/כיוון של הבקר כרגע?
        bool leftCtrlTracked = leftCtrlConnected &&
                                OVRInput.GetControllerPositionTracked(OVRInput.Controller.LTouch) &&
                                OVRInput.GetControllerOrientationTracked(OVRInput.Controller.LTouch);

        bool rightCtrlTracked = rightCtrlConnected &&
                                OVRInput.GetControllerPositionTracked(OVRInput.Controller.RTouch) &&
                                OVRInput.GetControllerOrientationTracked(OVRInput.Controller.RTouch);

        // האם הידיים ב-Hand Tracking באמת מזוהות כרגע?
        bool leftHandTracked = leftOVRHand && leftOVRHand.IsTracked;
        bool rightHandTracked = rightOVRHand && rightOVRHand.IsTracked;

        // החלפה לכל צד בנפרד
        UpdateSide(leftCtrlTracked, leftHandTracked, leftControllerVisual, leftSyntheticHand);
        UpdateSide(rightCtrlTracked, rightHandTracked, rightControllerVisual, rightSyntheticHand);

        // (רשות) לוג של ה"Active Controller" הכללי
        var active = OVRInput.GetActiveController(); // יחזיר LTouch/RTouch/Hands/None...
        // Debug.Log("Active controller: " + active);
    }

    void UpdateSide(bool controllerTracked, bool handTracked,
                    GameObject controllerGO, GameObject handGO)
    {
        if (preferControllers)
        {
            // מציג שלט אם הוא באמת בטרקינג; אחרת מציג יד אם יש טרקינג יד
            if (controllerGO) controllerGO.SetActive(controllerTracked);
            if (handGO) handGO.SetActive(!controllerTracked && handTracked);
        }
        else
        {
            // מעדיף יד: מציג יד אם יש טרקינג; אחרת מציג שלט אם הוא בטרקינג
            if (handGO) handGO.SetActive(handTracked);
            if (controllerGO) controllerGO.SetActive(!handTracked && controllerTracked);
        }
    }
}
