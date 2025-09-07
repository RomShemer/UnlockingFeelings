//using UnityEngine;

//public class HammerTrigger : MonoBehaviour
//{
//    public float minSpeedToHit = 1.0f; // רק אם מכה חזקה מספיק
//    private Vector3 lastPosition;
//    private Vector3 velocity;

//    void Update()
//    {
//        velocity = (transform.position - lastPosition) / Time.deltaTime;
//        lastPosition = transform.position;
//    }

//    private void OnTriggerEnter(Collider other)
//    {
//        if (other.gameObject.GetComponent<Destroy>() != null)
//        {
//            float impactSpeed = velocity.magnitude;
//            Debug.Log("Hammer Trigger hit! Speed: " + impactSpeed);

//            if (impactSpeed >= minSpeedToHit)
//            {
//                Destroy wallScript = other.GetComponent<Destroy>();
//                wallScript.hp--;

//                Debug.Log("Wall HP: " + wallScript.hp);
//            }
//        }
//    }
//}

//using UnityEngine;
//using System.Collections;

//public class HammerTrigger : MonoBehaviour
//{
//    [Header("Hit Settings")]
//    public float minSpeedToHit = 1.0f; // רק אם מכה חזקה מספיק

//    [Header("Controller Vibration")]
//    public bool vibrateLeftHand = false; // בחרי באינספקטור את היד
//    public bool vibrateRightHand = true;

//    private Vector3 lastPosition;
//    private Vector3 velocity;

//    [Header("Audio")]
//    public AudioSource audioSource;
//    public AudioClip hitSound;
//    public float playDuration = 1f;   // כמה זמן להשמיע בכל מכה
//    private float lastAudioTime = 0.5f; // זוכר איפה נעצרנו

//    void Update()
//    {
//        velocity = (transform.position - lastPosition) / Time.deltaTime;
//        lastPosition = transform.position;
//    }

//    private void OnTriggerEnter(Collider other)
//    {
//        if (other.gameObject.GetComponent<Destroy>() != null)
//        {
//            float impactSpeed = velocity.magnitude;
//            Debug.Log("Hammer Trigger hit! Speed: " + impactSpeed);

//            if (impactSpeed >= minSpeedToHit)
//            {
//                // הורדת HP
//                Destroy wallScript = other.GetComponent<Destroy>();
//                wallScript.hp--;

//                Debug.Log("Wall HP: " + wallScript.hp);

//                if (audioSource != null && hitSound != null)
//                {
//                    audioSource.clip = hitSound;

//                    // אם עצרנו בסוף – נתחיל מהתחלה
//                    if (lastAudioTime >= hitSound.length)
//                        lastAudioTime = 0f;

//                    audioSource.time = lastAudioTime;
//                    audioSource.Play();

//                    StartCoroutine(StopAudioAfter(playDuration));
//                }


//                // שליחת רטט לבקר
//                if (vibrateLeftHand)
//                    StartCoroutine(HapticPulse(OVRInput.Controller.LTouch));

//                if (vibrateRightHand)
//                    StartCoroutine(HapticPulse(OVRInput.Controller.RTouch));
//            }
//        }
//    }

//    private IEnumerator StopAudioAfter(float duration)
//    {
//        yield return new WaitForSeconds(duration);

//        if (audioSource.isPlaying)
//        {
//            lastAudioTime = audioSource.time; // זוכר את המיקום האחרון
//            audioSource.Stop();
//        }
//    }


//    // רטט חזק יותר למשך 0.3 שניות
//    private IEnumerator HapticPulse(OVRInput.Controller controller)
//    {
//        for (int i = 0; i < 3; i++) // 3 פולסים
//        {
//            OVRInput.SetControllerVibration(1f, 1f, controller);
//            yield return new WaitForSeconds(0.15f);
//            OVRInput.SetControllerVibration(0f, 0f, controller);
//            yield return new WaitForSeconds(0.05f);
//        }
//    }

//}

//using UnityEngine;
//using System.Collections;

