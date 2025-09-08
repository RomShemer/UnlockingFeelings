//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.Networking;
//using UnityEngine.XR;

//namespace angerRoom
//{
//    / <summary>
//    / מושך Orientation מה-ESP ומצמיד את הפטיש ליד, עם נעילת Roll ואילוץ פיזיקלי ב-FixedUpdate.
//    / - שימי על GameObject מנהל(למשל: "HammerIMU Driver").
//    / - חברי: handReference(יד/שלט), hammer(אב של הפטיש), hammerGripPoint(נקודת אחיזה על הידית).
//    / - ודאי של- hammer יש Rigidbody isKinematic = true + Colliders(ראש הפטיש: BoxCollider IsTrigger = true עם HammerTrigger).
//    / </ summary >
//    public class HammerIMUSimpleDriver : MonoBehaviour
//    {
//        [Header("ESP")]
//        [Tooltip("ה-URL של ה-ESP. לדוגמה: http://192.168.4.1/hammer או http://172.20.10.2/hammer")]
//        public string espUrl = "http://172.20.10.2/hammer";

//        [Tooltip("כל כמה זמן למשוך נתונים מהחיישן (שניות). 0.05 ≈ 20Hz")]
//        public float updateInterval = 0.05f;

//        [Header("Scene Refs")]
//        public Transform hammer;            // האובייקט של הפטיש (השורש שמזיז את כולו)
//        public Transform handReference;     // ה-Anchor של היד שמחזיקה את החיישן (Left/Right Hand Anchor)
//        public Transform hammerGripPoint;   // נקודת אחיזה בתוך הפטיש (Empty על הידית)
//        public Transform floorAnchor;       // אופציונלי: עוגן על הרצפה כשמכבים את הקריאה

//        [Header("Tuning")]
//        [Range(0f, 1f)] public float rotationSensitivity = 0.2f; // Slerp לרוטציה
//        public float minY = 0.5f;                                 // גובה מינימלי כדי לא “לצלול”

//        [Header("Toggle (A button)")]
//        [Tooltip("דבונס ללחיצה על A כדי להחליף מצב ON/OFF")]
//        public float buttonDebounceSeconds = 0.25f;

//         ----- runtime -----
//        private Rigidbody hammerRb;
//        private Quaternion initialRotationOffset;
//        private Coroutine fetchCoroutine;

//        private InputDevice rightController;
//        private bool isSensor = false;
//        private bool lastAPressed = false;
//        private float lastToggleTime = -999f;

//        void Awake()
//        {
//            מציאת שלט ימין(כמו בקוד שלך)
//            var list = new List<InputDevice>();
//            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, list);
//            if (list.Count > 0) rightController = list[0];
//        }

//        void Start()
//        {
//            if (!hammer) hammer = transform;

//            hammerRb = hammer.GetComponent<Rigidbody>();
//            if (!hammerRb) hammerRb = hammer.gameObject.AddComponent<Rigidbody>();

//            פיזיקה: אותו סגנון כמו שהיה
//            hammerRb.isKinematic = true;
//            hammerRb.interpolation = RigidbodyInterpolation.Interpolate;
//            hammerRb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

//            מכיילים אופסט רוטציה ראשוני ביחס ליד
//            if (handReference)
//                initialRotationOffset = Quaternion.Inverse(handReference.rotation) * hammer.rotation;
//            else
//                initialRotationOffset = Quaternion.identity;
//        }

//        void Update()
//        {
//            טוגול עם כפתור A, בדיוק כמו בקוד שעבד לך
//            if (rightController.isValid &&
//                rightController.TryGetFeatureValue(CommonUsages.primaryButton, out bool aPressed))
//            {
//                if (aPressed && !lastAPressed && Time.unscaledTime - lastToggleTime >= buttonDebounceSeconds)
//                {
//                    isSensor = !isSensor;
//                    lastToggleTime = Time.unscaledTime;

//                    if (isSensor)
//                    {
//                        if (fetchCoroutine != null) StopCoroutine(fetchCoroutine);
//                        fetchCoroutine = StartCoroutine(FetchSensorLoop());
//                    }
//                    else
//                    {
//                        if (fetchCoroutine != null) StopCoroutine(fetchCoroutine);

