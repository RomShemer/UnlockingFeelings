//using Oculus.Interaction.Input;
//using UnityEngine;

///// <summary>
///// HammerFollower – מצמיד את הפטיש ליד השחקן ומוסיף סיבוב יחסי מה-IMU.
///// שימי על האב של הפטיש. ודאי שיש Rigidbody (isKinematic=true) + Collider רגיל.
///// </summary>
//[DisallowMultipleComponent]
//public class HammerFollower : MonoBehaviour
//{
//    [Header("Scene References")]
//    [Tooltip("Anchor של היד/קונטרולר (RightHand/RightController Anchor)")]
//    public Transform handReference;

//    [Tooltip("Transform של הפטיש (אם ריק – יילקח מזה האובייקט)")]
//    public Transform hammer;

//    [Tooltip("נקודת אחיזה (Empty כילד על הידית) לשמירת אופסט טבעי")]
//    public Transform hammerGripPoint;

//    [Tooltip("מקור נתוני ה-IMU (האובייקט עם IMUClientHammer)")]
//    public IMUClientHammer imu;

//    [Header("Follow – Position")]
//    [Range(0f, 1f)] public float positionLerp = 0.6f;
//    public bool clampFloor = true;
//    public float minY = 0.5f;

//    [Header("Follow – Rotation")]
//    [Range(0f, 1f)] public float rotationLerp = 0.25f;
//    [Range(0f, 1f)] public float imuBlend = 1f;       // 0=רק יד, 1=IMU מלא (יחסי ליד)
//    public bool useIMURoll = false;                   // בד"כ כבוי ליציבות
//    public Vector3 initialEulerOffset = Vector3.zero; // יישור ידני ראשוני

//    [Header("Pitch Clamp (optional)")]
//    public bool clampPitch = true;
//    public float minPitchDeg = -60f;
//    public float maxPitchDeg = 60f;

//    Quaternion initialRotOffset;
//    Rigidbody rb;


//    void Reset() { hammer = transform; }

//    void Awake()
//    {
//        if (hammer == null) hammer = transform;

//        rb = hammer.GetComponent<Rigidbody>();
//        if (!rb) rb = hammer.gameObject.AddComponent<Rigidbody>();

//        rb.isKinematic = true;
//        rb.interpolation = RigidbodyInterpolation.Interpolate;
//        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

//        initialRotOffset = Quaternion.Euler(initialEulerOffset);
//    }

//    void Update()
//    {
//        if (!handReference || !hammer) return;

//        // ----- Position: צמוד ליד עם אופסט אחיזה -----
//        Vector3 gripOffset = Vector3.zero;
//        if (hammerGripPoint != null)
//            gripOffset = hammer.position - hammerGripPoint.position;

//        Vector3 targetPos = handReference.position + handReference.rotation * gripOffset;
//        if (clampFloor && targetPos.y < minY) targetPos.y = minY;
//        hammer.position = Vector3.Lerp(hammer.position, targetPos, positionLerp);

//        // ----- Rotation: יד * (בלנד עם IMU) * אופסט -----
//        Quaternion handRot = handReference.rotation;

//        if (imu == null)
//        {
//            Quaternion noImu = handRot * initialRotOffset;
//            hammer.rotation = Quaternion.Slerp(hammer.rotation, noImu, rotationLerp);
//            return;
//        }

//        float pitch = imu.euler.x;
//        float roll = imu.euler.y;
//        float yaw = imu.euler.z;

//        if (clampPitch) pitch = Mathf.Clamp(pitch, minPitchDeg, maxPitchDeg);

//        Quaternion imuRot = useIMURoll
//            ? Quaternion.Euler(pitch, yaw, roll)
//            : Quaternion.Euler(pitch, yaw, 0f);

//        Quaternion blended = Quaternion.Slerp(Quaternion.identity, imuRot, imuBlend);
//        Quaternion targetRot = handRot * blended * initialRotOffset;

//        hammer.rotation = Quaternion.Slerp(hammer.rotation, targetRot, rotationLerp);
//    }

//    /// <summary> יישור מחדש מול היד (כפתור נוח בזמן משחק). </summary>
//    public void RecalibrateInitialOffset()
//    {
//        if (!handReference || !hammer) return;
//        initialRotOffset = Quaternion.Inverse(handReference.rotation) * hammer.rotation;
//    }
//}

//using UnityEngine;

//[DisallowMultipleComponent]
//public class HammerFollower : MonoBehaviour
//{
//    [Header("Scene References")]
//    public Transform handReference;          // Anchor של היד/שלט
//    public Transform hammer;                 // אם ריק – יילקח מזה האובייקט
//    public Transform hammerGripPoint;        // אופסט אחיזה
//    public Transform wallTarget;             // נקודה/אובייקט על הקיר שהפטיש יסתכל אליו
//    public bool useHandForwardIfNoTarget = true;

//    [Header("IMU Source (translation only)")]
//    public IMUClientHammer imu;              // משתמשים רק ב-aMag_g (ללא סיבוב)
//    [Tooltip("סף כמה מעל 1g נחשב 'מכה'")]
//    public float accelThresholdG = 0.12f;
//    [Tooltip("כמה מטרים של דחיפה לכל יחידת ספייק")]
//    public float swingGain = 0.12f;
//    [Tooltip("מרחק מקסימלי קדימה/אחורה מהמיקום הבסיסי")]
//    public float swingMaxDistance = 0.35f;
//    [Tooltip("קפיץ (חזרה לאפס), גדול=חוזר מהר")]
//    public float returnSpring = 4.0f;
//    [Tooltip("דעיכה, גדול=מרגיע תנודות")]
//    public float returnDamping = 2.0f;

//    [Header("Follow – Position")]
//    [Range(0f, 1f)] public float positionLerp = 0.35f;
//    public bool clampFloor = true;
//    public float minY = 0.5f;