///// <summary>
///// HammerTrigger – מזהה מכה על בסיס IMU (/hammer) או מהירות מקומית (Fallback).
///// שימי על הילד של ראש הפטיש שיש עליו BoxCollider עם IsTrigger=true.
///// </summary>
//public class HammerTrigger : MonoBehaviour
//{
//    [Header("Hit Settings (Fallback: Velocity)")]
//    [Tooltip("סף מינימלי למהירות פגיעה (מטר/שניה) כשאין IMU או בנוסף אליו")]
//    public float minSpeedToHit = 1.0f;

//    [Header("IMU (Optional)")]
//    [Tooltip("שימי כאן את הקומפוננטה שמושכת את /hammer (IMUClientHammer)")]
//    public IMUClientHammer imu;  // אופציונלי
//    [Range(0f, 1f)] public float imuAccelWeight = 0.7f;
//    [Range(0f, 1f)] public float imuGyroWeight = 0.3f;
//    [Tooltip("נרמול ג'יירו (deg/s) לציון בין 0..1")]
//    public float imuGyroScaleDps = 500f;
//    [Tooltip("סף ציון IMU כדי להיחשב כמכה")]
//    public float imuImpactThreshold = 0.5f;
//    [Tooltip("לשלב IMU + מהירות (OR)")]
//    public bool combineIMUandSpeed = true;

//    [Header("Cooldown")]
//    [Tooltip("זמן מינימלי בין שתי מכות (שניות)")]
//    public float hitCooldown = 0.06f;
//    float lastHitTime = -999f;

//    [Header("Controller Vibration (Meta/Oculus)")]
//    public bool vibrateLeftHand = false;
//    public bool vibrateRightHand = true;
//    [Tooltip("מספר פולסים/רטיטות")] public int hapticPulses = 3;
//    [Tooltip("משך כל פולס (שניות)")] public float hapticPulse = 0.15f;
//    [Tooltip("הפסקה בין פולסים")] public float hapticGap = 0.05f;

//    [Header("Audio")]
//    public AudioSource audioSource;
//    public AudioClip hitSound;
//    [Tooltip("כמה זמן להשמיע בכל מכה")] public float playDuration = 1f;
//    float lastAudioTime = 0.5f;

//    // Velocity fallback (לפי תנועת הראש בסצנה)
//    Vector3 lastPosition;
//    Vector3 velocity;

//    void Update()
//    {
//        velocity = (transform.position - lastPosition) / Mathf.Max(Time.deltaTime, 1e-5f);
//        lastPosition = transform.position;
//    }

//    void OnTriggerEnter(Collider other)
//    {
//        // מחפשים Destroy על מי שנפגענו בו (או על ההורה שלו)
//        Destroy wallScript = other.GetComponent<Destroy>() ?? other.GetComponentInParent<Destroy>();
//        if (!wallScript) return;

//        // --- חישוב האם זו מכה לפי IMU ---
//        bool imuSaysHit = false;
//        float imuScore = 0f;

//        if (imu != null)
//        {
//            float accelComponent = Mathf.Max(0f, imu.aMag_g - 1.0f); // מעל 1g
//            float gyroComponent = Mathf.Clamp01(imu.wMag_dps / Mathf.Max(imuGyroScaleDps, 1e-3f));
//            imuScore = imuAccelWeight * accelComponent + imuGyroWeight * gyroComponent;
//            imuSaysHit = imuScore >= imuImpactThreshold;
//        }

//        // --- חישוב לפי מהירות (Fallback/שילוב) ---
//        float speed = velocity.magnitude;
//        bool speedSaysHit = speed >= minSpeedToHit;

//        bool isHit =
//            (imu != null && combineIMUandSpeed) ? (imuSaysHit || speedSaysHit) :
//            (imu != null && !combineIMUandSpeed) ? imuSaysHit :
//            speedSaysHit;

//        float now = Time.time;
//        if (!isHit || now - lastHitTime < hitCooldown) return;
//        lastHitTime = now;

//        // --- פגיעה: HP-- + סאונד + רטט ---
//        wallScript.hp--;
//        Debug.Log($"Hammer hit! Speed={speed:0.00} | IMU score={imuScore:0.00} | Wall HP={wallScript.hp}");

