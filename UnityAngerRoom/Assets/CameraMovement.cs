using UnityEngine;

public class OVRPlayerMovement : MonoBehaviour
{
    public float moveSpeed = 2.0f;
    public float turnSpeed = 60f;

    private CharacterController characterController;
    private Transform centerEye;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        centerEye = GameObject.Find("CenterEyeAnchor").transform;
    }

    void Update()
    {
        // תנועה עם סטיק שמאלי
        Vector2 input = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        Vector3 moveDirection = centerEye.forward * input.y + centerEye.right * input.x;
        moveDirection.y = 0;
        characterController.Move(moveDirection * moveSpeed * Time.deltaTime);

        // סיבוב עם סטיק ימני
        Vector2 turnInput = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        transform.Rotate(0, turnInput.x * turnSpeed * Time.deltaTime, 0);
    }
}