//    [Header("Follow – Rotation (NO IMU)")]
//    [Range(0f, 1f)] public float rotationLerp = 0.25f;
//    public Vector3 initialEulerOffset = Vector3.zero;

//    // --- פנימי ---
//    private Quaternion initialRotOffset;
//    private Rigidbody rb;
//    private float swingOffset;    // מ' לאורך הכיוון לקיר
//    private float swingVelocity;  // מהירות לאורך הכיוון לקיר

//    void Reset() { hammer = transform; }

//    void Awake()
//    {
//        if (!hammer) hammer = transform;

//        rb = hammer.GetComponent<Rigidbody>();
//        if (!rb) rb = hammer.gameObject.AddComponent<Rigidbody>();
//        rb.isKinematic = true;
//        rb.interpolation = RigidbodyInterpolation.Interpolate;
//        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

//        initialRotOffset = Quaternion.Euler(initialEulerOffset);
//    }

//    void Update()
//    {
//        if (!handReference || !hammer) return;

//        // 1) בסיס מיקום: הצמדה ליד + אופסט אחיזה
//        Vector3 gripOffset = hammerGripPoint ? (hammer.position - hammerGripPoint.position) : Vector3.zero;
//        Vector3 basePos = handReference.position + handReference.rotation * gripOffset;

//        // 2) כיוון לקיר (לסיבוב ולתזוזת הסווינג)
//        Vector3 dirToWall;
//        if (wallTarget) dirToWall = (wallTarget.position - basePos).normalized;
//        else if (useHandForwardIfNoTarget) dirToWall = handReference.forward.normalized;
//        else dirToWall = Vector3.forward; // פולבק

//        // 3) עדכון סווינג מה-IMU: דחיפה קדימה + קפיץ/דעיכה חזרה
//        if (imu != null)
//        {
//            // כמה מעל 1g (רק חיובי)
//            float overG = Mathf.Max(0f, imu.aMag_g - 1.0f - accelThresholdG);
//            // הופכים את הספייק לאימפולס קדימה
//            swingVelocity += overG * swingGain;

//            // דינמיקת קפיץ-דעיכה חזרה לאפס
//            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
//            float accel = -returnSpring * swingOffset - returnDamping * swingVelocity;
//            swingVelocity += accel * dt;
//            swingOffset += swingVelocity * dt;

//            // הגבלה למרחק
//            swingOffset = Mathf.Clamp(swingOffset, -swingMaxDistance * 0.15f, swingMaxDistance);
//        }
//        else
//        {
//            // בלי IMU – חוזר לאפס
//            swingVelocity = Mathf.MoveTowards(swingVelocity, 0f, 5f * Time.deltaTime);
//            swingOffset = Mathf.MoveTowards(swingOffset, 0f, 5f * Time.deltaTime);
//        }

//        // 4) מיקום סופי: בסיס + תזוזה לאורך הכיוון לקיר
//        Vector3 targetPos = basePos + dirToWall * swingOffset;
//        if (clampFloor && targetPos.y < minY) targetPos.y = minY;
//        hammer.position = Vector3.Lerp(hammer.position, targetPos, positionLerp);

//        // 5) סיבוב: תמיד אל הקיר (אין סיבוב מה-IMU)
//        Quaternion lookAtWall = Quaternion.LookRotation(dirToWall, Vector3.up) * initialRotOffset;
//        hammer.rotation = Quaternion.Slerp(hammer.rotation, lookAtWall, rotationLerp);
//    }

//    /// יישור מחדש אם צריך
//    public void RecalibrateInitialOffset()
//    {
//        if (!handReference || !hammer) return;
//        // מכוונים לפי המצב הנוכחי של הפטיש מול היד
//        initialRotOffset = Quaternion.Inverse(Quaternion.LookRotation(GetDirNow(), Vector3.up)) * hammer.rotation;
//    }

//    private Vector3 GetDirNow()
//    {
//        Vector3 gripOffset = hammerGripPoint ? (hammer.position - hammerGripPoint.position) : Vector3.zero;
//        Vector3 basePos = handReference.position + handReference.rotation * gripOffset;
//        if (wallTarget) return (wallTarget.position - basePos).normalized;
//        if (useHandForwardIfNoTarget) return handReference.forward.normalized;
//        return Vector3.forward;
//    }
//}


using UnityEngine;