//        if (audioSource && hitSound)
//        {
//            audioSource.clip = hitSound;
//            if (lastAudioTime >= hitSound.length) lastAudioTime = 0f;
//            audioSource.time = lastAudioTime;
//            audioSource.Play();
//            StartCoroutine(StopAudioAfter(playDuration));

//            float strength = (imu != null) ? Mathf.Clamp01(imuScore) : Mathf.Clamp01(speed / (minSpeedToHit * 2f));
//            audioSource.volume = Mathf.Clamp01(0.3f + 0.7f * strength);
//        }

//        StartCoroutine(HapticPulsesRoutine((imu != null) ? Mathf.Clamp01(imuScore) : Mathf.Clamp01(speed / (minSpeedToHit * 2f))));
//    }

//    IEnumerator StopAudioAfter(float duration)
//    {
//        yield return new WaitForSeconds(duration);
//        if (audioSource && audioSource.isPlaying)
//        {
//            lastAudioTime = audioSource.time;
//            audioSource.Stop();
//        }
//    }

//    IEnumerator HapticPulsesRoutine(float strength01)
//    {
//        float amplitude = Mathf.Clamp01(0.4f + 0.6f * strength01);
//        for (int i = 0; i < hapticPulses; i++)
//        {
//            if (vibrateRightHand) OVRInput.SetControllerVibration(1f, amplitude, OVRInput.Controller.RTouch);
//            if (vibrateLeftHand) OVRInput.SetControllerVibration(1f, amplitude, OVRInput.Controller.LTouch);
//            yield return new WaitForSeconds(hapticPulse);
//            if (vibrateRightHand) OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
//            if (vibrateLeftHand) OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);
//            yield return new WaitForSeconds(hapticGap);
//        }
//    }
//}
using UnityEngine;
using System.Collections;

/// <summary>
/// HammerTrigger – זיהוי מכה לפי תנועת ראש הפטיש (בלי תלות ב-IMU).
/// שימי על ה-Child של הראש (Collider IsTrigger=true). שורש הפטיש עם Rigidbody.
/// </summary>
[RequireComponent(typeof(Collider))]
public class HammerTrigger : MonoBehaviour
{
    [Header("Hit by Speed")]
    [Tooltip("סף מינימלי למהירות פגיעה (מ'/ש')")]
    public float minSpeedToHit = 1.0f;

    [Tooltip("לחשב רק את רכיב המהירות בכיוון קדימה של הראש (מומלץ)")]
    public bool useForwardComponent = true;

    [Tooltip("אם ה-Forward של הראש מצביע החוצה מהקיר – השאירי 1. אם הפוך – שימי -1")]
    [Range(-1f, 1f)] public float forwardDotSign = 1f;

    [Header("Hit by Angular Speed (optional)")]
    [Tooltip("לדרוש גם מינימום מהירות זוויתית (deg/s) של הפטיש")]
    public bool requireAngularSpeed = false;
    [Tooltip("סף מינימלי למהירות זוויתית (deg/s)")]
    public float minAngularSpeedToHit = 120f;

    [Tooltip("שורש הפטיש לחישוב מהירות זוויתית (אם ריק – ניקח את root)")]
    public Transform hammerRoot;

    [Header("Cooldown")]
    [Tooltip("זמן מינימלי בין שתי פגיעות (שניות)")]
    public float hitCooldown = 0.06f;
    private float lastHitTime = -999f;

    [Header("Haptics (Meta/Oculus)")]
    public bool vibrateLeftHand = false;
    public bool vibrateRightHand = true;
    [Tooltip("מספר פולסים/רטיטות")] public int hapticPulses = 3;
    [Tooltip("משך כל פולס (שניות)")] public float hapticPulse = 0.15f;
    [Tooltip("הפסקה בין פולסים (שניות)")] public float hapticGap = 0.05f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip hitSound;
    [Tooltip("כמה זמן להשמיע בכל מכה")] public float playDuration = 1f;
    private float lastAudioTime = 0.5f;

    // דגימת מהירות/מהירות זוויתית
    private Vector3 lastPos;
    private Vector3 velocity;

