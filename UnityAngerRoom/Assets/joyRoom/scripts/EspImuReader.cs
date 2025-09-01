using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class EspImuReader : MonoBehaviour
{
    [Header("Network")]
    public string espUrl = "http://172.20.10.4/sensor";
    [Range(0.01f, 0.2f)] public float updateInterval = 0.05f; // ≈20Hz
    private Coroutine loop;

    [Header("Target")]
    [Tooltip("אובייקט השורש של הספר (עם Rigidbody/Collider)")]
    public Transform flashlight;

    [Header("Apply To Transform")]
    [Tooltip("אם כבוי – הסקריפט רק מחשב יעדים (desiredRotation/targetWorldPos) ולא מזיז את ה-Transform בפועל")]
    public bool applyToTransform = false;

    // -------- Rotation (stable) --------
    [Header("Rotation")]
    public bool useImuToUnityMapping = true;          // X <- -Roll, Y <- Yaw, Z <- -Pitch
    [Range(20f, 720f)] public float maxDegPerSec = 240f; // מגביל קצב סיבוב כדי לבטל קפיצות
    [Range(0f, 15f)] public float angleDeadzoneDeg = 2f; // דד־זון קטן
    public bool enableRotation = true;

    [Tooltip("הטיית הרכבה פיזית של החיישן על הספר (אם לא מודבק בדיוק ישר)")]
    public Vector3 sensorMountOffsetDeg = Vector3.zero;

    // -------- Position (IMU-only, short-range) --------
    [Header("Position (IMU-only, short-range)")]
    [Tooltip("רגישות לסקייל של תנועה בעולם Unity")]
    [Range(0.1f, 5f)] public float positionSensitivity = 1.0f;
    [Tooltip("כמה מהר לדכא מהירות כשאין תנועה (ZUPT)")]
    [Range(0f, 10f)] public float velocityDamping = 2.0f;
    [Tooltip("דליפה איטית של המיקום עצמו לאפס כששקט (מפחית דריפט מצטבר)")]
    [Range(0f, 2f)] public float positionLeak = 0.15f;
    [Tooltip("טווח מקסימלי יחסית לבסיס (מ׳ וירטואליים)")]
    [Range(0.05f, 5f)] public float maxPosRange = 1.0f;

    // -------- Motion gating / quiet detection --------
    [Header("Motion gating / quiet detection")]
    [Tooltip("LPF לתאוצה (0=ללא, 1=רק החדש)")]
    [Range(0f, 1f)] public float accelLPF = 0.25f;
    [Tooltip("סף 'שקט' לג׳יירו (deg/s)")]
    [Range(0f, 40f)] public float gyroQuietDegPerSec = 8f;
    [Tooltip("חלון שקט לאקסלרומטר (||a|| - 1g)")]
    [Range(0f, 0.2f)] public float accelQuietGWindow = 0.04f;

    // -------- Calibration --------
    [Header("Calibration")]
    public bool calibrateOnStart = true;
    public bool autoRecalibrateOnQuiet = true;
    [Range(0.2f, 3f)] public float quietSecondsToCalibrate = 1.2f;
    [Range(0f, 10f)] public float recalibCooldown = 3f;

    // ---- state: rotation baseline (relative mapping) ----
    private Quaternion baselineWorld;   // רוטציית האובייקט בעת כיול
    private Quaternion baselineSensor;  // רוטציית החיישן בעת כיול
    private bool baselineSet;

    // ---- state: position baseline ----
    private Vector3 baseWorldPos;
    private Vector3 velWorld;   // "m/s" יחסיים
    private Vector3 posWorld;   // מיקום יחסי לבסיס

    // anti-jitter / misc
    private float quietTimer, cooldownTimer;
    private Vector3 lastTargetEuler;
    private bool gotLastTarget;

    // ----- חשיפות לשאר הסצנה -----
    public Quaternion desiredRotation { get; private set; }
    public Vector3     targetWorldPos  { get; private set; }
    public bool        isMovingNow     { get; private set; }
    public System.Action OnLift;   // אירוע: התחלת תנועה אחרי שקט

    bool wasCalm = true; // למעקב אחר transition שקט→תנועה

    void Start()
    {
        if (!flashlight) { Debug.LogError("[IMU] 'flashlight' לא משויך"); enabled = false; return; }

        var rb = flashlight.GetComponent<Rigidbody>();
        if (rb) { rb.isKinematic = true; rb.useGravity = false; rb.constraints = RigidbodyConstraints.None; }

        baseWorldPos = flashlight.position;
        loop = StartCoroutine(Loop());
    }

    IEnumerator Loop()
    {
        var wait = new WaitForSeconds(updateInterval);
        while (true)
        {
            yield return FetchOnce();
            yield return wait;
        }
    }

    IEnumerator FetchOnce()
    {
        using (var www = UnityWebRequest.Get(espUrl))
        {
            www.timeout = 3;
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success) yield break;

            var json = www.downloadHandler.text;
            SensorData d = null;
            try { d = JsonUtility.FromJson<SensorData>(json); } catch { }
            if (d == null) yield break;

            Apply(d);
        }
    }

    void Apply(SensorData d)
    {
        // --- build raw sensor rotation from IMU angles (+ mounting offset) ---
        var rawRot = ComputeRawSensorRotation(d) * Quaternion.Euler(sensorMountOffsetDeg);

        // --- init baseline once (relative mapping) ---
        if (!baselineSet && calibrateOnStart)
        {
            baselineSensor = rawRot;
            baselineWorld  = flashlight.rotation;
            baseWorldPos   = flashlight.position;
            baselineSet = true;
            cooldownTimer = recalibCooldown;
            gotLastTarget = false;
            velWorld = Vector3.zero;
            posWorld = Vector3.zero;
        }

        // --- desired rotation (relative to calibration) ---
        Quaternion desired = baselineSet
            ? baselineWorld * (Quaternion.Inverse(baselineSensor) * rawRot)
            : rawRot;

        // --- smooth rotation with angle-rate limiting ---
        if (enableRotation)
        {
            Vector3 cur = flashlight.rotation.eulerAngles;
            Vector3 tgt = desired.eulerAngles;
            float dt = Mathf.Max(0.0001f, updateInterval);
            float maxStep = maxDegPerSec * dt;

            float x = MoveTowardsAngleSmart(cur.x, tgt.x, maxStep, angleDeadzoneDeg);
            float y = MoveTowardsAngleSmart(cur.y, tgt.y, maxStep, angleDeadzoneDeg);
            float z = MoveTowardsAngleSmart(cur.z, tgt.z, maxStep, angleDeadzoneDeg);

            // drop extreme outliers
            if (gotLastTarget)
            {
                float dx = Mathf.Abs(Mathf.DeltaAngle(lastTargetEuler.x, tgt.x));
                float dy = Mathf.Abs(Mathf.DeltaAngle(lastTargetEuler.y, tgt.y));
                float dz = Mathf.Abs(Mathf.DeltaAngle(lastTargetEuler.z, tgt.z));
                if (dx <= 120f && dy <= 120f && dz <= 120f)
                {
                    desired = Quaternion.Euler(x, y, z);
                    lastTargetEuler = tgt;
                }
            }
            else
            {
                desired = Quaternion.Euler(x, y, z);
                lastTargetEuler = tgt;
                gotLastTarget = true;
            }
        }

        // ------------- Position 3D (IMU-only) -------------
        // תאוצה בגוף (g) -> לעולם (g) לפי הרוטציה הרצויה (יציבה יותר)
        Vector3 aBody_g  = new Vector3(d.ax, d.ay, d.az);
        Vector3 aWorld_g = (baselineSet ? desired : flashlight.rotation) * aBody_g;

        // מחסירים כבידה (1g כלפי מעלה עולמית)
        Vector3 linAcc_g = aWorld_g - Vector3.up;

        // LPF קטן לתאוצה
        linAcc_g = Vector3.Lerp(Vector3.zero, linAcc_g, 1f - Mathf.Clamp01(accelLPF));

        // סיווג "שקט"
        float gyroMag = Mathf.Sqrt(d.gx*d.gx + d.gy*d.gy + d.gz*d.gz);
        bool accelQuiet = Mathf.Abs(aWorld_g.magnitude - 1f) <= accelQuietGWindow;
        bool gyroQuiet  = gyroMag <= gyroQuietDegPerSec;
        bool motionQuiet = accelQuiet && gyroQuiet;

        float dt2 = Mathf.Max(0.0001f, updateInterval);

        // אינטגרציה למהירות
        if (motionQuiet) {
            velWorld = Vector3.Lerp(velWorld, Vector3.zero, Mathf.Clamp01(velocityDamping * dt2));
        } else {
            Vector3 acc_rel = linAcc_g * 9.81f;
            velWorld += acc_rel * dt2;
            velWorld = Vector3.Lerp(velWorld, Vector3.zero, Mathf.Clamp01(0.1f * dt2)); // דמפר קל
        }

        // אינטגרציה למיקום
        posWorld += velWorld * dt2;

        // דליפת מיקום לאפס כששקט (מפחית דריפט)
        if (motionQuiet) {
            posWorld = Vector3.Lerp(posWorld, Vector3.zero, Mathf.Clamp01(positionLeak * dt2));
        }

        // הגבלת טווח
        posWorld = Vector3.ClampMagnitude(posWorld, maxPosRange);

        // יעד מיקום בעולם
        Vector3 targetPos = baseWorldPos + posWorld * positionSensitivity;

        // ----- חשיפה + החלה אופציונלית -----
        desiredRotation = desired;
        targetWorldPos  = targetPos;

        if (applyToTransform)
        {
            flashlight.rotation = Quaternion.Slerp(flashlight.rotation, desiredRotation, 0.5f);
            flashlight.position = Vector3.Lerp(flashlight.position,  targetWorldPos,  0.5f);
        }

        // ----- זיהוי קצה: שקט -> תנועה -----
        bool nowCalm = motionQuiet;
        isMovingNow = !motionQuiet;
        if (wasCalm && !nowCalm) {
            OnLift?.Invoke(); // טריגר “לקיחה”
        }
        wasCalm = nowCalm;

        // --- Auto Recalibration on quiet ---
        if (cooldownTimer > 0f) cooldownTimer -= dt2;

        if (autoRecalibrateOnQuiet && motionQuiet && cooldownTimer <= 0f)
        {
            quietTimer += dt2;
            if (quietTimer >= quietSecondsToCalibrate)
            {
                baselineSensor = rawRot;
                baselineWorld  = flashlight.rotation;
                baseWorldPos   = flashlight.position;
                posWorld = Vector3.zero;
                velWorld = Vector3.zero;
                quietTimer = 0f;
                cooldownTimer = recalibCooldown;
                gotLastTarget = false;
            }
        }
        else
        {
            quietTimer = 0f;
        }
    }

    public void Recalibrate()
    {
        baselineWorld  = flashlight.rotation;
        baseWorldPos   = flashlight.position;
        posWorld = Vector3.zero;
        velWorld = Vector3.zero;
        cooldownTimer = recalibCooldown;
        gotLastTarget = false;
        // baselineSensor יתעדכן ב-Apply הבא מהדאטה הנוכחית
    }

    Quaternion ComputeRawSensorRotation(SensorData d)
    {
        float x, y, z;
        if (useImuToUnityMapping)
        {
            // IMU → Unity: X ← -Roll, Y ← Yaw, Z ← -Pitch
            x = -d.roll;
            y =  d.yaw;
            z = -d.pitch;
        }
        else
        {
            x = d.pitch;
            y = d.yaw;
            z = d.roll;
        }
        return Quaternion.Euler(x, y, z);
    }

    static float MoveTowardsAngleSmart(float current, float target, float maxStep, float deadzone)
    {
        float delta = Mathf.DeltaAngle(current, target);
        if (Mathf.Abs(delta) <= deadzone) return current;
        float step = Mathf.Clamp(delta, -maxStep, maxStep);
        return current + step;
    }

    [System.Serializable]
    public class SensorData
    {
        public float pitch, roll, yaw;  // deg
        public float ax, ay, az;        // g
        public float gx, gy, gz;        // deg/s
    }
}