/// <summary>
/// HammerFollower – מצמיד את הפטיש לעוגן היד/שלט, עם בלנד רוטציה מה-IMU (אופציונלי).
/// שימי על אב-הפטיש. ודאי שיש על האובייקט Rigidbody (isKinematic=true) + קוליידרים.
/// ראש הפטיש (ילד) צריך BoxCollider עם IsTrigger=true וסקריפט HammerTrigger.
/// </summary>
[DisallowMultipleComponent]
public class HammerFollower : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("Anchor של היד/שלט (למשל RightHandAnchor או RightControllerAnchor)")]
    public Transform handReference;

    [Tooltip("Transform של הפטיש (אם ריק – יילקח מזה האובייקט)")]
    public Transform hammer;

    [Tooltip("Empty על הידית לשמירת אופסט אחיזה טבעי (אופציונלי)")]
    public Transform hammerGripPoint;

    [Tooltip("מקור נתוני ה-IMU (אופציונלי) – נותן pitch/roll/yaw")]
    public IMUClientHammer imu;

    [Header("Follow – Position")]
    [Range(0f, 1f)] public float positionLerp = 0.6f;
    public bool clampFloor = true;
    public float minY = 0.5f;

    [Header("Follow – Rotation")]
    [Range(0f, 1f)] public float rotationLerp = 0.25f;
    [Range(0f, 1f)] public float imuBlend = 1f;     // 0=רק יד, 1=IMU מלא (יחסי ליד)
    public bool useIMURoll = false;                 // בד"כ כבוי ליציבות
    public Vector3 initialEulerOffset = Vector3.zero; // יישור ידני ראשוני

    [Header("Pitch Clamp (optional)")]
    public bool clampPitch = true;
    public float minPitchDeg = -60f;
    public float maxPitchDeg = 60f;

    [Header("Physics Stabilization")]
    [Tooltip("צעד מיקום מקסימלי לכל Fixed (מ' – מונע 'קפיצות' שחודרות קירות)")]
    public float maxLinearStepPerFixed = 0.06f;
    [Tooltip("צעד זווית מקסימלי לכל Fixed (מעלות)")]
    public float maxAngularStepPerFixed = 15f;

    // --- פנימי ---
    Quaternion initialRotOffset;
    Rigidbody rb;
    Vector3 _nextPos;
    Quaternion _nextRot;

    void Reset() { hammer = transform; }

    void Awake()
    {
        if (!hammer) hammer = transform;

        rb = hammer.GetComponent<Rigidbody>();
        if (!rb) rb = hammer.gameObject.AddComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        initialRotOffset = Quaternion.Euler(initialEulerOffset);

        // אתחול יעדים כדי למנוע קפיצה בפריים הראשון
        _nextPos = hammer.position;
        _nextRot = hammer.rotation;
    }

    void Update()
    {
        if (!handReference || !hammer || rb == null) return;

        // ----- Position: צמוד ליד עם אופסט אחיזה -----
        Vector3 gripOffset = Vector3.zero;
        if (hammerGripPoint)
            // אופסט מן הידית לפי מצב הפטיש הנוכחי
            gripOffset = rb.position - hammerGripPoint.position;

        Vector3 targetPos = handReference.position + handReference.rotation * gripOffset;
        if (clampFloor && targetPos.y < minY) targetPos.y = minY;

        // שומרים יעד; את ההזזה נעשה ב-FixedUpdate
        _nextPos = Vector3.Lerp(rb.position, targetPos, Mathf.Clamp01(positionLerp));

        // ----- Rotation: יד * (בלנד עם IMU) * אופסט -----
        Quaternion handRot = handReference.rotation;
        Quaternion targetRot;

        if (imu == null)
        {
            targetRot = handRot * initialRotOffset;
        }
        else
        {
            float pitch = imu.euler.x;
            float roll = imu.euler.y;
            float yaw = imu.euler.z;

            if (clampPitch)
                pitch = Mathf.Clamp(pitch, minPitchDeg, maxPitchDeg);

            Quaternion imuRot = useIMURoll
                ? Quaternion.Euler(pitch, yaw, roll)
                : Quaternion.Euler(pitch, yaw, 0f);

            Quaternion blended = Quaternion.Slerp(Quaternion.identity, imuRot, Mathf.Clamp01(imuBlend));
            targetRot = handRot * blended * initialRotOffset;
        }

        _nextRot = Quaternion.Slerp(rb.rotation, targetRot, Mathf.Clamp01(rotationLerp));
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        // הגבלת צעד לינארי כדי לא "לפרוץ" קוליידרים
        Vector3 fromPos = rb.position;
        Vector3 toPos = _nextPos;
        Vector3 delta = toPos - fromPos;
        float maxStep = Mathf.Max(0.0001f, maxLinearStepPerFixed);
        if (delta.magnitude > maxStep)
            toPos = fromPos + delta.normalized * maxStep;
        rb.MovePosition(toPos);

        // הגבלת צעד זוויתי
        Quaternion fromRot = rb.rotation;
        Quaternion toRot = _nextRot;
        float maxAng = Mathf.Max(0.1f, maxAngularStepPerFixed);
        rb.MoveRotation(Quaternion.RotateTowards(fromRot, toRot, maxAng));
    }

    /// <summary> יישור מחדש מול היד (כפתור נוח בזמן משחק). </summary>
    public void RecalibrateInitialOffset()
    {
        if (!handReference || !hammer) return;
        initialRotOffset = Quaternion.Inverse(handReference.rotation) * hammer.rotation;
    }
}


//using UnityEngine;

///// <summary>
///// HammerFollower – מצמיד את הפטיש ליד השחקן, עם בחירה אוטומטית של היד לפי מצב Controllers/Hands.
///// - אם שלט ימין פעיל => נצמד ליד שמאל (היד עם החיישן).
///// - אם שלט שמאל פעיל => נצמד ליד ימין.
///// - אם אין שלטים => בוחר יד לפי Hand Tracking והעדפת צד.
///// - שומר את כל לוגיקת ה-IMU לסיבוב, וה-Lerps למיקום/סיבוב.
///// שימי על האובייקט של הפטיש (האב). ודאי שיש Rigidbody isKinematic=true + Collider רגיל.
///// </summary>
//[DisallowMultipleComponent]
//public class HammerFollower : MonoBehaviour
//{
//    // ---------- בחירת יד אוטומטית ----------
//    public enum AutoPolicy
//    {
//        AutoOppositeActiveController, // ברירת מחדל: נצמד ליד ההפוכה לשלט הפעיל
//        PreferLeftIfNoControllers,    // אם אין שלטים: העדף שמאל (אם לא ב-Tracking -> ימין)
//        PreferRightIfNoControllers,   // אם אין שלטים: העדף ימין (אם לא ב-Tracking -> שמאל)
//        ForceLeft,                    // הכריחי שמאל (יעיל לניסוי)
//        ForceRight                    // הכריחי ימין
//    }

//    [Header("Auto Hand Selection")]
//    public AutoPolicy followPolicy = AutoPolicy.AutoOppositeActiveController;

//    [Tooltip("Anchor של יד שמאל (Hand Tracking) – בד\"כ LeftHandAnchor")]
//    public Transform leftHandAnchor;
//    [Tooltip("Anchor של יד ימין (Hand Tracking) – בד\"כ RightHandAnchor")]
//    public Transform rightHandAnchor;

//    [Tooltip("Anchor של שלט שמאל (אם תרצי להשתמש בו כגיבוי) – בד\"כ LeftControllerAnchor")]
//    public Transform leftControllerAnchor;
//    [Tooltip("Anchor של שלט ימין (אם תרצי להשתמש בו כגיבוי) – בד\"כ RightControllerAnchor")]
//    public Transform rightControllerAnchor;

