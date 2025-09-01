////using UnityEngine;
////using UnityEngine.XR;

////public class Shooting : MonoBehaviour
////{
////    public GameObject shoot;
////    public Transform firePoint;
////    public XRNode controllerNode = XRNode.RightHand;

////    private InputDevice controller;

////    void Start()
////    {
////        controller = InputDevices.GetDeviceAtXRNode(controllerNode);
////    }

////    void Update()
////    {
////        if (!controller.isValid)
////        {
////            controller = InputDevices.GetDeviceAtXRNode(controllerNode);
////        }

////        bool triggerPressed = false;
////        if (controller.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed) && triggerPressed)
////        {
////            Shoot();
////        }
////    }

////    void Shoot()
////    {
////        GameObject bullet = Instantiate(shoot, firePoint.position, firePoint.rotation);
////        Rigidbody rb = bullet.GetComponent<Rigidbody>();
////        if (rb != null)
////        {
////            rb.linearVelocity = firePoint.forward * 10f;
////        }

////        Debug.Log("Shoot!");
////    }
////}

//using UnityEngine;
//using UnityEngine.XR;

//public class Shooting : MonoBehaviour
//{
//    public GameObject shoot; // prefab של הכדור
//    public Transform controllerTransform; // זה השלט עצמו
//    public XRNode controllerNode = XRNode.RightHand;

//    private InputDevice controller;

//    void Start()
//    {
//        controller = InputDevices.GetDeviceAtXRNode(controllerNode);
//    }

//    void Update()
//    {
//        if (!controller.isValid)
//        {
//            controller = InputDevices.GetDeviceAtXRNode(controllerNode);
//        }

//        if (controller.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerPressed) && triggerPressed)
//        {
//            Shoot();
//        }
//    }

//    void Shoot()
//    {
//        GameObject bullet = Instantiate(shoot, controllerTransform.position, controllerTransform.rotation);
//        Rigidbody rb = bullet.GetComponent<Rigidbody>();
//        if (rb != null)
//        {
//            rb.linearVelocity = controllerTransform.forward * 10f;
//        }
//    }
//}

using UnityEngine;
using UnityEngine.XR;

public class Shooting : MonoBehaviour
{
    public GameObject shoot; // Prefab של הכדור
    public Transform controllerTransform; // Transform של השלט
    public XRNode controllerNode = XRNode.RightHand;

    private InputDevice controller;

    // מצלמה ראשית של השחקן
    private GameObject mainCamera;

    void Start()
    {
        controller = InputDevices.GetDeviceAtXRNode(controllerNode);
        mainCamera = Camera.main?.gameObject; // נאתר את המצלמה הראשית
    }

    void Update()
    {
        if (!controller.isValid)
        {
            controller = InputDevices.GetDeviceAtXRNode(controllerNode);
        }

        if (controller.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerPressed) && triggerPressed)
        {
            Shoot();
        }
    }

    void Shoot()
    {
        // מרחיק את הכדור קצת קדימה מהשלט
        Vector3 spawnPosition = controllerTransform.position + controllerTransform.forward * 0.3f;

        GameObject bullet = Instantiate(shoot, spawnPosition, controllerTransform.rotation);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = controllerTransform.forward * 10f;
        }

        // אם יש מצלמה ראשית, נתעלם מהתנגשות עם הקוליידרים שלה והילדים שלה
        if (mainCamera != null)
        {
            Collider bulletCol = bullet.GetComponent<Collider>();
            foreach (Collider camCol in mainCamera.GetComponentsInChildren<Collider>())
            {
                if (bulletCol != null && camCol != null)
                {
                    Physics.IgnoreCollision(bulletCol, camCol);
                }
            }
        }
    }
}