//                        כשמכבים – מניחים את הפטיש “שפוי” ברצפה אם יש עוגן
//                        if (floorAnchor)
//                        {
//                            hammer.position = new Vector3(floorAnchor.position.x,
//                                                          Mathf.Max(floorAnchor.position.y, minY),
//                                                          floorAnchor.position.z);
//                            hammer.rotation = floorAnchor.rotation;
//                        }
//                        else
//                        {
//                            ברירת מחדל ידנית קטנה, כמו אצלך
//                            hammer.rotation = Quaternion.Euler(-90f, 0f, hammer.rotation.eulerAngles.z);
//                            hammer.position = new Vector3(hammer.position.x, minY, hammer.position.z);
//                        }
//                    }
//                }
//                lastAPressed = aPressed;
//            }
//            else
//            {
//                lastAPressed = false;
//            }
//        }

//        IEnumerator FetchSensorLoop()
//        {
//            while (isSensor)
//            {
//                yield return StartCoroutine(GetSensorData());
//                yield return new WaitForSeconds(updateInterval);
//            }
//        }

//        IEnumerator GetSensorData()
//        {
//            if (string.IsNullOrEmpty(espUrl)) yield break;

//            using (UnityWebRequest www = UnityWebRequest.Get(espUrl))
//            {
//                www.timeout = 5; // כמו שהיה
//                yield return www.SendWebRequest();

//                if (www.result == UnityWebRequest.Result.Success)
//                {
//                    string json = www.downloadHandler.text;
//                    SensorData data = null;
//                    try { data = JsonUtility.FromJson<SensorData>(json); }
//                    catch { data = null; }

//                    if (data == null) yield break;

//                    נרמול / קלמפ כמו בקוד שעבד לך
//                    float rawPitch = NormalizeAngle(data.pitch);
//                    float pitch = Mathf.Clamp(rawPitch, -60f, 60f);
//                    float yaw = NormalizeAngle(data.yaw);
//                    float roll = NormalizeAngle(data.roll);

//                    סיבוב מה-IMU(בדוקה אצלך)
//                     משתמשים ב-yaw וב - pitch, ומבטלים roll כדי לשמור יציבות
//                    Quaternion imuRotation = Quaternion.Euler(0f, yaw, -pitch);

//                מיקום: להצמיד ליד עם אופסט אחיזה אמיתי
//                    if (handReference)
//                    {
//                        Vector3 gripOffset = (hammerGripPoint ? (hammer.position - hammerGripPoint.position) : Vector3.zero);
//                        Vector3 targetPos = handReference.position + handReference.rotation * gripOffset;
//                        if (targetPos.y < minY) targetPos.y = minY;
//                        hammer.position = targetPos;

//                    רוטציה: יד* imu *offset כיול
//                   Quaternion targetRot = handReference.rotation * imuRotation * initialRotationOffset;
//                        hammer.rotation = Quaternion.Slerp(hammer.rotation, targetRot, rotationSensitivity);
//                    }

//                    דיבאג(אופציונלי)
//                     Debug.Log($"IMU: ({pitch:0.0},{yaw:0.0},{roll:0.0}) | rot={hammer.rotation.eulerAngles}");
//                }
//                else
//                {
//                    Debug.LogWarning("HammerImuReader: Failed to fetch sensor data: " + www.error);
//                }
//            }
//        }

//        כלי קטן לנרמול זוויות(כמו אצלך)
//        private float NormalizeAngle(float angle)
//        {
//            angle %= 360f;
//            if (angle > 180f) angle -= 360f;
//            else if (angle < -180f) angle += 360f;
//            return angle;
//        }

//        מבנה JSON – מספיק שלשדות האלו יהיו ב-/hammer; אם יש עוד שדות – JsonUtility יתעלם
//       [System.Serializable]
//        public class SensorData
//        {
//            public float pitch;
//            public float roll;
//            public float yaw;
//            שדות שאולי יגיעו – לא חובה להשתמש בהם:
//            public float ax, ay, az;
//            public float gx, gy, gz;
//        }
//    }
//}

//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.Networking;
//using UnityEngine.XR;