//    [Tooltip("קומפוננטת OVRHand שמאל (לבדיקת IsTracked)")]
//    public OVRHand leftOVRHand;
//    [Tooltip("קומפוננטת OVRHand ימין (לבדיקת IsTracked)")]
//    public OVRHand rightOVRHand;

//    [Tooltip("אם היד שנבחרה לא ב-Tracking – האם ליפול חזרה ל-Anchor של השלט בצד הזה?")]
//    public bool fallbackToControllerAnchorIfHandNotTracked = true;

//    // --------- הישן: הפנייה ידנית (לא חובה יותר, נשמש בה רק אם Manual) ---------
//    [Header("Manual (optional – used only if ForceLeft/ForceRight)")]
//    [Tooltip("Transform של הפטיש (אם ריק – יילקח מזה האובייקט)")]
//    public Transform hammer;
//    [Tooltip("נקודת אחיזה (Empty כילד על הידית) לשמירת אופסט טבעי")]
//    public Transform hammerGripPoint;

//    [Header("IMU Source")]
//    [Tooltip("מקור נתוני ה-IMU (האובייקט עם IMUClientHammer)")]
//    public IMUClientHammer imu;

//    [Header("Follow – Position")]
//    [Range(0f, 1f)] public float positionLerp = 0.6f;
//    public bool clampFloor = true;
//    public float minY = 0.5f;

//    [Header("Follow – Rotation")]
//    [Range(0f, 1f)] public float rotationLerp = 0.25f;
//    [Range(0f, 1f)] public float imuBlend = 1f;       // 0=רק יד, 1=IMU מלא (יחסי ליד)
//    public bool useIMURoll = false;                   // בד"כ כבוי ליציבות
//    public Vector3 initialEulerOffset = Vector3.zero; // יישור ידני ראשוני
//    public bool clampPitch = true;
//    public float minPitchDeg = -60f;
//    public float maxPitchDeg = 60f;

//    // debug
//    [Header("Debug")]
//    [ReadOnlyInInspector] public string chosenSide; // "Left"/"Right"
//    [ReadOnlyInInspector] public Transform effectiveHandReference; // העוגן הפעיל בפועל

//    Quaternion initialRotOffset;
//    Rigidbody rb;

//    void Reset() { hammer = transform; }

//    void Awake()
//    {
//        if (hammer == null) hammer = transform;

//        rb = hammer.GetComponent<Rigidbody>();
//        if (!rb) rb = hammer.gameObject.AddComponent<Rigidbody>();

//        rb.isKinematic = true;
//        rb.interpolation = RigidbodyInterpolation.Interpolate;
//        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

//        initialRotOffset = Quaternion.Euler(initialEulerOffset);
//    }

//    void Update()
//    {
//        // 1) לבחור יד "מנצחת" בכל פריים
//        effectiveHandReference = ResolveHandReference(out bool leftSelected);

//        chosenSide = leftSelected ? "Left" : "Right";
//        if (!effectiveHandReference) return;

//        // 2) Position – הצמדה ליד + אופסט אחיזה
//        Vector3 gripOffset = Vector3.zero;
//        if (hammerGripPoint) gripOffset = hammer.position - hammerGripPoint.position;

//        Vector3 targetPos = effectiveHandReference.position + effectiveHandReference.rotation * gripOffset;
//        if (clampFloor && targetPos.y < minY) targetPos.y = minY;

//        hammer.position = Vector3.Lerp(hammer.position, targetPos, positionLerp);

//        // 3) Rotation – יד * (בלנד עם IMU) * אופסט
//        Quaternion handRot = effectiveHandReference.rotation;

//        if (imu == null)
//        {
//            Quaternion noImu = handRot * initialRotOffset;
//            hammer.rotation = Quaternion.Slerp(hammer.rotation, noImu, rotationLerp);
//            return;
//        }

//        float pitch = imu.euler.x;
//        float roll = imu.euler.y;
//        float yaw = imu.euler.z;

//        if (clampPitch) pitch = Mathf.Clamp(pitch, minPitchDeg, maxPitchDeg);

//        Quaternion imuRot = useIMURoll
//            ? Quaternion.Euler(pitch, yaw, roll)
//            : Quaternion.Euler(pitch, yaw, 0f);

//        Quaternion blended = Quaternion.Slerp(Quaternion.identity, imuRot, imuBlend);
//        Quaternion targetRot = handRot * blended * initialRotOffset;

//        hammer.rotation = Quaternion.Slerp(hammer.rotation, targetRot, rotationLerp);
//    }

//    /// <summary> בוחר Transform של היד לעקוב אחריה לפי מצב Controllers/Hands והמדיניות. </summary>
//    Transform ResolveHandReference(out bool leftSelected)
//    {
//        // סטטוס שלטים
//        var connected = OVRInput.GetConnectedControllers();
//        bool leftCtrlConnected = (connected & OVRInput.Controller.LTouch) != 0;
//        bool rightCtrlConnected = (connected & OVRInput.Controller.RTouch) != 0;

//        bool leftCtrlTracked = leftCtrlConnected &&
//                                OVRInput.GetControllerPositionTracked(OVRInput.Controller.LTouch) &&
//                                OVRInput.GetControllerOrientationTracked(OVRInput.Controller.LTouch);

//        bool rightCtrlTracked = rightCtrlConnected &&
//                                OVRInput.GetControllerPositionTracked(OVRInput.Controller.RTouch) &&
//                                OVRInput.GetControllerOrientationTracked(OVRInput.Controller.RTouch);

//        // סטטוס ידיים
//        bool leftHandTracked = leftOVRHand ? leftOVRHand.IsTracked : true; // אם לא הוקצה – נניח true
//        bool rightHandTracked = rightOVRHand ? rightOVRHand.IsTracked : true;

