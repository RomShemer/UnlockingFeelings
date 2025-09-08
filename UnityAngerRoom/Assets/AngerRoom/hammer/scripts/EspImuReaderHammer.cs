using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR;

namespace angerRoom
{
    public class NewMonoBehaviourScript : MonoBehaviour
    {
        public string espUrl = "http://10.100.102.51/sensor";
        public Transform hammer;   
        public float updateInterval = 0.2f; // בקשה כל 50ms (20Hz)
        private Coroutine fetchCoroutine;
        public Transform handReference; // היד של השחקן (לדוגמה RightHand Anchor)
        public Transform hammerGripPoint; 
        public Transform floorAnchor; // עוגן על הרצפה (bookAnchor)

        private Quaternion initialRotationOffset;
        public float rotationSensitivity = 0.2f;

        [Tooltip("כדי למנוע חזרה מהירה כשמחזיקים את הכפתור")]
        public float buttonDebounceSeconds = 0.25f;

        private Rigidbody hammertRb;
        private bool isTracking = false;


        InputDevice rightController;
        bool isSensor = false;
        bool lastAPressed = false;
        float lastToggleTime = -999f;

        void Start()
        {
            hammertRb = hammer.GetComponent<Rigidbody>();
            if (hammertRb == null)
            {
                hammertRb = hammer.gameObject.AddComponent<Rigidbody>();
            }

            initialRotationOffset = Quaternion.Inverse(handReference.rotation) * hammer.rotation;

        }

        void Awake()
        {
            FindRightController();
        }

        void Update()
        {
            if (rightController.isValid &&
                rightController.TryGetFeatureValue(CommonUsages.primaryButton, out bool aPressed))
            {
                Debug.Log("Primary button (A) pressed: " + aPressed);

                if (aPressed && !lastAPressed && Time.unscaledTime - lastToggleTime >= buttonDebounceSeconds)
                {
                    isSensor = !isSensor; // הופך ON/OFF בכל לחיצה
                    lastToggleTime = Time.unscaledTime;

                    if (isSensor)
                    {
                        fetchCoroutine = StartCoroutine(FetchSensorLoop());
                    }
                    else
                    {
                        if (fetchCoroutine != null)
                            StopCoroutine(fetchCoroutine);

                        hammer.rotation = Quaternion.Euler(-90f, 0, hammer.rotation.z);

                        hammer.position = new Vector3(hammer.position.x, 0.09859177f, hammer.position.z);

                    }
                }

                lastAPressed = aPressed;
            }
            else
            {
                lastAPressed = false;
            }


        }

        void FindRightController()
        {
            var list = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, list);
            if (list.Count > 0)
            {
                rightController = list[0];
                Debug.Log("Right controller found: " + rightController.name);
            }
        }

        public void SetHandReference(Transform newHand)
        {
            handReference = newHand;
        }


        IEnumerator FetchSensorLoop()
        {
            while (isSensor)
            {
                yield return StartCoroutine(GetSensorData());
                yield return new WaitForSeconds(updateInterval);
            }
        }

        //IEnumerator GetSensorData()
        //{
        //    using (UnityWebRequest   www = UnityWebRequest.Get(espUrl))
        //    {
        //        www.timeout = 2; // קיצור טיימאאוט למניעת תקיעות
        //        Debug.Log("Sending request to: " + espUrl);

        //        yield return www.SendWebRequest();

        //        if (www.result == UnityWebRequest.Result.Success)
        //        {
        //            Debug.Log("Response: " + www.downloadHandler.text);
        //            string json = www.downloadHandler.text;

        //            Debug.Log("Response: " + www.downloadHandler.text);

        //            SensorData data = JsonUtility.FromJson<SensorData>(json);

        //            float deltaTime = updateInterval;

        //            Quaternion targetRotation = Quaternion.Euler(
        //                               data.pitch,   // X-axis
        //                               data.yaw,     // Y-axis
        //                               data.roll     // Z-axis
        //                           );

        //             flashlight.localRotation = Quaternion.Slerp(
        //                flashlight.localRotation,
        //                targetRotation,
        //                rotationSensitivity
        //            );

        //            // הוספת הסיבוב היחסי לרוטציה הקיימת
        //            //flashlight.localRotation *= Quaternion.Euler(targetRotation);
        //            Debug.Log($"Δrot: {targetRotation} | New rot: {flashlight.localRotation.eulerAngles}");

        //            // שימוש בג'יירו להזזת הפנס
        //            //flashlight.localRotation = Quaternion.Euler(
        //            //    data.gx,   // Pitch
        //            //    data.gy,   // Yaw
        //            //    data.gz    // Roll
        //            //);
        //        }
        //        else
        //        {
        //            Debug.LogWarning("Failed to fetch sensor data: " + www.error);
        //        }
        //    }
        //}

        //IEnumerator GetSensorData()
        //{
        //    using (UnityWebRequest www = UnityWebRequest.Get(espUrl))
        //    {
        //        www.timeout = 2;
        //        yield return www.SendWebRequest();

        //        if (www.result == UnityWebRequest.Result.Success)
        //        {
        //            Debug.Log("Sending request to: " + espUrl);

        //            string json = www.downloadHandler.text;

        //            Debug.Log("Response: " + www.downloadHandler.text);

        //            SensorData data = JsonUtility.FromJson<SensorData>(json);

        //            // המרה של נתוני הסיבוב לזוויות יחסיות מנורמלות
        //            float deltaPitch = Mathf.Deg2Rad * data.pitch * rotationSensitivity;
        //            float deltaYaw = Mathf.Deg2Rad * data.yaw * rotationSensitivity;
        //            float deltaRoll = Mathf.Deg2Rad * data.roll * rotationSensitivity;

