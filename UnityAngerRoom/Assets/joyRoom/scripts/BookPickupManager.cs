using UnityEngine;

[RequireComponent(typeof(Transform))]
public class BookPickupManager : MonoBehaviour
{
    public enum BookState { OnAnchor, InRightHand, InLeftHand }

    [Header("References")]
    public EspImuReader imuReader;      // גרור את ה-EspImuReader
    public Transform anchor;            // עוגן חיצוני (לא ילד של הספר)
    public Transform rightHand;         // ה-Transform של הבקר האמיתי (שזז בבילד)
    public Transform leftHand;

    [Header("Snap Settings")]
    public Vector3 handLocalPosOffset   = new Vector3(0f, -0.05f, 0.08f);
    public Vector3 handLocalEulerOffset = new Vector3(0f, 90f, 0f);

    [Header("Anchor Orientation While OnAnchor")]
    public bool applyImuRotationOnAnchor = true;
    [Range(0f, 1f)] public float anchorRotSmoothing = 0.35f;
    public Vector3 anchorEulerOffset;

    [Header("Auto snap logic")]
    public bool neverAutoSnapAtStart = false;      // תשאיר FALSE כדי לאפשר הצמדה אוטומטית (מוגן ע"י gating/arming)
    public bool autoSnapOnMotion = true;           // פולבק: יתפוס לפי isMovingNow מה-Reader
    [Range(0.05f, 1f)] public float snapRetryEvery = 0.25f;

    [Header("Auto snap – fallback by tilt")]
    public bool useRotationTiltSnap = true;
    [Range(1f, 45f)] public float tiltDegToSnap = 10f;     // הטיית IMU ביחס לעוגן כדי "לקחת"
    [Range(0.05f, 1f)] public float tiltCheckEvery = 0.15f;

    [Header("Arming before auto-snap")]
    public bool requireCalmArming = true;           // דרוש פרק שקט לפני שמתירים auto-snap
    [Range(0.1f, 3f)] public float calmArmSeconds = 1.0f;

    [Header("Startup gating")]
    public float autoSnapDelaySeconds = 1.5f;       // דילי אחרי Play שבו אסור להצמיד
    float startTime;

    [Header("Auto snap – emergency trigger")]
    public bool enableEmergencyAutoSnap = true;     // עוקף חסמים: תופס אם יש תנועה/הטיה
    [Range(0f, 0.5f)] public float accelDeltaToSnap = 0.06f; // שמור לשימוש עתידי אם תחשוף |a| מה-Reader

    [Header("Build Safety / Debug")]
    public bool showHud = true;
    public KeyCode forceSnapKey = KeyCode.F;        // קיצור לבדיקה ידנית
    public KeyCode resetToAnchorKey = KeyCode.R;
    public float outOfBoundsY = -5f;
    public float tooFarDistance = 25f;

    // Internal
    BookState  state = BookState.OnAnchor;
    Quaternion rotSmoothed;
    Rigidbody  rb;
    float lastSnapAttempt;
    bool  subscribed;
    float lastTiltCheck;

