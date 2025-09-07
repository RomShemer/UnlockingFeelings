using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR;

namespace angerRoom
{
    /// <summary>
    /// מושך Orientation מה-ESP ומצמיד את הפטיש ליד, עם נעילת Roll ואילוץ פיזיקלי ב-FixedUpdate.
    /// - שימי על GameObject מנהל (למשל: "HammerIMU Driver").
    /// - חברי: handReference (יד/שלט), hammer (אב של הפטיש), hammerGripPoint (נקודת אחיזה על הידית).
    /// - ודאי של- hammer יש Rigidbody isKinematic=true + Colliders (ראש הפטיש: BoxCollider IsTrigger=true עם HammerTrigger).
    /// </summary>
    public class HammerIMUSimpleDriver : MonoBehaviour
    {
        [Header("ESP")]
        [Tooltip("ה-URL של ה-ESP. לדוגמה: http://192.168.4.1/hammer או http://172.20.10.2/hammer")]
        public string espUrl = "http://172.20.10.2/hammer";

        [Tooltip("כל כמה זמן למשוך נתונים מהחיישן (שניות). 0.05 ≈ 20Hz")]
        public float updateInterval = 0.05f;

        [Header("Scene Refs")]
        public Transform hammer;            // האובייקט של הפטיש (השורש שמזיז את כולו)
        public Transform handReference;     // ה-Anchor של היד שמחזיקה את החיישן (Left/Right Hand Anchor)
        public Transform hammerGripPoint;   // נקודת אחיזה בתוך הפטיש (Empty על הידית)
        public Transform floorAnchor;       // אופציונלי: עוגן על הרצפה כשמכבים את הקריאה

        [Header("Tuning")]
        [Range(0f, 1f)] public float rotationSensitivity = 0.2f; // Slerp לרוטציה
        public float minY = 0.5f;                                 // גובה מינימלי כדי לא “לצלול”

        [Header("Toggle (A button)")]
        [Tooltip("דבונס ללחיצה על A כדי להחליף מצב ON/OFF")]
        public float buttonDebounceSeconds = 0.25f;

        // ----- runtime -----
        private Rigidbody hammerRb;
        private Quaternion initialRotationOffset;
        private Coroutine fetchCoroutine;

        private InputDevice rightController;
        private bool isSensor = false;
        private bool lastAPressed = false;
        private float lastToggleTime = -999f;

        void Awake()
        {
            // מציאת שלט ימין (כמו בקוד שלך)
            var list = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, list);
            if (list.Count > 0) rightController = list[0];
        }

        void Start()
        {
            if (!hammer) hammer = transform;

            hammerRb = hammer.GetComponent<Rigidbody>();
            if (!hammerRb) hammerRb = hammer.gameObject.AddComponent<Rigidbody>();

            // פיזיקה: אותו סגנון כמו שהיה
            hammerRb.isKinematic = true;
            hammerRb.interpolation = RigidbodyInterpolation.Interpolate;
            hammerRb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            // מכיילים אופסט רוטציה ראשוני ביחס ליד
            if (handReference)
                initialRotationOffset = Quaternion.Inverse(handReference.rotation) * hammer.rotation;
            else
                initialRotationOffset = Quaternion.identity;
        }

        void Update()
        {
            // טוגול עם כפתור A, בדיוק כמו בקוד שעבד לך
            if (rightController.isValid &&
                rightController.TryGetFeatureValue(CommonUsages.primaryButton, out bool aPressed))
            {
                if (aPressed && !lastAPressed && Time.unscaledTime - lastToggleTime >= buttonDebounceSeconds)
                {
                    isSensor = !isSensor;
                    lastToggleTime = Time.unscaledTime;

                    if (isSensor)
                    {
                        if (fetchCoroutine != null) StopCoroutine(fetchCoroutine);
                        fetchCoroutine = StartCoroutine(FetchSensorLoop());
                    }
                    else
                    {
                        if (fetchCoroutine != null) StopCoroutine(fetchCoroutine);

                        // כשמכבים – מניחים את הפטיש “שפוי” ברצפה אם יש עוגן
                        if (floorAnchor)
                        {
                            hammer.position = new Vector3(floorAnchor.position.x,
                                                          Mathf.Max(floorAnchor.position.y, minY),
                                                          floorAnchor.position.z);
                            hammer.rotation = floorAnchor.rotation;
                        }
                        else
                        {
                            // ברירת מחדל ידנית קטנה, כמו אצלך
                            hammer.rotation = Quaternion.Euler(-90f, 0f, hammer.rotation.eulerAngles.z);
                            hammer.position = new Vector3(hammer.position.x, minY, hammer.position.z);
                        }
                    }
                }
                lastAPressed = aPressed;
            }
            else
            {
                lastAPressed = false;
            }
        }

        IEnumerator FetchSensorLoop()
        {
            while (isSensor)
            {
                yield return StartCoroutine(GetSensorData());
                yield return new WaitForSeconds(updateInterval);
            }
        }

        IEnumerator GetSensorData()
        {
            if (string.IsNullOrEmpty(espUrl)) yield break;

            using (UnityWebRequest www = UnityWebRequest.Get(espUrl))
            {
                www.timeout = 5; // כמו שהיה
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    string json = www.downloadHandler.text;
                    SensorData data = null;
                    try { data = JsonUtility.FromJson<SensorData>(json); }
                    catch { data = null; }

                    if (data == null) yield break;

                    // נרמול/קלמפ כמו בקוד שעבד לך
                    float rawPitch = NormalizeAngle(data.pitch);
                    float pitch = Mathf.Clamp(rawPitch, -60f, 60f);
                    float yaw = NormalizeAngle(data.yaw);
                    float roll = NormalizeAngle(data.roll);

                    // סיבוב מה-IMU (בדוקה אצלך)
                    // משתמשים ב-yaw וב-pitch, ומבטלים roll כדי לשמור יציבות
                    Quaternion imuRotation = Quaternion.Euler(0f, yaw, -pitch);

                    // מיקום: להצמיד ליד עם אופסט אחיזה אמיתי
                    if (handReference)
                    {
                        Vector3 gripOffset = (hammerGripPoint ? (hammer.position - hammerGripPoint.position) : Vector3.zero);
                        Vector3 targetPos = handReference.position + handReference.rotation * gripOffset;
                        if (targetPos.y < minY) targetPos.y = minY;
                        hammer.position = targetPos;

                        // רוטציה: יד * imu * offset כיול
                        Quaternion targetRot = handReference.rotation * imuRotation * initialRotationOffset;
                        hammer.rotation = Quaternion.Slerp(hammer.rotation, targetRot, rotationSensitivity);
                    }

                    // דיבאג (אופציונלי)
                    // Debug.Log($"IMU: ({pitch:0.0},{yaw:0.0},{roll:0.0}) | rot={hammer.rotation.eulerAngles}");
                }
                else
                {
                    Debug.LogWarning("HammerImuReader: Failed to fetch sensor data: " + www.error);
                }
            }
        }

        // כלי קטן לנרמול זוויות (כמו אצלך)
        private float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            else if (angle < -180f) angle += 360f;
            return angle;
        }

        // מבנה JSON – מספיק שלשדות האלו יהיו ב-/hammer; אם יש עוד שדות – JsonUtility יתעלם
        [System.Serializable]
        public class SensorData
        {
            public float pitch;
            public float roll;
            public float yaw;
            // שדות שאולי יגיעו – לא חובה להשתמש בהם:
            public float ax, ay, az;
            public float gx, gy, gz;
        }
    }
}
