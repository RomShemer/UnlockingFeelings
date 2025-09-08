using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class EspImuReaderSad : MonoBehaviour
{
    [Header("Network")]
    public string espUrl = "http://10.100.102.28/sensor";
    [Range(0.02f, 0.2f)] public float updateInterval = 0.05f;

    [Header("Targets")]
    public Transform flashlight;          // שורש הספר הנוכחי
    public Transform handReference;       // RightHandAnchor / LeftHandAnchor (אופציונלי)
    public Transform flashlightGripPoint; // ילד בתוך הספר = נקודת אחיזה (localPosition)
    public Transform floorAnchor;         // עוגן על הרצפה (bookAnchor)

    [Header("Tuning")]
    [Range(0f, 1f)] public float rotationSensitivity = 0.2f;
    public float minY = 0.5f;

    [Header("Attach gate (IMU motion to attach to hand)")]
    public bool  autoAttach            = true;   // אפשר לכבות חיבור אוטומטי
    public float attachWarmupSeconds   = 1.5f;   // גרייס לפני שמותר להתחבר
    public float attachMinHoldSeconds  = 0.35f;  // זמן תנועה רציפה לפני חיבור
    public float attachGyroDegPerSec   = 40f;    // סף תנועה לפי ג'יירו
    public float attachAccelDeltaG     = 0.25f;  // סף תנועה לפי |a|-1g

    [Header("Game Flow Lock")]
    public bool lockedUntilBlueChoice = true;    // כשהוא true – לא יתחבר ליד עד "פתיחה"

    [Header("Binding")]
    [Tooltip("כשעושים Bind ליעד חדש – לשחרר נעילה אוטומטית")]
    public bool autoUnlockOnBind = true;

    Rigidbody rb;
    Coroutine fetchLoop;

    enum ControlState { OnFloor, AttachedToHand, PodiumLocked }
    ControlState state = ControlState.OnFloor;
    Transform lockedAnchor; // לעיגון לפודיום (לא חובה כאן)

    // הטיה מה-IMU (pitch/roll בלבד)
    Quaternion targetImuTilt = Quaternion.identity;

    // החלקה לחיישנים (דיאגנוסטיקה/שער)
    float smoothedGyro = 0f, smoothedAccelDelta = 0f;

    // Offset רוטציה בזמן ההצמדה ליד (כדי למנוע "קפיצה" בזווית)
    Quaternion initialRotationOffset = Quaternion.identity;

    // תזמון לגרייס + דיבאונס
    float sceneStartTime;
    float movingTimer = 0f;

    // Fallback למדידת מהירות שינוי הזווית
    Quaternion _prevTilt;
    bool _prevTiltInitialized = false;

    void Awake()
    {
        if (!flashlightGripPoint) flashlightGripPoint = flashlight;
        RefreshRigidbodyFromFlashlight();
    }

    void Start()
    {
        SnapToFloor();                      // מצב פתיחה: דבוק לרצפה (אם יש floorAnchor)
        sceneStartTime = Time.time;         // התחלת גרייס
        fetchLoop = StartCoroutine(FetchSensorLoop());
    }

    void OnDisable()
    {
        if (fetchLoop != null) StopCoroutine(fetchLoop);
    }

    IEnumerator FetchSensorLoop()
    {
        while (true)
        {
            if (state != ControlState.PodiumLocked)
                yield return GetSensorData();
            else
                yield return null;

            yield return new WaitForSeconds(updateInterval);
        }
    }

    IEnumerator GetSensorData()
    {
        using (var www = UnityWebRequest.Get(espUrl))
        {
            www.timeout = 5;
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success) yield break;

            var data = JsonUtility.FromJson<SensorData>(www.downloadHandler.text);

            // --- tilt מה-IMU ---
            float pitch = Mathf.Clamp(NormalizeAngle(data.pitch), -60f, 60f);
            float roll  = NormalizeAngle(data.roll);
            var imuTilt = Quaternion.Euler(roll, 0f, -pitch);
            targetImuTilt = Quaternion.Slerp(targetImuTilt, imuTilt, rotationSensitivity);

            // --- שער חיבור ליד (רק כשעל הרצפה) ---
            if (state == ControlState.OnFloor && autoAttach && !lockedUntilBlueChoice)
            {
                if (Time.time - sceneStartTime >= attachWarmupSeconds && handReference != null)
                {
                    float gyroMag = Mathf.Abs(data.gx) + Mathf.Abs(data.gy) + Mathf.Abs(data.gz);
                    float accelMag = Mathf.Sqrt(data.ax*data.ax + data.ay*data.ay + data.az*data.az);
                    float accelDelta = Mathf.Abs(accelMag - 1f);

                    smoothedGyro       = Mathf.Lerp(smoothedGyro, gyroMag, 0.3f);
                    smoothedAccelDelta = Mathf.Lerp(smoothedAccelDelta, accelDelta, 0.3f);

                    // Fallback: מהירות שינוי הטילט
                    if (!_prevTiltInitialized) { _prevTilt = targetImuTilt; _prevTiltInitialized = true; }
                    float angleDelta = Quaternion.Angle(_prevTilt, targetImuTilt);
                    float angleSpeed = angleDelta / Mathf.Max(0.0001f, updateInterval);
                    _prevTilt = targetImuTilt;

                    const float angleSpeedThresholdDegPerSec = 20f;

                    bool moving =
                        (smoothedGyro > attachGyroDegPerSec) ||
                        (smoothedAccelDelta > attachAccelDeltaG) ||
                        (angleSpeed > angleSpeedThresholdDegPerSec);

                    movingTimer = moving ? (movingTimer + updateInterval) : 0f;

                    if (movingTimer >= attachMinHoldSeconds)
                        AttachToHand();
                }
                else
                {
                    movingTimer = 0f;
                }
            }
            else
            {
                movingTimer = 0f;
            }
        }
    }

    void FixedUpdate()
    {
        if (!flashlight) return;

        if (state == ControlState.PodiumLocked)
        {
            if (!lockedAnchor) return;
            Vector3 p = lockedAnchor.position; if (p.y < minY) p.y = minY;
            ApplyPose(p, lockedAnchor.rotation);
            return;
        }

        if (state == ControlState.OnFloor)
        {
            if (!floorAnchor) return;
            Vector3 p = floorAnchor.position; if (p.y < minY) p.y = minY;
            ApplyPose(p, floorAnchor.rotation);
            return;
        }

        // state == AttachedToHand
        if (!handReference || !flashlightGripPoint) return;

        // yaw מהיד (מישור אופקי)
        Vector3 flatFwd = handReference.forward; flatFwd.y = 0f;
        Quaternion handYaw = flatFwd.sqrMagnitude > 1e-6f
            ? Quaternion.LookRotation(flatFwd.normalized, Vector3.up)
            : Quaternion.Euler(0f, handReference.eulerAngles.y, 0f);

        // רוטציה סופית: yaw מהיד * הטיית IMU * offset כיול
        Quaternion targetRot = handYaw * targetImuTilt * initialRotationOffset;

        // מיקום: לגרום ל-GripPoint להתלכד עם היד
        Vector3 targetPos = handReference.position - (targetRot * flashlightGripPoint.localPosition);
        if (targetPos.y < minY) targetPos.y = minY;

        ApplyPose(targetPos, targetRot);
    }

    // ---------- API למשחק ----------
    /// <summary>לקשור את החיישן לספר חדש (קריאה מכפתור צבע).</summary>
    public void BindToTargets(Transform newFlashlight, Transform newGripPoint, Transform newFloorAnchor, bool unlock = true, bool snapNow = true)
    {
        flashlight = newFlashlight;
        flashlightGripPoint = newGripPoint ? newGripPoint : newFlashlight;
        floorAnchor = newFloorAnchor;

        RefreshRigidbodyFromFlashlight();

        state = ControlState.OnFloor;
        _prevTiltInitialized = false;
        initialRotationOffset = Quaternion.identity;

        if (unlock || autoUnlockOnBind)
            lockedUntilBlueChoice = false;

        sceneStartTime = Time.time;   // גרייס קצר אחרי bind
        if (snapNow) SnapToFloor();
    }

    /// <summary>Bind בלי לשחרר נעילה (נשאר נעול).</summary>
    public void BindLocked(Transform newFlashlight, Transform newGripPoint, Transform newFloorAnchor, bool snapNow = true)
        => BindToTargets(newFlashlight, newGripPoint, newFloorAnchor, unlock:false, snapNow:snapNow);

    /// <summary>שחרור נעילה ידני (flow ישן).</summary>
    public void UnlockByBlueChoice()
    {
        lockedUntilBlueChoice = false;
        sceneStartTime = Time.time;
        SnapToFloor();
    }

    /// <summary>מי הספר שמחובר כרגע (יכול להיות null).</summary>
    public Transform CurrentFlashlight => flashlight;

    /// <summary>ניתוק מלא: החיישן מפסיק לשלוט; יידרש שוב כפתור צבע כדי לחבר.</summary>
    public void ClearBinding(bool relock = true)
    {
        lockedUntilBlueChoice = relock;
        state = ControlState.OnFloor;

        flashlight = null;
        flashlightGripPoint = null;
        floorAnchor = null;
        rb = null;

        movingTimer = 0f;
        _prevTiltInitialized = false;
        initialRotationOffset = Quaternion.identity;
    }

    [ContextMenu("Force Attach To Hand")]
    public void ForceAttach() { AttachToHand(); }

    [ContextMenu("Detach To Floor")]
    public void DetachToFloor() { SnapToFloor(); }

    // ---------- מצבי מעבר ----------
    void AttachToHand()
    {
        state = ControlState.AttachedToHand;

        if (handReference && flashlight)
            initialRotationOffset = Quaternion.Inverse(handReference.rotation) * flashlight.rotation;

        if (rb)
        {
            rb.constraints = RigidbodyConstraints.None;
            rb.isKinematic = true;
        }
    }

    public void SnapToPodium(Transform anchor)
    {
        lockedAnchor = anchor;
        state = ControlState.PodiumLocked;

        if (rb)
        {
            rb.isKinematic = true;
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero; // Unity 6
#else
            rb.velocity = Vector3.zero;       // Unity 2022/2023
#endif
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        if (lockedAnchor) ApplyPose(lockedAnchor.position, lockedAnchor.rotation);
    }

    void SnapToFloor()
    {
        state = ControlState.OnFloor;

        if (rb)
        {
            rb.isKinematic = true;
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        if (floorAnchor) ApplyPose(floorAnchor.position, floorAnchor.rotation);
    }

    // ---------- עזר ----------
    void RefreshRigidbodyFromFlashlight()
    {
        rb = flashlight ? flashlight.GetComponent<Rigidbody>() : null;
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
        }
    }

    void ApplyPose(Vector3 pos, Quaternion rot)
    {
        if (rb) { rb.MovePosition(pos); rb.MoveRotation(rot); }
        else if (flashlight) { flashlight.position = pos; flashlight.rotation = rot; }
    }

    static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        else if (angle < -180f) angle += 360f;
        return angle;
    }

    [System.Serializable]
    public class SensorData
    {
        public float pitch, roll, yaw;
        public float ax, ay, az;
        public float gx, gy, gz;
    }
}
