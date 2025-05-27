using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CameraMovement : MonoBehaviour
{
    public Camera playerCamera;
    public float walkSpeed = 6f;
    public float runSpeed = 12f;
    public float jumpPower = 7f;
    public float gravity = 10f;
    public float lookSpeed = 2f;
    public float lookXLimit = 45f;
    public float defaultHeight = 2f;
    //public float crouchHeight = 1f;
    //public float crouchSpeed = 3f;

    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;
    private CharacterController characterController;

    private bool canMove = true;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        
        //////
        characterController.height = defaultHeight;
        characterController.center = new Vector3(0, defaultHeight / 2, 0);
        //////
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
{
    // תנועה עם הסטיק השמאלי (Horizontal ו-Vertical)
    float moveX = Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f ? Input.GetAxis("Horizontal") : 0f;
    float moveZ = Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f ? Input.GetAxis("Vertical") : 0f;

    // תנועה קדימה/אחורה ולצדדים
    Vector3 forward = transform.TransformDirection(Vector3.forward);
    Vector3 right = transform.TransformDirection(Vector3.right);

    bool isRunning = Input.GetKey(KeyCode.LeftShift);
    float curSpeedX = canMove ? (isRunning ? runSpeed : walkSpeed) * moveZ : 0;
    float curSpeedY = canMove ? (isRunning ? runSpeed : walkSpeed) * moveX : 0;
    float movementDirectionY = moveDirection.y;
    moveDirection = (forward * curSpeedX) + (right * curSpeedY);

    // קפיצה - רק אם השחקן על הקרקע ולחצן הקפיצה נלחץ
    if (Input.GetButtonDown("Jump") && canMove && characterController.isGrounded)
    {
        moveDirection.y = jumpPower; // קפיצה
    }
    else
    {
        moveDirection.y = movementDirectionY; // שמירה על כוח Y אחר אם לא קופצים
    }

    // כוח המשיכה
    if (!characterController.isGrounded)
    {
        moveDirection.y -= gravity * Time.deltaTime; // אם השחקן באוויר, הוספת כוח המשיכה
    }

    // כריעה
    /*if (Input.GetKey(KeyCode.R) && canMove)
    {
        characterController.height = crouchHeight;
        characterController.center = new Vector3(0, crouchHeight / 2, 0);
        walkSpeed = crouchSpeed;
        runSpeed = crouchSpeed;
    }
    else
    {
        characterController.height = defaultHeight;
        characterController.center = new Vector3(0, defaultHeight / 2, 0);
        walkSpeed = 3f;
        runSpeed = 12f;
    }*/

    // תנועה
    characterController.Move(moveDirection * Time.deltaTime);

    // סיבוב עם הסטיק הימני
    if (canMove)
    {
        // סיבוב למעלה ולמטה עם הסטיק הימני (Right Stick Vertical)
        rotationX += -Input.GetAxis("RightStickVertical") * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);

        // סיבוב ימינה ושמאלה עם הסטיק הימני (Right Stick Horizontal)
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("RightStickHorizontal") * lookSpeed, 0);
    }
}

}