//        // בחירת צד לפי המדיניות
//        switch (followPolicy)
//        {
//            case AutoPolicy.ForceLeft:
//                leftSelected = true;
//                return ChooseAnchorForSide(true, leftHandTracked, leftControllerAnchor, leftHandAnchor);

//            case AutoPolicy.ForceRight:
//                leftSelected = false;
//                return ChooseAnchorForSide(false, rightHandTracked, rightControllerAnchor, rightHandAnchor);

//            case AutoPolicy.PreferLeftIfNoControllers:
//                {
//                    if (!leftCtrlTracked && !rightCtrlTracked)
//                    {
//                        // אין שלטים ⇒ העדף שמאל אם ב-Tracking, אחרת ימין
//                        if (leftHandTracked) { leftSelected = true; return leftHandAnchor ? leftHandAnchor : leftControllerAnchor; }
//                        if (rightHandTracked) { leftSelected = false; return rightHandAnchor ? rightHandAnchor : rightControllerAnchor; }
//                        // אין Hand Tracking בכלל ⇒ נפילה לעוגני שלטים (אם קיימים)
//                        leftSelected = true;
//                        return leftControllerAnchor ? leftControllerAnchor : rightControllerAnchor;
//                    }
//                    // אם יש שלט פעיל בצד אחד – נלך לצד השני (המחזיק חיישן)
//                    if (rightCtrlTracked && !leftCtrlTracked) { leftSelected = true; return ChooseAnchorForSide(true, leftHandTracked, leftControllerAnchor, leftHandAnchor); }
//                    if (leftCtrlTracked && !rightCtrlTracked) { leftSelected = false; return ChooseAnchorForSide(false, rightHandTracked, rightControllerAnchor, rightHandAnchor); }
//                    // שני שלטים או אמביוולנטי ⇒ ברירת מחדל שמאל
//                    leftSelected = true;
//                    return ChooseAnchorForSide(true, leftHandTracked, leftControllerAnchor, leftHandAnchor);
//                }

//            case AutoPolicy.PreferRightIfNoControllers:
//                {
//                    if (!leftCtrlTracked && !rightCtrlTracked)
//                    {
//                        if (rightHandTracked) { leftSelected = false; return rightHandAnchor ? rightHandAnchor : rightControllerAnchor; }
//                        if (leftHandTracked) { leftSelected = true; return leftHandAnchor ? leftHandAnchor : leftControllerAnchor; }
//                        leftSelected = false;
//                        return rightControllerAnchor ? rightControllerAnchor : leftControllerAnchor;
//                    }
//                    if (rightCtrlTracked && !leftCtrlTracked) { leftSelected = true; return ChooseAnchorForSide(true, leftHandTracked, leftControllerAnchor, leftHandAnchor); }
//                    if (leftCtrlTracked && !rightCtrlTracked) { leftSelected = false; return ChooseAnchorForSide(false, rightHandTracked, rightControllerAnchor, rightHandAnchor); }
//                    // שני שלטים ⇒ ברירת מחדל ימין
//                    leftSelected = false;
//                    return ChooseAnchorForSide(false, rightHandTracked, rightControllerAnchor, rightHandAnchor);
//                }

//            case AutoPolicy.AutoOppositeActiveController:
//            default:
//                {
//                    // אם רק צד אחד עם שלט פעיל ⇒ נבחר את היד ההפוכה (מחזיקת חיישן)
//                    if (rightCtrlTracked && !leftCtrlTracked)
//                    {
//                        leftSelected = true;
//                        return ChooseAnchorForSide(true, leftHandTracked, leftControllerAnchor, leftHandAnchor);
//                    }
//                    if (leftCtrlTracked && !rightCtrlTracked)
//                    {
//                        leftSelected = false;
//                        return ChooseAnchorForSide(false, rightHandTracked, rightControllerAnchor, rightHandAnchor);
//                    }

//                    // אין שלטים פעילים ⇒ נבחר יד לפי Hand Tracking (או ברירת מחדל שמאל)
//                    if (leftHandTracked && leftHandAnchor) { leftSelected = true; return leftHandAnchor; }
//                    if (rightHandTracked && rightHandAnchor) { leftSelected = false; return rightHandAnchor; }

//                    // אין Hand Tracking ⇒ ניפול לעוגני שלטים אם קיימים
//                    if (leftControllerAnchor) { leftSelected = true; return leftControllerAnchor; }
//                    if (rightControllerAnchor) { leftSelected = false; return rightControllerAnchor; }

//                    // אין כלום מוגדר
//                    leftSelected = true;
//                    return null;
//                }
//        }
//    }

//    /// <summary>
//    /// בוחר עוגן עבור צד נתון: אם היד ב-Tracking – handAnchor; אחרת (אם מותר) controllerAnchor.
//    /// </summary>
//    Transform ChooseAnchorForSide(bool leftSide, bool handTracked, Transform ctrlAnchor, Transform handAnchor)
//    {
//        if (handTracked && handAnchor) return handAnchor;
//        if (fallbackToControllerAnchorIfHandNotTracked && ctrlAnchor) return ctrlAnchor;
//        // אם אין יד ואין גיבוי לשלט – נחזיר מה שיש
//        return handAnchor ? handAnchor : ctrlAnchor;
//    }

//    /// <summary> יישור מחדש מול היד (לחצן בזמן משחק). </summary>
//    public void RecalibrateInitialOffset()
//    {
//        if (!effectiveHandReference || !hammer) return;
//        initialRotOffset = Quaternion.Inverse(effectiveHandReference.rotation) * hammer.rotation;
//    }
//}