    // arming
    bool  armed;         // האם מותר כבר לתפוס
    float calmAccum;     // זמן שקט מצטבר

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.useGravity = false;
        rb.isKinematic = true;

        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = true;
        gameObject.SetActive(true);
    }

    void Start()
    {
        if (!anchor) { Debug.LogError("[BookPickup] Anchor לא הוגדר!"); enabled = false; return; }
        if (anchor.IsChildOf(transform)) { Debug.LogError("[BookPickup] Anchor הוא ילד של הספר!"); enabled = false; return; }

        transform.SetParent(null, true);
        transform.SetPositionAndRotation(anchor.position, anchor.rotation);
        rotSmoothed = transform.rotation;
        state = BookState.OnAnchor;

        // startup gating
        startTime = Time.time;

        // arming init
        armed = !requireCalmArming;  // אם דורשים שקט – מתחילים לא דרוכים
        calmAccum = 0f;

        if (imuReader && !subscribed) { imuReader.OnLift += OnLiftDetected; subscribed = true; }

        Debug.Log($"[BookPickup] Ready. AutoSnap={(!neverAutoSnapAtStart)}, MotionSnap={autoSnapOnMotion}, TiltSnap={useRotationTiltSnap}, RequireCalm={requireCalmArming} ({calmArmSeconds:F1}s), StartupDelay={autoSnapDelaySeconds:F1}s, Emergency={enableEmergencyAutoSnap}");
    }

    void OnDisable()
    {
        if (imuReader && subscribed) { imuReader.OnLift -= OnLiftDetected; subscribed = false; }
    }

    // טריגר מאירוע OnLift של ה-Reader (שקט→תנועה)
    void OnLiftDetected()
    {
        if (neverAutoSnapAtStart) return;
        if (!IsAutoSnapAllowedNow()) return;   // gating + arming
        if (state != BookState.OnAnchor) return;
        TrySnap("OnLift");
    }

    void Update()
    {
        // קיצורי מקלדת
        if (Input.GetKeyDown(forceSnapKey))  TrySnap("Key F");
        if (Input.GetKeyDown(resetToAnchorKey)) ReturnToAnchor();

        // בטיחות: נפילה/NaN/בריחה
        if (!float.IsFinite(transform.position.sqrMagnitude) ||
            transform.position.y < outOfBoundsY ||
            (Camera.main && Vector3.Distance(Camera.main.transform.position, transform.position) > tooFarDistance))
        {
            ReturnToAnchor();
        }

        if (state == BookState.OnAnchor)
        {
            // מקבעים לעוגן
            transform.position = anchor.position;

            // סיבוב לפי ה-IMU בזמן בעוגן (אופציונלי)
            if (applyImuRotationOnAnchor && imuReader)
            {
                var targetRot = imuReader.desiredRotation * Quaternion.Euler(anchorEulerOffset);
                rotSmoothed = Quaternion.Slerp(rotSmoothed, targetRot, anchorRotSmoothing);
                transform.rotation = rotSmoothed;
            }
            else
            {
                transform.rotation = anchor.rotation;
            }

            // אם עדיין ב-delay של ההתחלה – אל תנסה להצמיד בכלל
            if (!IsPastStartupDelay()) return;

            // ---- Arming: צריך "שקט" לפני שמתירים auto-snap ----
            if (requireCalmArming && imuReader)
            {
                if (!imuReader.isMovingNow) {
                    calmAccum += Time.deltaTime;
                    if (!armed && calmAccum >= calmArmSeconds) {
                        armed = true; // עכשיו מותר לתפוס
                    }
                } else {
                    calmAccum = 0f; // תנועה מבטלת צבירת שקט
                }
            }

            // פולבק: אם יש תנועה (isMovingNow) ננסה להיצמד במרווחי זמן
            if (!neverAutoSnapAtStart && autoSnapOnMotion && IsAutoSnapAllowedNow() && imuReader && imuReader.isMovingNow)
            {
                if (Time.time - lastSnapAttempt >= snapRetryEvery)
                    TrySnap("Polling");
            }

            // Fallback נוסף: הטיה יחסית לעוגן
            if (useRotationTiltSnap && IsAutoSnapAllowedNow() && imuReader && !neverAutoSnapAtStart)
            {
                if (Time.time - lastTiltCheck >= tiltCheckEvery)
                {
                    lastTiltCheck = Time.time;
                    var desiredOnAnchor = imuReader.desiredRotation * Quaternion.Euler(anchorEulerOffset);
                    float ang = Quaternion.Angle(desiredOnAnchor, anchor.rotation);
                    if (ang >= tiltDegToSnap)
                        TrySnap("TiltFallback");
                }
            }

            // --- Emergency: אם יש תנועה או הטיה אמיתית - נתפוס ליד מיד ---
            if (enableEmergencyAutoSnap && IsAutoSnapAllowedNow() && imuReader && !neverAutoSnapAtStart)
            {
                var desiredOnAnchor = imuReader.desiredRotation * Quaternion.Euler(anchorEulerOffset);
                float ang = Quaternion.Angle(desiredOnAnchor, anchor.rotation);
                bool moving = imuReader.isMovingNow;  // סיגנל תנועה כללי מה-Reader

                if (ang >= tiltDegToSnap || moving)
                {
                    TrySnap("EmergencyAuto");
                }
            }
        }
    }

    bool IsAutoSnapAllowedNow()
    {
        return IsPastStartupDelay() && (!requireCalmArming || armed);
    }

    bool IsPastStartupDelay()
    {
        return (Time.time - startTime) >= autoSnapDelaySeconds;
    }

    void LateUpdate()
    {
        if (state == BookState.InRightHand && IsValidHand(rightHand)) ApplyHandPose(rightHand);
        else if (state == BookState.InLeftHand && IsValidHand(leftHand)) ApplyHandPose(leftHand);
        else if (state != BookState.OnAnchor) ReturnToAnchor(); // אם היד "נעלמה" – חזרה לעוגן
    }

    // ===== לוגיקת הצמדת הספר ליד =====
    void TrySnap(string reason)
    {
        lastSnapAttempt = Time.time;

        Transform chosen = null;
        BookState next = BookState.OnAnchor;

        bool rOK = IsValidHand(rightHand);
        bool lOK = IsValidHand(leftHand);

        if (rOK && lOK)
        {
            float dr = Vector3.Distance(anchor.position, rightHand.position);
            float dl = Vector3.Distance(anchor.position, leftHand.position);
            chosen = (dr <= dl ? rightHand : leftHand);
            next = (dr <= dl ? BookState.InRightHand : BookState.InLeftHand);
        }
        else if (rOK) { chosen = rightHand; next = BookState.InRightHand; }
        else if (lOK) { chosen = leftHand;  next = BookState.InLeftHand;  }

        if (!chosen)
        {
            Debug.LogWarning($"[BookPickup] {reason}: אין יד תקפה (RightOK={rOK}, LeftOK={lOK}). ודא שאתה גורר את טרנספורמי ה-XR Controllers הנכונים (אלה שמופיעים כ-OK ב-HUD).");
            return;
        }

        Debug.Log($"[BookPickup] {reason}: מצמיד ליד {(next==BookState.InRightHand ? "Right" : "Left")}");
        state = next;

        transform.SetParent(chosen, worldPositionStays:false);
        if (rb) { rb.isKinematic = true; rb.useGravity = false; }
        transform.localPosition = handLocalPosOffset;
        transform.localRotation = Quaternion.Euler(handLocalEulerOffset);
    }

    void ReturnToAnchor()
    {
        Debug.Log("[BookPickup] ReturnToAnchor");
        state = BookState.OnAnchor;
        transform.SetParent(null, true);
        transform.SetPositionAndRotation(anchor.position, anchor.rotation);
        if (rb) { rb.isKinematic = true; rb.useGravity = false; }
        rotSmoothed = transform.rotation;

        // reset arming when returning to anchor
        if (requireCalmArming) {
            armed = false;
            calmAccum = 0f;
        }
        // גם מאתחל את הדילי אם תרצה שהמשחק יתנהג כמו תחילת סצנה לאחר החזרה לעוגן:
        // startTime = Time.time;
    }

    bool IsValidHand(Transform hand)
    {
        return hand && hand.gameObject.activeInHierarchy && float.IsFinite(hand.position.sqrMagnitude);
    }

    void ApplyHandPose(Transform hand)
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition, handLocalPosOffset, 0.5f);
        var targetLocalRot = Quaternion.Euler(handLocalEulerOffset);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetLocalRot, 0.5f);
    }

    void OnGUI()
    {
        if (!showHud) return;
        string handR = IsValidHand(rightHand) ? "OK" : "X";
        string handL = IsValidHand(leftHand) ? "OK" : "X";
        GUI.Label(new Rect(10,10,1200,22),
            $"Book: {state} | RH:{handR} LH:{handL} | Armed:{armed} Calm:{calmAccum:F2}/{calmArmSeconds:F2}s | DelayLeft:{Mathf.Max(0f, autoSnapDelaySeconds - (Time.time-startTime)):F2}s");
    }
}