//namespace angerRoom
//{
//    /// <summary>
//    /// מושך Orientation מה-ESP ומצמיד את הפטיש ליד.
//    /// ההזזה בפועל נעשית ב-FixedUpdate עם Rigidbody.MovePosition/MoveRotation,
//    /// כולל הגבלות צעד כדי למנוע "דילוג" דרך קירות ולוודא ש-OnTriggerEnter נורה.
//    /// שימי את הסקריפט על GameObject מנהל (למשל: HammerIMU Driver).
//    /// </summary>
//    [DisallowMultipleComponent]
//    public class HammerIMUSimpleDriver : MonoBehaviour
//    {
//        [Header("ESP")]
//        [Tooltip("לדוגמה: http://192.168.4.1/hammer או http://172.20.10.2/hammer")]
//        public string espUrl = "http://172.20.10.2/hammer";

//        [Tooltip("תדירות דגימה מה-ESP (שניות). 0.05 ≈ 20Hz")]
//        [Range(0.02f, 0.2f)] public float updateInterval = 0.05f;

//        [Header("Scene Refs")]
//        public Transform hammer;            // השורש של הפטיש
//        public Transform handReference;     // עוגן היד/שלט
//        public Transform hammerGripPoint;   // נקודת אחיזה על הידית (Child בתוך הפטיש)
//        public Transform floorAnchor;       // אופציונלי: עוגן לרצפה כשמכבים

//        [Header("Tuning")]
//        [Range(0f, 1f)] public float rotationSensitivity = 0.2f; // Slerp לרוטציה
//        public float minY = 0.5f;                                 // לא לרדת מתחת לרצפה לוגית

//        [Header("Toggle (A button)")]
//        public float buttonDebounceSeconds = 0.25f;

//        [Header("Physics follow (anti-tunneling)")]
//        [Tooltip("מטר מקסימלי לזוז בכל FixedUpdate")]
//        public float maxLinearStep = 0.25f;
//        [Tooltip("מעלות מקסימליות להסתובב בכל FixedUpdate")]
//        public float maxAngularStepDeg = 60f;

//        // ---- Runtime ----
//        Rigidbody hammerRb;
//        Quaternion initialRotationOffset = Quaternion.identity;
//        Coroutine fetchCoroutine;

//        InputDevice rightController;
//        bool isSensor = false;
//        bool lastAPressed = false;
//        float lastToggleTime = -999f;

//        // יעדי תנועה שחושבו מקריאת ה-ESP (מוחלים ב-FixedUpdate)
//        Vector3 _nextPos;
//        Quaternion _nextRot;
//        bool _havePose = false;

//        void Awake()
//        {
//            // מציאת שלט ימין כדי לטוגגל עם A (כמו בקוד שעבד לך)
//            var list = new List<InputDevice>();
//            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, list);
//            if (list.Count > 0) rightController = list[0];
//        }

//        void Start()
//        {
//            if (!hammer) hammer = transform;

//            hammerRb = hammer.GetComponent<Rigidbody>();
//            if (!hammerRb) hammerRb = hammer.gameObject.AddComponent<Rigidbody>();

//            // הגדרות RB לשורש הפטיש
//            hammerRb.isKinematic = true;
//            hammerRb.useGravity = false;
//            hammerRb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
//            hammerRb.interpolation = RigidbodyInterpolation.Interpolate;

//            if (handReference)
//                initialRotationOffset = Quaternion.Inverse(handReference.rotation) * hammer.rotation;

//            // להתחיל עם היעד הנוכחי
//            _nextPos = hammer.position;
//            _nextRot = hammer.rotation;
//            _havePose = true;
//        }

//        void OnDisable()
//        {
//            if (fetchCoroutine != null) StopCoroutine(fetchCoroutine);
//            fetchCoroutine = null;
//            isSensor = false;
//        }

//        void Update()
//        {
//            // טוגול קריאה מה-ESP עם כפתור A
//            if (rightController.isValid &&
//                rightController.TryGetFeatureValue(CommonUsages.primaryButton, out bool aPressed))
//            {
//                if (aPressed && !lastAPressed && Time.unscaledTime - lastToggleTime >= buttonDebounceSeconds)
//                {
//                    isSensor = !isSensor;
//                    lastToggleTime = Time.unscaledTime;