//#region Helper Attribute (read-only in inspector)
//public class ReadOnlyInInspectorAttribute : PropertyAttribute { }
//#if UNITY_EDITOR
//[UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyInInspectorAttribute))]
//public class ReadOnlyInInspectorDrawer : UnityEditor.PropertyDrawer
//{
//    public override float GetPropertyHeight(UnityEditor.SerializedProperty property, GUIContent label)
//        => UnityEditor.EditorGUI.GetPropertyHeight(property, label, true);
//    public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
//    {
//        var prev = GUI.enabled; GUI.enabled = false;
//        UnityEditor.EditorGUI.PropertyField(position, property, label, true);
//        GUI.enabled = prev;
//    }
//}
//#endif
//#endregion


//using UnityEngine;

//[DisallowMultipleComponent]
//public class HammerFollower : MonoBehaviour
//{
//    // ---------- בחירת יד אוטומטית ----------
//    public enum AutoPolicy
//    {
//        AutoOppositeActiveController, // אם ימין מוחזק -> נלך לשמאל (החיישן), ולהפך
//        PreferLeftIfNoControllers,
//        PreferRightIfNoControllers,
//        ForceLeft,
//        ForceRight
//    }
//    // הוסיפי שדות (למעלה)
//    [Header("Physics Stabilization")]
//    public float maxLinearStepPerFixed = 0.25f;     // כמה מטרים מותר לזוז ב-Fixed (למנוע "קפיצות")
//    public float maxAngularStepPerFixed = 45f;      // כמה מעלות מותר לסובב ב-Fixed
//    private Vector3 _nextPos;
//    private Quaternion _nextRot;


//    [Header("Auto Hand Selection")]
//    public AutoPolicy followPolicy = AutoPolicy.AutoOppositeActiveController;

//    [Tooltip("Anchor של יד שמאל (Hand Tracking)")]
//    public Transform leftHandAnchor;
//    [Tooltip("Anchor של יד ימין (Hand Tracking)")]
//    public Transform rightHandAnchor;

//    [Tooltip("Anchor של שלט שמאל (גיבוי בלבד)")]
//    public Transform leftControllerAnchor;
//    [Tooltip("Anchor של שלט ימין (גיבוי בלבד)")]
//    public Transform rightControllerAnchor;

//    [Tooltip("קומפוננטת OVRHand שמאל (ל-IsTracked)")]
//    public OVRHand leftOVRHand;
//    [Tooltip("קומפוננטת OVRHand ימין (ל-IsTracked)")]
//    public OVRHand rightOVRHand;

//    [Header("Controller 'Held' Detection")]
//    [Tooltip("כמה ללחוץ Grip/Trigger כדי להיחשב 'מוחזק'")]
//    [Range(0f, 1f)] public float holdThreshold = 0.25f;
//    [Tooltip("התחשבות גם במגע קיבולי (אגודל/אצבע) כדי לזהות אחיזה קלה")]
//    public bool useCapacitiveTouch = true;

//    [Tooltip("אם יד לא ב-Tracking: ליפול לעוגן של השלט באותו צד?")]
//    public bool fallbackToControllerAnchorIfHandNotTracked = false;

//    [Header("Head Fallback (רשות)")]
//    [Tooltip("עוגן הראש (CenterEyeAnchor) לפולבקים של כתף שמאל/ימין)")]
//    public Transform headAnchor;
//    public bool useShoulderFallback = true;
//    public Vector3 leftShoulderOffsetLocal = new Vector3(-0.18f, -0.20f, 0.08f);
//    public Vector3 rightShoulderOffsetLocal = new Vector3(0.18f, -0.20f, 0.08f);

//    // --------- IMU + Follow ---------
//    [Header("Manual / Object Refs")]
//    public Transform hammer;           // אם ריק – יילקח מזה האובייקט
//    public Transform hammerGripPoint;

//    [Header("IMU Source")]
//    public IMUClientHammer imu;

//    [Header("Follow – Position")]
//    [Range(0f, 1f)] public float positionLerp = 0.6f;
//    public bool clampFloor = true;
//    public float minY = 0.5f;

//    [Header("Follow – Rotation")]
//    [Range(0f, 1f)] public float rotationLerp = 0.25f;
//    [Range(0f, 1f)] public float imuBlend = 1f;
//    public bool useIMURoll = false;
//    public Vector3 initialEulerOffset = Vector3.zero;
//    public bool clampPitch = true;
//    public float minPitchDeg = -60f;
//    public float maxPitchDeg = 60f;

//    // Debug
//    [Header("Debug (read-only)")]
//    [SerializeField] string chosenSide;
//    Transform effectiveHandReference;

//    Quaternion initialRotOffset;
//    Rigidbody rb;

//    void Reset() { hammer = transform; }

//    void Awake()
//    {
//        if (!hammer) hammer = transform;

//        rb = hammer.GetComponent<Rigidbody>();
//        if (!rb) rb = hammer.gameObject.AddComponent<Rigidbody>();
//        rb.isKinematic = true;
//        rb.interpolation = RigidbodyInterpolation.Interpolate;
//        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

//        initialRotOffset = Quaternion.Euler(initialEulerOffset);

//        // בסוף Awake()
//        _nextPos = rb.position;
//        _nextRot = rb.rotation;

//    }

//    //void Update()
//    //{
//    //    effectiveHandReference = ResolveHandReference(out bool leftSelected);
//    //    chosenSide = leftSelected ? "Left" : "Right";

//    //    if (!effectiveHandReference) return;

//    //    // ---- Position (צמוד ליד + אופסט אחיזה) ----
//    //    Vector3 gripOffset = hammerGripPoint ? (hammer.position - hammerGripPoint.position) : Vector3.zero;
//    //    Vector3 targetPos = effectiveHandReference.position + effectiveHandReference.rotation * gripOffset;

//    //    if (clampFloor && targetPos.y < minY) targetPos.y = minY;
//    //    hammer.position = Vector3.Lerp(hammer.position, targetPos, positionLerp);

//    //    // ---- Rotation (יד * בלנד עם IMU * אופסט) ----
//    //    Quaternion handRot = effectiveHandReference.rotation;

