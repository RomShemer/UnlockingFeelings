using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class EspImuReader : MonoBehaviour
{
    [Header("Network")]
    public string espUrl = "http://10.100.102.28/sensor";
    [Range(0.02f, 0.2f)] public float updateInterval = 0.05f;

    [Header("Targets")]
    public Transform flashlight;          // שורש הספר
    public Transform handReference;       // RightHandAnchor / LeftHandAnchor
    public Transform flashlightGripPoint; // ילד בתוך הספר = נקודת אחיזה
    public Transform floorAnchor;         // עוגן על הרצפה (bookAnchor)

    [Header("Tuning")]
    [Range(0f, 1f)] public float rotationSensitivity = 0.2f;
    public float minY = 0.5f;

    [Header("Attach gate (IMU motion to attach to hand)")]
    public float attachGyroDegPerSec = 40f;  // סף תנועה לפי ג'יירו
    public float attachAccelDeltaG   = 0.25f; // סף תנועה לפי |a|-1g

    Rigidbody rb;
    Coroutine fetchLoop;

    enum ControlState { OnFloor, AttachedToHand, PodiumLocked }
    ControlState state = ControlState.OnFloor;
    Transform lockedAnchor; // לעיגון לפודיום

    // הטיה מה-IMU (pitch/roll בלבד)
    Quaternion targetImuTilt = Quaternion.identity;

    // החלקה לחיישנים (דיאגנוסטיקה/שער)
    float smoothedGyro = 0f, smoothedAccelDelta = 0f;

    // Offset רוטציה בזמן ההצמדה ליד (כדי למנוע "קפיצה" בזווית)
    Quaternion initialRotationOffset = Quaternion.identity;

    void Awake()
    {
        if (!flashlightGripPoint) flashlightGripPoint = flashlight;

        rb = flashlight.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
        }
    }

    void Start()
    {
        // מצב פתיחה: דבוק לרצפה
        SnapToFloor();
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
            if (state == ControlState.OnFloor)
            {
                float gyroMag = Mathf.Abs(data.gx) + Mathf.Abs(data.gy) + Mathf.Abs(data.gz);
                float accelMag = Mathf.Sqrt(data.ax*data.ax + data.ay*data.ay + data.az*data.az);
                float accelDelta = Mathf.Abs(accelMag - 1f);

                smoothedGyro       = Mathf.Lerp(smoothedGyro, gyroMag, 0.3f);
                smoothedAccelDelta = Mathf.Lerp(smoothedAccelDelta, accelDelta, 0.3f);

                if (smoothedGyro > attachGyroDegPerSec || smoothedAccelDelta > attachAccelDeltaG)
                    AttachToHand();
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

        // yaw מהיד/ראש (מישור אופקי)
        Quaternion handYaw;
        Vector3 flatFwd = handReference.forward; flatFwd.y = 0f;
        handYaw = flatFwd.sqrMagnitude > 1e-6f
            ? Quaternion.LookRotation(flatFwd.normalized, Vector3.up)
            : Quaternion.Euler(0f, handReference.eulerAngles.y, 0f);

        // רוטציה סופית: yaw מהיד * הטיית IMU * offset כיול
        Quaternion targetRot = handYaw * targetImuTilt * initialRotationOffset;

        // מיקום: לגרום ל-GripPoint להתלכד עם היד
        Vector3 targetPos = handReference.position - (targetRot * flashlightGripPoint.localPosition);
        if (targetPos.y < minY) targetPos.y = minY;

        ApplyPose(targetPos, targetRot);
    }

    // ---------- מצבי מעבר ----------
    void AttachToHand()
    {
        state = ControlState.AttachedToHand;

        // כיול offset כך שהסיבוב לא יקפוץ
        if (handReference)
            initialRotationOffset = Quaternion.Inverse(handReference.rotation) * flashlight.rotation;

        // שחרר קפיאות אם היו
        if (rb)
        {
            rb.constraints = RigidbodyConstraints.None;
            rb.isKinematic = true;
        }
        // Debug.Log("[Book] Attached to hand.");
    }

    public void SnapToPodium(Transform anchor)
    {
        lockedAnchor = anchor;
        state = ControlState.PodiumLocked;

        if (rb)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        // הצבה מידית (לא חייב, אבל נעים)
        if (lockedAnchor) ApplyPose(lockedAnchor.position, lockedAnchor.rotation);
        // Debug.Log("[Book] Locked to podium.");
    }

    void SnapToFloor()
    {
        state = ControlState.OnFloor;

        if (rb)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        if (floorAnchor) ApplyPose(floorAnchor.position, floorAnchor.rotation);
        // Debug.Log("[Book] Snapped to floor.");
    }

    // ---------- עזר ----------
    void ApplyPose(Vector3 pos, Quaternion rot)
    {
        if (rb) { rb.MovePosition(pos); rb.MoveRotation(rot); }
        else { flashlight.position = pos; flashlight.rotation = rot; }
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