//                    if (isSensor)
//                    {
//                        if (fetchCoroutine != null) StopCoroutine(fetchCoroutine);
//                        fetchCoroutine = StartCoroutine(FetchSensorLoop());
//                    }
//                    else
//                    {
//                        if (fetchCoroutine != null) StopCoroutine(fetchCoroutine);
//                        fetchCoroutine = null;

//                        // כשמכבים – נייח מיקום/זווית לרצפה אם יש עוגן
//                        if (floorAnchor)
//                        {
//                            var p = floorAnchor.position;
//                            if (p.y < minY) p.y = minY;
//                            _nextPos = p;
//                            _nextRot = floorAnchor.rotation;
//                            _havePose = true;
//                        }
//                        else
//                        {
//                            // ברירת מחדל "שפויה"
//                            _nextPos = new Vector3(hammer.position.x, Mathf.Max(minY, hammer.position.y), hammer.position.z);
//                            _nextRot = Quaternion.Euler(-90f, 0f, hammer.rotation.eulerAngles.z);
//                            _havePose = true;
//                        }
//                    }
//                }
//                lastAPressed = aPressed;
//            }
//            else
//            {
//                lastAPressed = false;
//            }
//        }

//        IEnumerator FetchSensorLoop()
//        {
//            while (isSensor)
//            {
//                yield return StartCoroutine(GetSensorDataOnce());
//                yield return new WaitForSeconds(updateInterval);
//            }
//        }

//        /// <summary>
//        /// שולף דגימה אחת, מחשב יעד מיקום/רוטציה ושומר ל-FixedUpdate.
//        /// </summary>
//        IEnumerator GetSensorDataOnce()
//        {
//            if (string.IsNullOrEmpty(espUrl)) yield break;

//            using (UnityWebRequest www = UnityWebRequest.Get(espUrl))
//            {
//                www.timeout = 5;
//                yield return www.SendWebRequest();

//                if (www.result != UnityWebRequest.Result.Success)
//                    yield break;

//                SensorData data = null;
//                try { data = JsonUtility.FromJson<SensorData>(www.downloadHandler.text); }
//                catch { data = null; }

//                if (data == null || handReference == null)
//                    yield break;

//                // נרמול/קלמפ כמו שעבד לך
//                float rawPitch = NormalizeAngle(data.pitch);
//                float pitch = Mathf.Clamp(rawPitch, -60f, 60f);
//                float yaw = NormalizeAngle(data.yaw);
//                // אנחנו מבטלים Roll בשביל יציבות (אם תרצי אפשר להחזיר)
//                Quaternion imuRotation = Quaternion.Euler(0f, yaw, -pitch);

//                // מיקום מטרה: לגרום ל-GripPoint של הפטיש להתיישב על היד
//                Vector3 gripOffset = (hammerGripPoint ? (hammer.position - hammerGripPoint.position) : Vector3.zero);
//                Vector3 targetPos = handReference.position + handReference.rotation * gripOffset;
//                if (targetPos.y < minY) targetPos.y = minY;

//                // רוטציית מטרה: יד * imu * כיול ראשוני
//                Quaternion targetRot = handReference.rotation * imuRotation * initialRotationOffset;

//                // שומרים יעדים – היישום בפועל יעשה ב-FixedUpdate דרך RB
//                _nextPos = targetPos;
//                _nextRot = Quaternion.Slerp(hammer.rotation, targetRot, rotationSensitivity);
//                _havePose = true;
//            }
//        }

//        void FixedUpdate()
//        {
//            if (!_havePose || hammerRb == null) return;

//            // תנועה קווית – הגבלת צעד כדי לא "לדלג" דרך קיר/קוליידר
//            Vector3 fromPos = hammerRb.position;
//            Vector3 toPos = _nextPos;
//            Vector3 delta = toPos - fromPos;
//            float maxStep = Mathf.Max(0.001f, maxLinearStep);
//            if (delta.magnitude > maxStep)
//                toPos = fromPos + delta.normalized * maxStep;

//            hammerRb.MovePosition(toPos);

//            // תנועה זוויתית – הגבלת צעד במעלות
//            Quaternion fromRot = hammerRb.rotation;
//            Quaternion toRot = Quaternion.RotateTowards(fromRot, _nextRot, Mathf.Max(1f, maxAngularStepDeg));
//            hammerRb.MoveRotation(toRot);
//        }