//    //    if (!imu)
//    //    {
//    //        hammer.rotation = Quaternion.Slerp(hammer.rotation, handRot * initialRotOffset, rotationLerp);
//    //        return;
//    //    }

//    //    float pitch = imu.euler.x;
//    //    float roll = imu.euler.y;
//    //    float yaw = imu.euler.z;
//    //    if (clampPitch) pitch = Mathf.Clamp(pitch, minPitchDeg, maxPitchDeg);

//    //    Quaternion imuRot = useIMURoll ? Quaternion.Euler(pitch, yaw, roll)
//    //                                   : Quaternion.Euler(pitch, yaw, 0f);

//    //    Quaternion blended = Quaternion.Slerp(Quaternion.identity, imuRot, imuBlend);
//    //    Quaternion targetRot = handRot * blended * initialRotOffset;

//    //    hammer.rotation = Quaternion.Slerp(hammer.rotation, targetRot, rotationLerp);
//    //}
//    void Update()
//    {
//        // לבחור את היד/עוגן הפעיל לפי המדיניות
//        effectiveHandReference = ResolveHandReference(out bool leftSelected);
//        chosenSide = leftSelected ? "Left" : "Right";

//        if (!hammer || rb == null || !effectiveHandReference)
//        {
//            // אם אין רפרנסים, תשמרי את המצב הנוכחי כיעד כדי לא "לקפוץ"
//            _nextPos = rb ? rb.position : transform.position;
//            _nextRot = rb ? rb.rotation : transform.rotation;
//            return;
//        }

//        // ----- POSITION: יד + אופסט אחיזה -----
//        Vector3 gripOffset = Vector3.zero;
//        if (hammerGripPoint != null)
//            // אופסט מן הידית (לפי המיקום הנוכחי של הריגידבודי)
//            gripOffset = rb.position - hammerGripPoint.position;

//        Vector3 targetPos = effectiveHandReference.position
//                          + effectiveHandReference.rotation * gripOffset;

//        if (clampFloor && targetPos.y < minY)
//            targetPos.y = minY;

//        // החלקה קלה לפני ה־FixedUpdate, ואז ב־FixedUpdate גם נגביל צעד
//        _nextPos = Vector3.Lerp(rb.position, targetPos, Mathf.Clamp01(positionLerp));

//        // ----- ROTATION: רוטציית יד * (בלנד עם IMU) * אופסט -----
//        Quaternion handRot = effectiveHandReference.rotation;
//        Quaternion targetRot;

//        if (imu == null)
//        {
//            targetRot = handRot * initialRotOffset;
//        }
//        else
//        {
//            float pitch = imu.euler.x;
//            float roll = imu.euler.y;
//            float yaw = imu.euler.z;

//            if (clampPitch)
//                pitch = Mathf.Clamp(pitch, minPitchDeg, maxPitchDeg);

//            Quaternion imuRot = useIMURoll
//                ? Quaternion.Euler(pitch, yaw, roll)
//                : Quaternion.Euler(pitch, yaw, 0f);

//            Quaternion blended = Quaternion.Slerp(
//                Quaternion.identity,
//                imuRot,
//                Mathf.Clamp01(imuBlend)
//            );

//            targetRot = handRot * blended * initialRotOffset;
//        }

//        _nextRot = Quaternion.Slerp(rb.rotation, targetRot, Mathf.Clamp01(rotationLerp));
//    }



//    void FixedUpdate()
//    {
//        if (!hammer || rb == null) return;

//        // Clip linear step
//        Vector3 fromPos = rb.position;
//        Vector3 toPos = _nextPos;
//        Vector3 delta = toPos - fromPos;
//        float maxStep = maxLinearStepPerFixed;
//        if (delta.magnitude > maxStep) toPos = fromPos + delta.normalized * maxStep;
//        rb.MovePosition(toPos);

//        // Clip angular step
//        Quaternion fromRot = rb.rotation;
//        Quaternion toRot = _nextRot;
//        float maxAng = maxAngularStepPerFixed;
//        rb.MoveRotation(Quaternion.RotateTowards(fromRot, toRot, maxAng));
//    }


//    // ====== בחירה חכמה של היד לעקוב אחריה ======
//    Transform ResolveHandReference(out bool leftSelected)
//    {
//        // 1) האם כל צד מוחזק בפועל? (לא רק טרקינג)
//        bool leftHeld = ControllerActuallyHeld(true);
//        bool rightHeld = ControllerActuallyHeld(false);

//        // 2) סטטוס Hand-Tracking
//        bool leftHandTracked = leftOVRHand ? leftOVRHand.IsTracked : false;
//        bool rightHandTracked = rightOVRHand ? rightOVRHand.IsTracked : false;

//        // 3) מדיניות
//        switch (followPolicy)
//        {
//            case AutoPolicy.ForceLeft:
//                leftSelected = true;
//                return AnchorForSide(true, leftHandTracked);

//            case AutoPolicy.ForceRight:
//                leftSelected = false;
//                return AnchorForSide(false, rightHandTracked);

//            case AutoPolicy.AutoOppositeActiveController:
//            default:
//                // אם ימין מוחזק -> נלך לשמאל; אם שמאל מוחזק -> נלך לימין
//                if (rightHeld && !leftHeld) { leftSelected = true; return AnchorForSide(true, leftHandTracked); }
//                if (leftHeld && !rightHeld) { leftSelected = false; return AnchorForSide(false, rightHandTracked); }

//                // אם אין שלטים מוחזקים:
//                if (!leftHeld && !rightHeld)
//                {
//                    // בחרי יד עם Hand-Tracking פעיל, אחרת פולבק
//                    if (leftHandTracked) { leftSelected = true; return leftHandAnchor; }
//                    if (rightHandTracked) { leftSelected = false; return rightHandAnchor; }

//                    // אין Hand-Tracking: פולבק נוח לכתף-שמאל/ימין מהראש
//                    if (useShoulderFallback && headAnchor)
//                    {
//                        leftSelected = true;
//                        return ShoulderFallback(true);
//                    }