        //            // יוצרים שינוי סיבוב יחסי (delta rotation)
        //            Quaternion deltaRotation = Quaternion.Euler(deltaPitch, deltaYaw, deltaRoll);

        //            Debug.Log($"ΔRot: ({deltaPitch}, {deltaYaw}, {deltaRoll}) | New rot: {flashlight.localRotation.eulerAngles}");

        //            // מעדכנים את הרוטציה הנוכחית של הפנס ביחס לdelta
        //            flashlight.localRotation *= deltaRotation;

        //            Debug.Log($"ΔRot: ({deltaPitch}, {deltaYaw}, {deltaRoll}) | New rot: {flashlight.localRotation.eulerAngles}");
        //        }
        //        else
        //        {
        //            Debug.LogWarning("Failed to fetch sensor data: " + www.error);
        //        }
        //    }
        //}

        //IEnumerator GetSensorData()
        //{
        //    using (UnityWebRequest www = UnityWebRequest.Get(espUrl))
        //    {
        //        www.timeout = 5;
        //        yield return www.SendWebRequest();

        //        if (www.result == UnityWebRequest.Result.Success)
        //        {
        //            string json = www.downloadHandler.text;
        //            SensorData data = JsonUtility.FromJson<SensorData>(json);

        //            // נרמול זוויות Pitch, Roll, Yaw לטווח -180 עד 180
        //            float pitch = NormalizeAngle(data.pitch);
        //            float yaw = NormalizeAngle(data.yaw);
        //            float roll = NormalizeAngle(data.roll);

        //            // קובעים סיבוב מוחלט על פי הקריאות של החיישן
        //            Quaternion targetRotation = Quaternion.Euler(pitch, yaw, roll);

        //            // מעבר חלק לסיבוב החדש (Slerp)
        //            flashlight.localRotation = Quaternion.Slerp(
        //                flashlight.localRotation,
        //                targetRotation,
        //                rotationSensitivity
        //            );

        //            Debug.Log($"Target Rot: ({pitch}, {yaw}, {roll}) | New rot: {flashlight.localRotation.eulerAngles}");
        //        }
        //        else
        //        {
        //            Debug.LogWarning("Failed to fetch sensor data: " + www.error);
        //        }
        //    }
        //}

        // פונקציה שמנרמלת כל זווית לטווח [-180, 180]

        //IEnumerator GetSensorData()
        //{
        //    using (UnityWebRequest www = UnityWebRequest.Get(espUrl))
        //    {
        //        www.timeout = 5;
        //        yield return www.SendWebRequest();

        //        if (www.result == UnityWebRequest.Result.Success)
        //        {
        //            string json = www.downloadHandler.text;
        //            SensorData data = JsonUtility.FromJson<SensorData>(json);

        //            float pitch = NormalizeAngle(data.pitch);
        //            float yaw = NormalizeAngle(data.yaw);
        //            float roll = NormalizeAngle(data.roll);

        //            Quaternion imuRotation = Quaternion.Euler(pitch, yaw, roll);

        //            // 1. הצמדת מיקום הפנס ליד
        //            flashlight.position = handReference.position;

        //            // 2. שילוב סיבוב היד עם סיבוב החיישן (סיבוב יחסי)
        //            flashlight.rotation = handReference.rotation * imuRotation;

        //            Debug.Log($"Target Rot: ({pitch}, {yaw}, {roll}) | New rot: {flashlight.rotation.eulerAngles}");
        //        }
        //        else
        //        {
        //            Debug.LogWarning("Failed to fetch sensor data: " + www.error);
        //        }
        //    }
        //}
        IEnumerator GetSensorData()
        {
            using (UnityWebRequest www = UnityWebRequest.Get(espUrl))
            {
                www.timeout = 5;
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    string json = www.downloadHandler.text;
                    SensorData data = JsonUtility.FromJson<SensorData>(json);

                    float rawPitch = NormalizeAngle(data.pitch);
                    float pitch = Mathf.Clamp(rawPitch, -60f, 60f);  // 🔥 לא יותר מדי למטה או למעלה
                                                                     //float pitch = NormalizeAngle(data.pitch);

                    float yaw = NormalizeAngle(data.yaw);
                    float roll = NormalizeAngle(data.roll);

                    // הסיבוב של החיישן עצמו
                    //Quaternion imuRotation = Quaternion.Euler(pitch, yaw, 0f);
                    Quaternion imuRotation = Quaternion.Euler(0f, yaw, -pitch);

                    Vector3 gripOffset = hammer.position - hammerGripPoint.position;
                    hammer.position = handReference.position + handReference.rotation * gripOffset;

                    if (hammer.position.y < 0.5f)
                    {
                        hammer.position = new Vector3(
                            hammer.position.x,
                            0.5f,
                            hammer.position.z
                        );
                    }
                    // מיקום: הצמדה ליד
                    //flashlight.position = handReference.position;

                    // סיבוב: יחסית ליד
                    //flashlight.rotation = handReference.rotation * imuRotation;
                    hammer.rotation = handReference.rotation * imuRotation * initialRotationOffset;


                    Debug.Log($"IMU: ({pitch}, {yaw}, {roll}) | Final Rotation: {hammer.rotation.eulerAngles}");
                }
                else
                {
                    Debug.LogWarning("Failed to fetch sensor data: " + www.error);
                }
            }
        }

        private float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f)
                angle -= 360f;
            else if (angle < -180f)
                angle += 360f;
            return angle;
        }


        [System.Serializable]
        public class SensorData
        {
            public float pitch;
            public float roll;
            public float yaw;
        }
    }
}