//        // ---------- עזרים ----------
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
//            // שדות נוספים אם יגיעו – לא חובה להשתמש:
//            public float ax, ay, az;
//            public float gx, gy, gz;
//        }
//    }
//}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR;

namespace angerRoom
{
    /// <summary>
    /// מושך Orientation מה-ESP, מחשב יעד, ומזיז את ה-Rigidbody רק ב-FixedUpdate עם MovePosition/MoveRotation.
    /// כך טריגרים/קוליז'נים עובדים גם כשלא אוחזים ב-Grab.
    /// שימי את הסקריפט על GameObject מנהל. חברי: handReference, hammer, hammerGripPoint.
    /// על הפטיש עצמו: Rigidbody (isKinematic=true), Body Collider (לא טריגר), Head BoxCollider (IsTrigger=true, דק ובולט מעט קדימה).
    /// </summary>
    public class HammerIMUSimpleDriver : MonoBehaviour
    {
        [Header("ESP")]
        public string espUrl = "http://172.20.10.2/hammer";
        [Tooltip("כל כמה זמן למשוך נתונים מהחיישן (שניות)")]
        public float updateInterval = 0.05f;

        [Header("Scene Refs")]
        public Transform hammer;          // שורש הפטיש (עם ה-Rigidbody והקוליידרים)
        public Transform handReference;   // Anchor של היד/השלט
        public Transform hammerGripPoint; // נקודת אחיזה על הידית (Empty על הפטיש)
        public Transform floorAnchor;     // אופציונלי

        [Header("Rotation")]
        [Range(0f, 1f)] public float rotationSensitivity = 0.25f; // בלנד לסיבוב המטרה (סינון)
        public float minY = 0.5f;
        public bool clampPitch = true;
        public float minPitchDeg = -60f, maxPitchDeg = 60f;

        [Header("Physics Apply (FixedUpdate)")]
        [Tooltip("מקס' מרחק למעבר בכל FixedUpdate (מונע 'קפיצות' שמפספסות טריגר)")]
        public float maxLinearStepPerFixed = 0.25f;      // במטרים
        [Tooltip("מקס' סיבוב בכל FixedUpdate (מעלות)")]
        public float maxAngularStepPerFixed = 60f;       // במעלות

        [Header("Toggle (A button)")]
        public float buttonDebounceSeconds = 0.25f;

        // runtime
        private Rigidbody rb;
        private Quaternion initialRotationOffset = Quaternion.identity;

        private InputDevice rightController;
        private bool isSensor = false, lastAPressed = false;
        private float lastToggleTime = -999f;

        // יעד שאותו ניישם בפיזיקה
        private Vector3 _nextPos;
        private Quaternion _nextRot;

        // נתוני IMU מסשן אחרון (לנוחות)
        private float lastPitch, lastYaw;

        void Awake()
        {
            if (!hammer) hammer = transform;
            rb = hammer.GetComponent<Rigidbody>();
            if (!rb) rb = hammer.gameObject.AddComponent<Rigidbody>();

            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.detectCollisions = true;

            // שלט ימין (לטוגל A)
            var list = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, list);
            if (list.Count > 0) rightController = list[0];

            if (handReference)
                initialRotationOffset = Quaternion.Inverse(handReference.rotation) * hammer.rotation;

            // יעד פתיחה = מצב נוכחי
            _nextPos = rb.position;
            _nextRot = rb.rotation;
        }

        void Start()
        {
            // אופציונלי: להתחיל ישר בפולינג
            // isSensor = true;
            // StartCoroutine(FetchSensorLoop());
        }

        void Update()
        {
            // טוגול עם כפתור A
            if (rightController.isValid &&
                rightController.TryGetFeatureValue(CommonUsages.primaryButton, out bool aPressed))
            {
                if (aPressed && !lastAPressed && Time.unscaledTime - lastToggleTime >= buttonDebounceSeconds)
                {
                    isSensor = !isSensor;
                    lastToggleTime = Time.unscaledTime;

                    StopAllCoroutines();
                    if (isSensor)
                        StartCoroutine(FetchSensorLoop());
                    else
                        SnapOffToFloorOrHold();
                }
                lastAPressed = aPressed;
            }
            else lastAPressed = false;
        }