    private Quaternion lastRootRot;
    private float angularSpeedDps;

    void Awake()
    {
        if (!hammerRoot) hammerRoot = transform.root;

        // הבטחה שהקוליידר באמת טריגר
        var col = GetComponent<Collider>();
        if (col && !col.isTrigger)
        {
            Debug.LogWarning($"[HammerTrigger] {name}: collider לא על Trigger – מעדכן ל-true.");
            col.isTrigger = true;
        }

        lastPos = transform.position;
        if (hammerRoot) lastRootRot = hammerRoot.rotation;
    }

    void Update()
    {
        float dt = Mathf.Max(Time.deltaTime, 1e-5f);

        // מהירות ליניארית של ראש הפטיש
        velocity = (transform.position - lastPos) / dt;
        lastPos = transform.position;

        // מהירות זוויתית של שורש הפטיש (אם קיים)
        if (hammerRoot)
        {
            float angle = Quaternion.Angle(lastRootRot, hammerRoot.rotation); // במעלות
            angularSpeedDps = angle / dt;
            lastRootRot = hammerRoot.rotation;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // מחפשים Destroy על האובייקט שנפגענו בו (או על האב שלו)
        Destroy wall = other.GetComponent<Destroy>() ?? other.GetComponentInParent<Destroy>();
        if (!wall) return;

        // הגבלת כיוון (אופציונלי): רק תנועה קדימה של הראש נספרת
        float speedMetric;
        if (useForwardComponent)
        {
            float forwardComponent = Vector3.Dot(velocity, transform.forward * Mathf.Sign(forwardDotSign));
            speedMetric = Mathf.Max(0f, forwardComponent); // רק רכיב קדימה
        }
        else
        {
            speedMetric = velocity.magnitude;
        }

        bool passSpeed = speedMetric >= minSpeedToHit;
        bool passAngular = !requireAngularSpeed || (angularSpeedDps >= minAngularSpeedToHit);

        float now = Time.time;
        if (!(passSpeed && passAngular) || (now - lastHitTime < hitCooldown)) return;
        lastHitTime = now;

        // פגיעה – הורדת HP
        wall.hp--;
        Debug.Log($"Hammer hit! speed={speedMetric:0.00} m/s, ang={angularSpeedDps:0} dps, wallHP={wall.hp}");

        // סאונד
        if (audioSource && hitSound)
        {
            audioSource.clip = hitSound;
            if (lastAudioTime >= hitSound.length) lastAudioTime = 0f;
            audioSource.time = lastAudioTime;
            audioSource.Play();
            StartCoroutine(StopAudioAfter(playDuration));

            // עוצמת סאונד יחסית לעוצמת המכה
            float vol = Mathf.Clamp01(0.3f + 0.7f * Mathf.InverseLerp(minSpeedToHit, minSpeedToHit * 2f, speedMetric));
            audioSource.volume = vol;
        }

        // רטט
        float amp = Mathf.Clamp01(0.4f + 0.6f * Mathf.InverseLerp(minSpeedToHit, minSpeedToHit * 2f, speedMetric));
        StartCoroutine(HapticPulsesRoutine(amp));
    }

    private IEnumerator StopAudioAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (audioSource && audioSource.isPlaying)
        {
            lastAudioTime = audioSource.time;
            audioSource.Stop();
        }
    }

    private IEnumerator HapticPulsesRoutine(float amplitude)
    {
        amplitude = Mathf.Clamp01(amplitude);
        for (int i = 0; i < hapticPulses; i++)
        {
            if (vibrateRightHand) OVRInput.SetControllerVibration(1f, amplitude, OVRInput.Controller.RTouch);
            if (vibrateLeftHand) OVRInput.SetControllerVibration(1f, amplitude, OVRInput.Controller.LTouch);

            yield return new WaitForSeconds(hapticPulse);

            if (vibrateRightHand) OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
            if (vibrateLeftHand) OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);

            yield return new WaitForSeconds(hapticGap);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // ויזואליזציה של כיוון "קדימה" של הראש
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.3f);
    }
#endif
}