//                    // פולבק אחרון: אם ביקשת – לעוגני שלטים
//                    if (fallbackToControllerAnchorIfHandNotTracked)
//                    {
//                        if (leftControllerAnchor) { leftSelected = true; return leftControllerAnchor; }
//                        if (rightControllerAnchor) { leftSelected = false; return rightControllerAnchor; }
//                    }

//                    leftSelected = true;
//                    return null;
//                }

//                // שני שלטים מוחזקים (מצב נדיר אצלך) – בחרי יד ברירת-מחדל:
//                leftSelected = false;
//                return AnchorForSide(false, rightHandTracked);

//            case AutoPolicy.PreferLeftIfNoControllers:
//                if (!leftHeld && !rightHeld)
//                {
//                    if (leftHandTracked) { leftSelected = true; return leftHandAnchor; }
//                    if (rightHandTracked) { leftSelected = false; return rightHandAnchor; }
//                    if (useShoulderFallback && headAnchor) { leftSelected = true; return ShoulderFallback(true); }
//                    if (fallbackToControllerAnchorIfHandNotTracked && leftControllerAnchor) { leftSelected = true; return leftControllerAnchor; }
//                    leftSelected = true; return null;
//                }
//                if (rightHeld && !leftHeld) { leftSelected = true; return AnchorForSide(true, leftHandTracked); }
//                if (leftHeld && !rightHeld) { leftSelected = false; return AnchorForSide(false, rightHandTracked); }
//                leftSelected = true; return AnchorForSide(true, leftHandTracked);

//            case AutoPolicy.PreferRightIfNoControllers:
//                if (!leftHeld && !rightHeld)
//                {
//                    if (rightHandTracked) { leftSelected = false; return rightHandAnchor; }
//                    if (leftHandTracked) { leftSelected = true; return leftHandAnchor; }
//                    if (useShoulderFallback && headAnchor) { leftSelected = false; return ShoulderFallback(false); }
//                    if (fallbackToControllerAnchorIfHandNotTracked && rightControllerAnchor) { leftSelected = false; return rightControllerAnchor; }
//                    leftSelected = false; return null;
//                }
//                if (rightHeld && !leftHeld) { leftSelected = true; return AnchorForSide(true, leftHandTracked); }
//                if (leftHeld && !rightHeld) { leftSelected = false; return AnchorForSide(false, rightHandTracked); }
//                leftSelected = false; return AnchorForSide(false, rightHandTracked);
//        }
//    }

//    // מחזיר אם *באמת* מחזיקים שלט (לחיצת Grip/Trigger/Touch), לא רק “יש טרקינג”.
//    bool ControllerActuallyHeld(bool left)
//    {
//        var c = left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;

//        // קודם וידוא טרקינג בסיסי
//        bool tracked =
//            OVRInput.GetControllerPositionTracked(c) &&
//            OVRInput.GetControllerOrientationTracked(c);

//        if (!tracked) return false;

//        float grip = left ? OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger)
//                            : OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger);
//        float index = left ? OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger)
//                            : OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger);

//        bool pressed =
//            grip > holdThreshold ||
//            index > holdThreshold ||
//            OVRInput.Get(left ? OVRInput.Button.PrimaryThumbstick : OVRInput.Button.SecondaryThumbstick) ||
//            OVRInput.Get(left ? OVRInput.Button.One : OVRInput.Button.Three) ||
//            OVRInput.Get(left ? OVRInput.Button.Two : OVRInput.Button.Four);

//        if (useCapacitiveTouch)
//        {
//            pressed |=
//                OVRInput.Get(left ? OVRInput.NearTouch.PrimaryThumbButtons : OVRInput.NearTouch.SecondaryThumbButtons) ||
//                OVRInput.Get(left ? OVRInput.NearTouch.PrimaryIndexTrigger : OVRInput.NearTouch.SecondaryIndexTrigger);
//        }

//        return pressed;
//    }

//    // בוחר עוגן יד/שלט לצד נתון, כולל פולבק לכתף אם ביקשת
//    Transform AnchorForSide(bool leftSide, bool handTracked)
//    {
//        Transform handA = leftSide ? leftHandAnchor : rightHandAnchor;
//        Transform ctrlA = leftSide ? leftControllerAnchor : rightControllerAnchor;

//        if (handTracked && handA) return handA;

//        if (useShoulderFallback && headAnchor)
//            return ShoulderFallback(leftSide);

//        if (fallbackToControllerAnchorIfHandNotTracked && ctrlA)
//            return ctrlA;

//        return handA ? handA : ctrlA;
//    }

//    // פולבק “כתף” מתוך הראש (מרגיע כשאין Hand-Tracking ואין שלט מוחזק)
//    Transform ShoulderFallback(bool leftSide)
//    {
//        if (!headAnchor) return null;

//        Vector3 local = leftSide ? leftShoulderOffsetLocal : rightShoulderOffsetLocal;
//       // ניצור עוגן זמני/סטטי אם תרצי – כאן נחשב מיקום/כיוון ישירות:
//        _tmpAnchor.position = headAnchor.TransformPoint(local);
//        _tmpAnchor.rotation = Quaternion.LookRotation(headAnchor.forward, Vector3.up);
//        return _tmpAnchor;
//    }

//    // עוגן זמני לשימוש פנימי (לא לשכוח ליצור ב-Awake)
//    Transform _tmpAnchor;
//    void OnEnable()
//    {
//        if (_tmpAnchor == null)
//        {
//            var go = new GameObject("HammerFollower_TempAnchor");
//            go.hideFlags = HideFlags.HideAndDontSave;
//            _tmpAnchor = go.transform;
//        }
//    }

//    public void RecalibrateInitialOffset()
//    {
//        if (!effectiveHandReference || !hammer) return;
//        initialRotOffset = Quaternion.Inverse(effectiveHandReference.rotation) * hammer.rotation;
//    }
//}
