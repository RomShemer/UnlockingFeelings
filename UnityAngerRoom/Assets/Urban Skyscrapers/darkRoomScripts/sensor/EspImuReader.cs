using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR;

namespace fearRoom
{
    public class EspImuReader : MonoBehaviour
    {
        public string espUrl = "http://10.100.102.51/sensor";
        public Transform flashlight;   // האובייקט של הפנס בחדר
        public float updateInterval = 0.2f; // בקשה כל 50ms (20Hz)
        private Coroutine fetchCoroutine;
        public Transform handReference; // היד של השחקן (לדוגמה RightHand Anchor)
        public Transform flashlightGripPoint; // תגדירי את זה באינספקטור (נקודת האחיזה בתוך הפנס)
        public Transform floorAnchor; // עוגן על הרצפה (bookAnchor)

        private Quaternion initialRotationOffset;
        public float rotationSensitivity = 0.2f;

        [Tooltip("כדי למנוע חזרה מהירה כשמחזיקים את הכפתור")]
        public float buttonDebounceSeconds = 0.25f;

        private Rigidbody flashlightRb;
        private bool isTracking = false;


        InputDevice rightController;
        bool isSensor = false;
        bool lastAPressed = false;
        float lastToggleTime = -999f;

        void Start()
        {
            flashlightRb = flashlight.GetComponent<Rigidbody>();
            if (flashlightRb == null)
            {
                flashlightRb = flashlight.gameObject.AddComponent<Rigidbody>();
            }

            initialRotationOffset = Quaternion.Inverse(handReference.rotation) * flashlight.rotation;

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

                        flashlight.rotation = Quaternion.Euler(-90f, 0, flashlight.rotation.z);

                        flashlight.position = new Vector3(flashlight.position.x, 0.09859177f, flashlight.position.z);

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

                    Vector3 gripOffset = flashlight.position - flashlightGripPoint.position;
                    flashlight.position = handReference.position + handReference.rotation * gripOffset;

                    if (flashlight.position.y < 0.5f)
                    {
                        flashlight.position = new Vector3(
                            flashlight.position.x,
                            0.5f,
                            flashlight.position.z
                        );
                    }
                    // מיקום: הצמדה ליד
                    //flashlight.position = handReference.position;

                    // סיבוב: יחסית ליד
                    //flashlight.rotation = handReference.rotation * imuRotation;
                    flashlight.rotation = handReference.rotation * imuRotation * initialRotationOffset;


                    Debug.Log($"IMU: ({pitch}, {yaw}, {roll}) | Final Rotation: {flashlight.rotation.eulerAngles}");
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

//using System.Collections;
//using UnityEngine;
//using UnityEngine.Networking;

//namespace fearRoom
//{
//    public class EspImuReader : MonoBehaviour
//    {
//        [Header("Network")]
//        public string espUrl = "http://10.100.102.51/sensor";
//        [Range(0.02f, 0.2f)] public float updateInterval = 0.05f;

//        [Header("Targets")]
//        public Transform flashlight;          // שורש הספר
//        public Transform handReference;       // RightHandAnchor / LeftHandAnchor
//        public Transform flashlightGripPoint; // ילד בתוך הספר = נקודת אחיזה
//        public Transform floorAnchor;         // עוגן על הרצפה (bookAnchor)

//        [Header("Tuning")]
//        [Range(0f, 1f)] public float rotationSensitivity = 0.2f;
//        public float minY = 0.5f;

//        [Header("Attach gate (IMU motion to attach to hand)")]
//        public float attachGyroDegPerSec = 40f;  // סף תנועה לפי ג'יירו
//        public float attachAccelDeltaG = 0.25f; // סף תנועה לפי |a|-1g

//        Rigidbody rb;
//        Coroutine fetchLoop;

//        enum ControlState { OnFloor, AttachedToHand, PodiumLocked }
//        ControlState state = ControlState.OnFloor;
//        Transform lockedAnchor; // לעיגון לפודיום

//        // הטיה מה-IMU (pitch/roll בלבד)
//        Quaternion targetImuTilt = Quaternion.identity;

//        // החלקה לחיישנים (דיאגנוסטיקה/שער)
//        float smoothedGyro = 0f, smoothedAccelDelta = 0f;

//        // Offset רוטציה בזמן ההצמדה ליד (כדי למנוע "קפיצה" בזווית)
//        Quaternion initialRotationOffset = Quaternion.identity;

//        void Awake()
//        {
//            if (!flashlightGripPoint) flashlightGripPoint = flashlight;

//            rb = flashlight.GetComponent<Rigidbody>();
//            if (rb)
//            {
//                rb.isKinematic = true;
//                rb.useGravity = false;
//            }
//        }

//        void Start()
//        {
//            // מצב פתיחה: דבוק לרצפה
//            SnapToFloor();
//            fetchLoop = StartCoroutine(FetchSensorLoop());
//        }

//        void OnDisable()
//        {
//            if (fetchLoop != null) StopCoroutine(fetchLoop);
//        }

//        IEnumerator FetchSensorLoop()
//        {
//            while (true)
//            {
//                if (state != ControlState.PodiumLocked)
//                    yield return GetSensorData();
//                else
//                    yield return null;

//                yield return new WaitForSeconds(updateInterval);
//            }
//        }

//        IEnumerator GetSensorData()
//        {
//            using (var www = UnityWebRequest.Get(espUrl))
//            {
//                www.timeout = 5;
//                yield return www.SendWebRequest();
//                if (www.result != UnityWebRequest.Result.Success) yield break;

//                var data = JsonUtility.FromJson<SensorData>(www.downloadHandler.text);

//                // --- tilt מה-IMU ---
//                float pitch = Mathf.Clamp(NormalizeAngle(data.pitch), -60f, 60f);
//                float roll = NormalizeAngle(data.roll);
//                var imuTilt = Quaternion.Euler(roll, 0f, -pitch);
//                targetImuTilt = Quaternion.Slerp(targetImuTilt, imuTilt, rotationSensitivity);

//                // --- שער חיבור ליד (רק כשעל הרצפה) ---
//                if (state == ControlState.OnFloor)
//                {
//                    float gyroMag = Mathf.Abs(data.gx) + Mathf.Abs(data.gy) + Mathf.Abs(data.gz);
//                    float accelMag = Mathf.Sqrt(data.ax * data.ax + data.ay * data.ay + data.az * data.az);
//                    float accelDelta = Mathf.Abs(accelMag - 1f);

//                    smoothedGyro = Mathf.Lerp(smoothedGyro, gyroMag, 0.3f);
//                    smoothedAccelDelta = Mathf.Lerp(smoothedAccelDelta, accelDelta, 0.3f);

//                    if (smoothedGyro > attachGyroDegPerSec || smoothedAccelDelta > attachAccelDeltaG)
//                        AttachToHand();
//                }
//            }
//        }

//        void FixedUpdate()
//        {
//            if (!flashlight) return;

//            if (state == ControlState.PodiumLocked)
//            {
//                if (!lockedAnchor) return;
//                Vector3 p = lockedAnchor.position; if (p.y < minY) p.y = minY;
//                ApplyPose(p, lockedAnchor.rotation);
//                return;
//            }

//            if (state == ControlState.OnFloor)
//            {
//                if (!floorAnchor) return;
//                Vector3 p = floorAnchor.position; if (p.y < minY) p.y = minY;
//                ApplyPose(p, floorAnchor.rotation);
//                return;
//            }

//            // state == AttachedToHand
//            if (!handReference || !flashlightGripPoint) return;

//            // yaw מהיד/ראש (מישור אופקי)
//            Quaternion handYaw;
//            Vector3 flatFwd = handReference.forward; flatFwd.y = 0f;
//            handYaw = flatFwd.sqrMagnitude > 1e-6f
//                ? Quaternion.LookRotation(flatFwd.normalized, Vector3.up)
//                : Quaternion.Euler(0f, handReference.eulerAngles.y, 0f);

//            // רוטציה סופית: yaw מהיד * הטיית IMU * offset כיול
//            Quaternion targetRot = handYaw * targetImuTilt * initialRotationOffset;

//            // מיקום: לגרום ל-GripPoint להתלכד עם היד
//            Vector3 targetPos = handReference.position - (targetRot * flashlightGripPoint.localPosition);
//            if (targetPos.y < minY) targetPos.y = minY;

//            ApplyPose(targetPos, targetRot);
//        }

//        // ---------- מצבי מעבר ----------
//        void AttachToHand()
//        {
//            state = ControlState.AttachedToHand;

//            // כיול offset כך שהסיבוב לא יקפוץ
//            if (handReference)
//                initialRotationOffset = Quaternion.Inverse(handReference.rotation) * flashlight.rotation;

//            // שחרר קפיאות אם היו
//            if (rb)
//            {
//                rb.constraints = RigidbodyConstraints.None;
//                rb.isKinematic = true;
//            }
//            // Debug.Log("[Book] Attached to hand.");
//        }

//        public void SnapToPodium(Transform anchor)
//        {
//            lockedAnchor = anchor;
//            state = ControlState.PodiumLocked;

//            if (rb)
//            {
//                rb.isKinematic = true;
//                rb.linearVelocity = Vector3.zero;
//                rb.angularVelocity = Vector3.zero;
//                rb.constraints = RigidbodyConstraints.FreezeAll;
//            }

//            // הצבה מידית (לא חייב, אבל נעים)
//            if (lockedAnchor) ApplyPose(lockedAnchor.position, lockedAnchor.rotation);
//            // Debug.Log("[Book] Locked to podium.");
//        }

//        void SnapToFloor()
//        {
//            state = ControlState.OnFloor;

//            if (rb)
//            {
//                rb.isKinematic = true;
//                rb.linearVelocity = Vector3.zero;
//                rb.angularVelocity = Vector3.zero;
//                rb.constraints = RigidbodyConstraints.FreezeAll;
//            }

//            if (floorAnchor) ApplyPose(floorAnchor.position, floorAnchor.rotation);
//            // Debug.Log("[Book] Snapped to floor.");
//        }

//        // ---------- עזר ----------
//        void ApplyPose(Vector3 pos, Quaternion rot)
//        {
//            if (rb) { rb.MovePosition(pos); rb.MoveRotation(rot); }
//            else { flashlight.position = pos; flashlight.rotation = rot; }
//        }

//        static float NormalizeAngle(float angle)
//        {
//            angle %= 360f;
//            if (angle > 180f) angle -= 360f;
//            else if (angle < -180f) angle += 360f;
//            return angle;
//        }

//        [System.Serializable]
//        public class SensorData
//        {
//            public float pitch, roll, yaw;
//            public float ax, ay, az;
//            public float gx, gy, gz;
//        }
//    }

//}