        void SnapOffToFloorOrHold()
        {
            if (floorAnchor)
            {
                var p = floorAnchor.position; if (p.y < minY) p.y = minY;
                _nextPos = p;
                _nextRot = floorAnchor.rotation;
            }
            else
            {
                var p = rb.position; if (p.y < minY) p.y = minY;
                _nextPos = p;
                // כיוון כללי קדימה-לרצפה
                _nextRot = Quaternion.Euler(-90f, 0f, 0f);
            }
        }

        IEnumerator FetchSensorLoop()
        {
            var wait = new WaitForSeconds(updateInterval);
            while (isSensor)
            {
                yield return GetSensorDataOnce();
                yield return wait;
            }
        }

        IEnumerator GetSensorDataOnce()
        {
            if (string.IsNullOrEmpty(espUrl)) yield break;

            using (var www = UnityWebRequest.Get(espUrl))
            {
                www.timeout = 5;
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                    yield break;

                SensorData data = null;
                try { data = JsonUtility.FromJson<SensorData>(www.downloadHandler.text); }
                catch { yield break; }

                // נרמול/קלמפ
                float pitch = NormalizeAngle(data.pitch);
                if (clampPitch) pitch = Mathf.Clamp(pitch, minPitchDeg, maxPitchDeg);
                float yaw = NormalizeAngle(data.yaw);
                // roll לא נשתמש כדי לייצב
                lastPitch = pitch; lastYaw = yaw;

                // חישוב יעד: מיקום צמוד ליד (עם אופסט אחיזה), וסיבוב יד * IMU(pitch,yaw) * כיול
                if (!handReference) yield break;

                // אופסט אחיזה: לגרום ל-GripPoint ליפול בדיוק על היד
                Vector3 gripOffset = Vector3.zero;
                if (hammerGripPoint) gripOffset = hammer.position - hammerGripPoint.position;
                Vector3 targetPos = handReference.position + handReference.rotation * gripOffset;
                if (targetPos.y < minY) targetPos.y = minY;

                // סיבוב מה-IMU: Z= -pitch, Y= yaw, בלי roll
                Quaternion imuRotation = Quaternion.Euler(0f, yaw, -pitch);
                Quaternion targetRot = handReference.rotation * imuRotation * initialRotationOffset;

                // קצת החלקה רכה ליעד (הבלנד כאן רק לעידון; התנועה האמיתית ב-Fixed)
                //_nextPos = Vector3.Lerp(rb.position, targetPos, 0.6f);
                //_nextRot = Quaternion.Slerp(rb.rotation, targetRot, rotationSensitivity);
                _nextPos = targetPos;
                _nextRot = targetRot;
            }
        }

        void FixedUpdate()
        {
            // החלת היעד בפיזיקה כדי לא לפספס טריגרים/קוליז'נים
            if (!rb) return;

            // תזוזה ליניארית – הגבלת צעד
            Vector3 from = rb.position;
            Vector3 to = _nextPos;
            Vector3 delta = to - from;
            float maxStep = Mathf.Max(0.001f, maxLinearStepPerFixed);
            if (delta.magnitude > maxStep)
                to = from + delta.normalized * maxStep;

            rb.MovePosition(to);

            // תזוזה זוויתית – הגבלת צעד
            Quaternion rotFrom = rb.rotation;
            Quaternion rotTo = _nextRot;
            float maxAng = Mathf.Max(1f, maxAngularStepPerFixed);
            Quaternion clamped = Quaternion.RotateTowards(rotFrom, rotTo, maxAng);
            rb.MoveRotation(clamped);
        }

        // ------- helpers -------
        static float NormalizeAngle(float a)
        {
            a %= 360f;
            if (a > 180f) a -= 360f;
            else if (a < -180f) a += 360f;
            return a;
        }

        [System.Serializable]
        public class SensorData
        {
            public float pitch, roll, yaw;
            // אופציונלי: ax,ay,az,gx,gy,gz – JsonUtility יתעלם אם יש עוד שדות
            public float ax, ay, az;
            public float gx, gy, gz;
        }
    }
}

